package de.akakraftbs.android.workshoponline.core.auth

import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import de.akakraftbs.android.workshoponline.core.di.CredentialsStore
import de.akakraftbs.android.workshoponline.core.server.ServerConfiguration
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.withContext
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import java.security.KeyStore
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec
import javax.inject.Inject
import javax.inject.Singleton

@Serializable
data class Credentials(
    val accessToken: String,
    val accessTokenExpiresAt: Long,
    val refreshToken: String? = null,
) {
    val isAccessTokenFresh: Boolean get() = accessTokenExpiresAt - System.currentTimeMillis() > 60_000
}

/**
 * Speichert Zugangsdaten getrennt je Server – Gegenstück zum iOS-Keychain.
 *
 * Die Daten liegen in einem DataStore, verschlüsselt mit einem AES-Schlüssel aus dem
 * Android Keystore. Der Schlüssel verlässt das Gerät nicht; nach einer Wiederherstellung
 * auf einem anderen Gerät sind die Daten unlesbar und werden verworfen.
 */
@Singleton
class CredentialStore @Inject constructor(
    @param:CredentialsStore private val dataStore: DataStore<Preferences>,
) {
    suspend fun load(server: ServerConfiguration): Credentials? {
        val stored = dataStore.data.first()[key(server)] ?: return null
        return runCatching { withContext(Dispatchers.IO) { Json.decodeFromString<Credentials>(decrypt(stored)) } }
            .onFailure { clear(server) }
            .getOrNull()
    }

    suspend fun save(server: ServerConfiguration, credentials: Credentials) {
        val encrypted = withContext(Dispatchers.IO) { encrypt(Json.encodeToString(credentials)) }
        dataStore.edit { it[key(server)] = encrypted }
    }

    suspend fun clear(server: ServerConfiguration) {
        dataStore.edit { it.remove(key(server)) }
    }

    private fun key(server: ServerConfiguration) = stringPreferencesKey("credentials|${server.id}")

    // region Keystore

    private fun encrypt(plain: String): String {
        val cipher = Cipher.getInstance(TRANSFORMATION).apply { init(Cipher.ENCRYPT_MODE, secretKey()) }
        val encrypted = cipher.doFinal(plain.toByteArray())
        return Base64.encodeToString(cipher.iv + encrypted, Base64.NO_WRAP)
    }

    private fun decrypt(stored: String): String {
        val bytes = Base64.decode(stored, Base64.NO_WRAP)
        val iv = bytes.copyOfRange(0, IV_LENGTH)
        val cipher = Cipher.getInstance(TRANSFORMATION).apply {
            init(Cipher.DECRYPT_MODE, secretKey(), GCMParameterSpec(128, iv))
        }
        return String(cipher.doFinal(bytes, IV_LENGTH, bytes.size - IV_LENGTH))
    }

    private fun secretKey(): SecretKey {
        val keyStore = KeyStore.getInstance(ANDROID_KEYSTORE).apply { load(null) }
        (keyStore.getEntry(KEY_ALIAS, null) as? KeyStore.SecretKeyEntry)?.let { return it.secretKey }
        val spec = KeyGenParameterSpec.Builder(KEY_ALIAS, KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT)
            .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
            .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
            .setKeySize(256)
            .build()
        return KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, ANDROID_KEYSTORE)
            .apply { init(spec) }
            .generateKey()
    }

    private companion object {
        const val ANDROID_KEYSTORE = "AndroidKeyStore"
        const val KEY_ALIAS = "akakraft.credentials"
        const val TRANSFORMATION = "AES/GCM/NoPadding"
        const val IV_LENGTH = 12
    }

    // endregion
}
