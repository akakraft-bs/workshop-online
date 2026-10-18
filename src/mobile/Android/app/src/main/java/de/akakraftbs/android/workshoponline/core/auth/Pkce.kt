package de.akakraftbs.android.workshoponline.core.auth

import java.security.MessageDigest
import java.security.SecureRandom
import java.util.Base64

/** Proof Key for Code Exchange (RFC 7636, S256) für den Google-Login. */
class Pkce(val verifier: String = randomVerifier()) {
    val challenge: String = base64Url(MessageDigest.getInstance("SHA-256").digest(verifier.toByteArray(Charsets.US_ASCII)))

    companion object {
        private fun randomVerifier(): String = ByteArray(32).also(SecureRandom()::nextBytes).let(::base64Url)

        fun base64Url(bytes: ByteArray): String = Base64.getUrlEncoder().withoutPadding().encodeToString(bytes)
    }
}
