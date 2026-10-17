package de.akakraftbs.android.workshoponline.core.network

import kotlinx.serialization.KSerializer
import kotlinx.serialization.Serializable
import kotlinx.serialization.SerializationException
import kotlinx.serialization.descriptors.PrimitiveKind
import kotlinx.serialization.descriptors.PrimitiveSerialDescriptor
import kotlinx.serialization.descriptors.SerialDescriptor
import kotlinx.serialization.encoding.Decoder
import kotlinx.serialization.encoding.Encoder
import kotlinx.serialization.json.Json
import java.time.Instant
import java.time.OffsetDateTime
import java.time.format.DateTimeFormatter

/** .NET liefert bis zu 7 Nachkommastellen und teils keine Zeitzone – beides wird hier normalisiert. */
object DateParsing {
    private val zoneSuffix = Regex("""[+-]\d{2}:?\d{2}$""")
    private val compactZone = Regex("""([+-]\d{2})(\d{2})$""")

    fun parse(raw: String): Instant? {
        var text = raw.trim()
        if (text.length == 10) text += "T00:00:00Z"
        if (!text.endsWith("Z") && !zoneSuffix.containsMatchIn(text)) text += "Z"
        text = text.replace(compactZone, "$1:$2")
        return runCatching { OffsetDateTime.parse(text, DateTimeFormatter.ISO_OFFSET_DATE_TIME).toInstant() }.getOrNull()
    }

    fun format(instant: Instant): String = DateTimeFormatter.ISO_INSTANT.format(instant)
}

object InstantSerializer : KSerializer<Instant> {
    override val descriptor: SerialDescriptor = PrimitiveSerialDescriptor("DotNetInstant", PrimitiveKind.STRING)

    override fun deserialize(decoder: Decoder): Instant {
        val raw = decoder.decodeString()
        return DateParsing.parse(raw) ?: throw SerializationException("Ungültiges Datum: $raw")
    }

    override fun serialize(encoder: Encoder, value: Instant) = encoder.encodeString(DateParsing.format(value))
}

/** Datum im .NET-Format für `@Serializable`-Modelle. */
typealias ApiInstant = @Serializable(with = InstantSerializer::class) Instant

/** Enum, das unbekannte Server-Werte nicht als Fehler behandelt, sondern auf [unknown] abbildet. */
abstract class ResilientEnumSerializer<T : Enum<T>>(
    serialName: String,
    private val values: Array<T>,
    private val unknown: T,
    private val raw: (T) -> String,
) : KSerializer<T> {
    override val descriptor: SerialDescriptor = PrimitiveSerialDescriptor(serialName, PrimitiveKind.STRING)

    override fun deserialize(decoder: Decoder): T {
        val value = decoder.decodeString()
        return values.firstOrNull { raw(it) == value } ?: unknown
    }

    override fun serialize(encoder: Encoder, value: T) = encoder.encodeString(raw(value))
}

object JsonCoding {
    val json: Json = Json {
        ignoreUnknownKeys = true
        explicitNulls = false
        coerceInputValues = true
    }
}
