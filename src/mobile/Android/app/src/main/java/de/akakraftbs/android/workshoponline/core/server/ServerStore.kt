package de.akakraftbs.android.workshoponline.core.server

import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import de.akakraftbs.android.workshoponline.core.di.SettingsStore
import kotlinx.coroutines.flow.first
import kotlinx.serialization.json.Json
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ServerStore @Inject constructor(
    @param:SettingsStore private val dataStore: DataStore<Preferences>,
) {
    suspend fun current(): ServerConfiguration {
        if (!AppConfig.allowsCustomServer) return ServerConfiguration.Standard
        val stored = dataStore.data.first()[key] ?: return ServerConfiguration.Standard
        return runCatching { Json.decodeFromString<ServerConfiguration>(stored) }
            .getOrDefault(ServerConfiguration.Standard)
    }

    suspend fun select(server: ServerConfiguration) {
        dataStore.edit { it[key] = Json.encodeToString(server) }
    }

    private companion object {
        val key = stringPreferencesKey("server.current")
    }
}
