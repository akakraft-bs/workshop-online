package de.akakraftbs.android.workshoponline.core.cache

import de.akakraftbs.android.workshoponline.core.network.DateParsing
import de.akakraftbs.android.workshoponline.core.network.Query
import java.time.LocalDate
import java.time.YearMonth
import java.time.ZoneId

/**
 * Abfragen, die von Ansichten *und* [OfflineSync] genutzt werden. Pfad und Query müssen
 * exakt übereinstimmen (auch die Reihenfolge), sonst trifft der Cache-Schlüssel nicht.
 * Die Werte entsprechen denen der iOS-App.
 */
object Endpoints {
    const val HALLENBUCH_PAGE_SIZE = 30

    val hallenbuchFirstPage: Query = listOf("page" to "0", "pageSize" to "$HALLENBUCH_PAGE_SIZE")

    enum class CalendarKind(val raw: String) { Hallenbelegung("Hallenbelegung"), Veranstaltungen("Veranstaltungen") }

    /** Termine eines Monats (lokaler Monatsanfang bis Anfang des Folgemonats). */
    fun calendarEvents(month: YearMonth, kind: CalendarKind, zone: ZoneId = ZoneId.systemDefault()): Query {
        val start = month.atDay(1).atStartOfDay(zone).toInstant()
        val end = month.plusMonths(1).atDay(1).atStartOfDay(zone).toInstant()
        return listOf(
            "from" to DateParsing.format(start),
            "to" to DateParsing.format(end),
            "type" to kind.raw,
        )
    }

    fun currentMonth(): YearMonth = YearMonth.from(LocalDate.now())
}
