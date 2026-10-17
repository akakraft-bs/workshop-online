package de.akakraftbs.android.workshoponline.core.connectivity

import android.content.Context
import android.net.ConnectivityManager
import android.net.Network
import android.net.NetworkCapabilities
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.stateIn
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ConnectivityMonitor @Inject constructor(@ApplicationContext context: Context) {
    private val manager = context.getSystemService(ConnectivityManager::class.java)
    private val networkAvailable = MutableStateFlow(currentlyConnected())
    private val _simulateOffline = MutableStateFlow(false)

    /** Zum Testen des Offline-Modus in den Einstellungen. */
    val simulateOffline: StateFlow<Boolean> = _simulateOffline.asStateFlow()

    val isOnlineFlow: StateFlow<Boolean> = combine(networkAvailable, _simulateOffline) { available, simulated ->
        available && !simulated
    }.stateIn(CoroutineScope(SupervisorJob() + Dispatchers.Default), SharingStarted.Eagerly, networkAvailable.value)

    /** Für synchrone Abfragen (z. B. im [de.akakraftbs.android.workshoponline.core.network.ApiClient]). */
    val isOnline: Boolean get() = networkAvailable.value && !_simulateOffline.value

    fun setSimulateOffline(enabled: Boolean) {
        _simulateOffline.value = enabled
    }

    init {
        manager.registerDefaultNetworkCallback(object : ConnectivityManager.NetworkCallback() {
            override fun onCapabilitiesChanged(network: Network, capabilities: NetworkCapabilities) {
                networkAvailable.value = capabilities.hasCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET)
            }

            override fun onLost(network: Network) {
                networkAvailable.value = false
            }
        })
    }

    private fun currentlyConnected(): Boolean =
        manager.getNetworkCapabilities(manager.activeNetwork)
            ?.hasCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET) == true
}
