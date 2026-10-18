package de.akakraftbs.android.workshoponline.core.auth

import android.content.Context
import android.net.Uri
import androidx.browser.customtabs.CustomTabsIntent
import androidx.core.net.toUri
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Nimmt den Rücksprung `akakraft://auth?code=…` aus dem Custom Tab entgegen
 * (Gegenstück zu `ASWebAuthenticationSession` auf iOS). Die [de.akakraftbs.android.workshoponline.MainActivity]
 * reicht passende Intents hierher, das Login-ViewModel tauscht den Code ein.
 */
@Singleton
class AuthRedirects @Inject constructor() {
    private val _pendingCode = MutableStateFlow<String?>(null)

    /** Einmal-Code aus dem letzten Rücksprung; leer, wenn der Server keinen Code mitgeschickt hat. */
    val pendingCode: StateFlow<String?> = _pendingCode.asStateFlow()

    fun handle(uri: Uri?): Boolean {
        if (uri?.scheme != CALLBACK_SCHEME || uri.host != CALLBACK_HOST) return false
        _pendingCode.value = uri.getQueryParameter("code").orEmpty()
        return true
    }

    fun consume() {
        _pendingCode.value = null
    }

    companion object {
        const val CALLBACK_SCHEME = "akakraft"
        const val CALLBACK_HOST = "auth"
    }
}

/** Öffnet eine Adresse in einem Custom Tab (Google-Login, Links in die Web-App). */
fun Context.openInCustomTab(url: String) {
    CustomTabsIntent.Builder()
        .setShowTitle(true)
        .build()
        .launchUrl(this, url.toUri())
}
