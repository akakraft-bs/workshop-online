package de.akakraftbs.android.workshoponline.core.network

import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.contentOrNull
import java.io.IOException
import java.net.ConnectException
import java.net.NoRouteToHostException
import java.net.SocketTimeoutException
import java.net.UnknownHostException
import kotlinx.serialization.SerializationException
import kotlin.coroutines.cancellation.CancellationException

sealed class ApiError(override val message: String) : Exception(message) {
    data object Offline : ApiError("Keine Verbindung. Änderungen sind nur online möglich.")
    data object Unauthorized : ApiError("Deine Sitzung ist abgelaufen. Bitte melde dich erneut an.")
    data object Forbidden : ApiError("Dafür fehlt dir die Berechtigung.")
    data object NotFound : ApiError("Der Eintrag wurde nicht gefunden.")
    data object EmailNotConfirmed : ApiError("Bitte bestätige zuerst deine E-Mail-Adresse.")
    data class Server(val status: Int, val serverMessage: String?) :
        ApiError(serverMessage ?: "Serverfehler ($status).")
    data class Decoding(val detail: String) : ApiError("Die Antwort des Servers konnte nicht gelesen werden.")
    data class Transport(val detail: String) : ApiError(detail)

    companion object {
        fun from(error: Throwable): ApiError = when (error) {
            is CancellationException -> throw error
            is ApiError -> error
            is UnknownHostException, is ConnectException, is NoRouteToHostException,
            is SocketTimeoutException -> Offline
            is SerializationException, is IllegalArgumentException -> Decoding(error.toString())
            is IOException -> Transport(error.localizedMessage ?: "Netzwerkfehler.")
            else -> Transport(error.localizedMessage ?: error.toString())
        }

        /** Liest Fehlermeldungen im Format `"Text"` oder `{ "error": "Text" }`. */
        fun message(body: String?): String? {
            if (body.isNullOrBlank()) return null
            val element = runCatching { Json.parseToJsonElement(body) }.getOrNull() ?: return null
            return when (element) {
                is JsonPrimitive -> element.contentOrNull?.takeIf { element.isString && it.isNotEmpty() }
                is JsonObject -> listOf("error", "message", "title", "detail").firstNotNullOfOrNull { key ->
                    (element[key] as? JsonPrimitive)?.takeIf { it.isString }?.contentOrNull?.takeIf { it.isNotEmpty() }
                }
                else -> null
            }
        }
    }
}
