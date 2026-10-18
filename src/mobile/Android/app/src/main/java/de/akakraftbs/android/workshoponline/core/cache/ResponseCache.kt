package de.akakraftbs.android.workshoponline.core.cache

import androidx.room.ColumnInfo
import androidx.room.Dao
import androidx.room.Database
import androidx.room.Entity
import androidx.room.Index
import androidx.room.PrimaryKey
import androidx.room.Query
import androidx.room.RoomDatabase
import androidx.room.Upsert
import java.io.ByteArrayOutputStream
import java.time.Instant
import java.util.zip.GZIPInputStream
import java.util.zip.GZIPOutputStream
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Eine zwischengespeicherte API-Antwort. Gespeichert wird das rohe JSON (gzip-komprimiert,
 * damit auch große Listen unter dem CursorWindow-Limit bleiben), damit neue Felder im
 * Backend keine Datenbank-Migration in der App erfordern.
 */
@Entity(tableName = "cached_response", indices = [Index("scope")])
data class CachedResponse(
    @PrimaryKey val key: String,
    val scope: String,
    @ColumnInfo(typeAffinity = ColumnInfo.BLOB) val payload: ByteArray,
    val updatedAt: Long,
) {
    override fun equals(other: Any?) = other is CachedResponse && other.key == key && other.updatedAt == updatedAt
    override fun hashCode() = key.hashCode()
}

@Dao
interface CachedResponseDao {
    @Query("SELECT * FROM cached_response WHERE `key` = :key LIMIT 1")
    suspend fun entry(key: String): CachedResponse?

    @Upsert
    suspend fun upsert(entry: CachedResponse)

    @Query("DELETE FROM cached_response WHERE scope = :scope")
    suspend fun deleteScope(scope: String)

    @Query("DELETE FROM cached_response")
    suspend fun deleteAll()

    @Query("SELECT COALESCE(SUM(LENGTH(payload)), 0) FROM cached_response")
    suspend fun totalBytes(): Long
}

/** Der Cache ist verzichtbar: Bei Schemaänderungen wird er verworfen statt migriert. */
@Database(entities = [CachedResponse::class], version = 1, exportSchema = false)
abstract class OfflineDatabase : RoomDatabase() {
    abstract fun cachedResponses(): CachedResponseDao
}

class CacheEntry(val payload: String, val updatedAt: Instant)

@Singleton
class ResponseCache @Inject constructor(private val dao: CachedResponseDao) {
    suspend fun entry(key: String): CacheEntry? =
        dao.entry(key)?.let { CacheEntry(gunzip(it.payload), Instant.ofEpochMilli(it.updatedAt)) }

    suspend fun store(payload: String, key: String, scope: String) {
        dao.upsert(CachedResponse(key, scope, gzip(payload), System.currentTimeMillis()))
    }

    suspend fun removeAll(scope: String) = dao.deleteScope(scope)

    suspend fun removeEverything() = dao.deleteAll()

    suspend fun totalBytes(): Long = dao.totalBytes()

    private fun gzip(text: String): ByteArray =
        ByteArrayOutputStream().also { out -> GZIPOutputStream(out).use { it.write(text.toByteArray()) } }.toByteArray()

    private fun gunzip(bytes: ByteArray): String = GZIPInputStream(bytes.inputStream()).use { String(it.readBytes()) }
}
