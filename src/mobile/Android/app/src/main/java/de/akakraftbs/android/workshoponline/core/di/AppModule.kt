package de.akakraftbs.android.workshoponline.core.di

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.preferencesDataStore
import androidx.room.Room
import androidx.work.WorkManager
import coil3.ImageLoader
import coil3.disk.DiskCache
import coil3.disk.directory
import coil3.memory.MemoryCache
import coil3.network.okhttp.OkHttpNetworkFetcherFactory
import coil3.request.crossfade
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import de.akakraftbs.android.workshoponline.core.cache.CachedResponseDao
import de.akakraftbs.android.workshoponline.core.cache.OfflineDatabase
import okhttp3.OkHttpClient
import javax.inject.Qualifier
import javax.inject.Singleton

@Qualifier
@Retention(AnnotationRetention.BINARY)
annotation class SettingsStore

@Qualifier
@Retention(AnnotationRetention.BINARY)
annotation class CredentialsStore

private val Context.settingsDataStore by preferencesDataStore(name = "settings")
private val Context.credentialsDataStore by preferencesDataStore(name = "credentials")

@Module
@InstallIn(SingletonComponent::class)
object AppModule {
    @Provides
    @Singleton
    @SettingsStore
    fun settingsStore(@ApplicationContext context: Context): DataStore<Preferences> = context.settingsDataStore

    @Provides
    @Singleton
    @CredentialsStore
    fun credentialsStore(@ApplicationContext context: Context): DataStore<Preferences> = context.credentialsDataStore

    /** Gemeinsamer Verbindungspool; Cookies werden nie automatisch verwaltet. */
    @Provides
    @Singleton
    fun okHttpClient(): OkHttpClient = OkHttpClient.Builder().build()

    @Provides
    @Singleton
    fun offlineDatabase(@ApplicationContext context: Context): OfflineDatabase =
        Room.databaseBuilder(context, OfflineDatabase::class.java, "offline-cache.db")
            .fallbackToDestructiveMigration(dropAllTables = true)
            .build()

    @Provides
    fun cachedResponseDao(database: OfflineDatabase): CachedResponseDao = database.cachedResponses()

    @Provides
    @Singleton
    fun imageLoader(@ApplicationContext context: Context, okHttpClient: OkHttpClient): ImageLoader =
        ImageLoader.Builder(context)
            .components { add(OkHttpNetworkFetcherFactory(callFactory = { okHttpClient })) }
            .memoryCache { MemoryCache.Builder().maxSizePercent(context, 0.2).build() }
            .diskCache {
                DiskCache.Builder()
                    .directory(context.cacheDir.resolve("images"))
                    .maxSizeBytes(400L * 1024 * 1024)
                    .build()
            }
            .crossfade(true)
            .build()

    @Provides
    fun workManager(@ApplicationContext context: Context): WorkManager = WorkManager.getInstance(context)
}
