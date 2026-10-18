package de.akakraftbs.android.workshoponline.core.server

import de.akakraftbs.android.workshoponline.BuildConfig
import kotlinx.serialization.Serializable
import okhttp3.HttpUrl
import okhttp3.HttpUrl.Companion.toHttpUrl
import okhttp3.HttpUrl.Companion.toHttpUrlOrNull

/**
 * Zielserver der App. Alles Server-spezifische (Tokens, Cache) wird über [id] getrennt abgelegt,
 * damit später – wie bei Nextcloud – mehrere Server bzw. ein Wechsel möglich sind.
 *
 * [baseUrl] ist immer normalisiert: `scheme://host[:port]` ohne Pfad.
 */
@Serializable
data class ServerConfiguration(
    val baseUrl: String,
    val apiPath: String = "/api",
    val webAppPath: String = "/app",
) {
    val id: String get() = baseUrl.trimEnd('/').lowercase()

    val displayName: String
        get() {
            val url = baseUrl.toHttpUrl()
            val defaultPort = HttpUrl.defaultPort(url.scheme)
            return if (url.port == defaultPort) url.host else "${url.host}:${url.port}"
        }

    val apiBaseUrl: HttpUrl
        get() = baseUrl.toHttpUrl().newBuilder().apply {
            apiPath.trim('/').takeIf { it.isNotEmpty() }?.let { addPathSegments(it) }
        }.build()

    /** Link in die Web-App, z. B. für Admin-Funktionen, die es nur dort gibt. */
    fun webUrl(route: String): String {
        val path = listOf(webAppPath, route).joinToString("/") { it.trim('/') }.trim('/')
        return baseUrl.toHttpUrl().newBuilder().apply {
            if (path.isNotEmpty()) addPathSegments(path)
        }.build().toString()
    }

    val isLocal: Boolean get() = baseUrl.toHttpUrl().host.lowercase() in setOf("localhost", "127.0.0.1", "10.0.2.2")

    companion object {
        val Akakraft = ServerConfiguration(baseUrl = "https://app.akakraft-bs.de")

        /**
         * Lokales Backend (`dotnet run`). Ohne `/api`, wie das Web-Frontend lokal –
         * sonst stimmt die bei Google hinterlegte Rücksprungadresse nicht.
         * Im Emulator bzw. auf dem Gerät vorher `adb reverse tcp:5153 tcp:5153` ausführen.
         */
        val Local = ServerConfiguration(baseUrl = "http://localhost:5153", apiPath = "", webAppPath = "")

        /** Debug-Builds nutzen das lokale Backend, Release-Builds den Vereinsserver. */
        val Standard: ServerConfiguration get() = if (BuildConfig.DEBUG) Local else Akakraft

        /** Erzeugt eine Konfiguration aus einer Nutzereingabe wie `app.akakraft-bs.de` oder `https://host:8443`. */
        fun fromUserInput(input: String): ServerConfiguration? {
            var text = input.trim()
            if (text.isEmpty()) return null
            if ("://" !in text) text = "https://$text"
            val url = text.toHttpUrlOrNull() ?: return null
            if (url.host.isEmpty()) return null
            val base = url.newBuilder().encodedPath("/").query(null).fragment(null).build()
                .toString().trimEnd('/')
            val server = ServerConfiguration(baseUrl = base)
            return if (server.isLocal) server.copy(apiPath = "", webAppPath = "") else server
        }
    }
}

object AppConfig {
    /** Schaltet die Server-Auswahl im Login und in den Einstellungen frei (in Debug-Builds immer an). */
    val allowsCustomServer: Boolean = BuildConfig.DEBUG
}
