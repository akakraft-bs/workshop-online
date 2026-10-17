package de.akakraftbs.android.workshoponline.ui.components

import android.text.format.DateUtils
import java.time.Duration
import java.time.Instant
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.ZoneId
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter
import java.time.format.FormatStyle
import java.util.Locale

/** Anzeigeformate – Gegenstück zu den `Date`-Erweiterungen der iOS-App. */
private val zone: ZoneId get() = ZoneId.systemDefault()
private val locale: Locale get() = Locale.getDefault()

fun Instant.local(): LocalDateTime = LocalDateTime.ofInstant(this, zone)
fun Instant.localDate(): LocalDate = local().toLocalDate()
fun LocalDateTime.toInstantLocal(): Instant = atZone(zone).toInstant()
fun LocalDate.startInstant(): Instant = atStartOfDay(zone).toInstant()

/** 16.10.2026 */
fun Instant.dayString(): String = DateTimeFormatter.ofLocalizedDate(FormatStyle.MEDIUM).withLocale(locale).withZone(zone).format(this)

/** 18:30 */
fun Instant.timeString(): String = DateTimeFormatter.ofLocalizedTime(FormatStyle.SHORT).withLocale(locale).withZone(zone).format(this)

/** Freitag, 16. Oktober */
fun Instant.weekdayDayMonth(): String = DateTimeFormatter.ofPattern("EEEE, d. MMMM", locale).withZone(zone).format(this)

/** Freitag, 16. Oktober 2026 */
fun Instant.longDate(): String = DateTimeFormatter.ofLocalizedDate(FormatStyle.FULL).withLocale(locale).withZone(zone).format(this)

/** Fr., 16. Okt. */
fun Instant.shortWeekdayDay(): String = DateTimeFormatter.ofPattern("EEE, d. MMM", locale).withZone(zone).format(this)

/** „vor 3 Stunden“ / „in 2 Tagen“ */
fun Instant.relativeString(): String =
    DateUtils.getRelativeTimeSpanString(toEpochMilli(), System.currentTimeMillis(), DateUtils.MINUTE_IN_MILLIS).toString()

fun LocalDate.monthYear(): String = DateTimeFormatter.ofPattern("LLLL yyyy", locale).format(this)

/** Stunden mit höchstens einer Nachkommastelle, z. B. „2,5 h“. */
fun hours(duration: Duration): String {
    val value = maxOf(duration.toMinutes(), 0) / 60.0
    val text = if (value % 1.0 == 0.0) "%.0f".format(locale, value) else "%.1f".format(locale, value)
    return "$text h"
}

/** Kalendertag als 00:00 UTC – so speichert auch das Web-Frontend reine Datumswerte. */
fun LocalDate.utcMidnight(): Instant = atStartOfDay(ZoneOffset.UTC).toInstant()

/** Gegenstück zu [utcMidnight] für die Anzeige. */
fun Instant.utcDay(): LocalDate = atZone(ZoneOffset.UTC).toLocalDate()

/** Reines Datum (gespeichert als 00:00 UTC) als „16.10.2026“. */
fun Instant.utcDayString(): String = utcDay().startInstant().dayString()

/** „1,2 MB“ */
fun Long.byteString(): String = when {
    this < 1024 -> "$this B"
    this < 1024 * 1024 -> "%.0f KB".format(locale, this / 1024.0)
    else -> "%.1f MB".format(locale, this / (1024.0 * 1024.0))
}
