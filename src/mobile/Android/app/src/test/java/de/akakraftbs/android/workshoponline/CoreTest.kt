package de.akakraftbs.android.workshoponline

import de.akakraftbs.android.workshoponline.core.auth.Pkce
import de.akakraftbs.android.workshoponline.core.network.ApiError
import de.akakraftbs.android.workshoponline.core.network.DateParsing
import de.akakraftbs.android.workshoponline.core.network.JsonCoding
import de.akakraftbs.android.workshoponline.core.server.ServerConfiguration
import de.akakraftbs.android.workshoponline.model.Badges
import de.akakraftbs.android.workshoponline.model.Permissions
import de.akakraftbs.android.workshoponline.model.Role
import de.akakraftbs.android.workshoponline.model.User
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.time.Instant

class DateParsingTest {
    @Test fun parsesDotNetSevenFractionDigitsWithoutZone() {
        assertEquals(Instant.parse("2026-10-16T08:30:00.1234567Z"), DateParsing.parse("2026-10-16T08:30:00.1234567"))
    }

    @Test fun parsesOffsetWithAndWithoutColon() {
        assertEquals(Instant.parse("2026-10-16T06:30:00Z"), DateParsing.parse("2026-10-16T08:30:00+02:00"))
        assertEquals(Instant.parse("2026-10-16T06:30:00Z"), DateParsing.parse("2026-10-16T08:30:00+0200"))
    }

    @Test fun parsesDateOnlyAsUtcMidnight() {
        assertEquals(Instant.parse("2026-10-16T00:00:00Z"), DateParsing.parse("2026-10-16"))
    }

    @Test fun rejectsGarbage() {
        assertNull(DateParsing.parse("gestern"))
    }
}

class PkceTest {
    /** Testvektor aus RFC 7636, Anhang B. */
    @Test fun matchesRfcExample() {
        val pkce = Pkce("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk")
        assertEquals("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", pkce.challenge)
    }

    @Test fun generatesUrlSafeVerifier() {
        val verifier = Pkce().verifier
        assertEquals(43, verifier.length)
        assertTrue(verifier.all { it.isLetterOrDigit() || it == '-' || it == '_' })
    }
}

class ServerConfigurationTest {
    @Test fun normalizesUserInput() {
        val server = ServerConfiguration.fromUserInput(" app.akakraft-bs.de/app/dashboard?x=1 ")!!
        assertEquals("https://app.akakraft-bs.de", server.baseUrl)
        assertEquals("/api", server.apiPath)
        assertEquals("https://app.akakraft-bs.de/api/auth/me", server.apiBaseUrl.newBuilder().addPathSegments("auth/me").build().toString())
    }

    @Test fun localServerHasNoApiPrefix() {
        val server = ServerConfiguration.fromUserInput("http://localhost:5153")!!
        assertEquals("", server.apiPath)
        assertEquals("localhost:5153", server.displayName)
        assertEquals("http://localhost:5153/", server.apiBaseUrl.toString())
    }

    @Test fun rejectsOtherSchemes() {
        assertNull(ServerConfiguration.fromUserInput("ftp://example.org"))
        assertNull(ServerConfiguration.fromUserInput(""))
    }

    @Test fun buildsWebUrls() {
        assertEquals("https://app.akakraft-bs.de/app/auth/register", ServerConfiguration.Akakraft.webUrl("auth/register"))
    }
}

class ModelDecodingTest {
    private val json = JsonCoding.json

    @Test fun decodesUserWithUnknownRoleAndExtraFields() {
        val user = json.decodeFromString<User>(
            """{"id":"ABC","email":"a@b.de","name":"Max Muster","createdAt":"2025-01-02T03:04:05.1234567",
               "roles":["Member","Kaiser"],"neuesFeld":42}""",
        )
        assertEquals(listOf(Role.Member, Role.Unknown), user.roles)
        assertTrue(Permissions(user).isMember)
        assertTrue(Permissions(user).isOwner("abc"))
        assertFalse(Permissions(user).isPrivileged)
    }

    @Test fun badgesToleratesMissingFields() {
        assertEquals(Badges(openMaengel = 3), json.decodeFromString<Badges>("""{"openMaengel":3}"""))
    }

    @Test fun readsServerErrorMessages() {
        assertEquals("Kaputt", ApiError.message("\"Kaputt\""))
        assertEquals("Fehlt", ApiError.message("""{"title":"Fehlt"}"""))
        assertNull(ApiError.message("<html>"))
    }
}
