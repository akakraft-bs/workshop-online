package de.akakraftbs.android.workshoponline.core.location

import android.Manifest
import android.annotation.SuppressLint
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.core.content.ContextCompat
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.longPreferencesKey
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.hilt.work.HiltWorker
import androidx.work.Constraints
import androidx.work.CoroutineWorker
import androidx.work.ExistingWorkPolicy
import androidx.work.NetworkType
import androidx.work.OneTimeWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.WorkerParameters
import androidx.work.workDataOf
import com.google.android.gms.location.Geofence
import com.google.android.gms.location.GeofencingEvent
import com.google.android.gms.location.GeofencingRequest
import com.google.android.gms.location.LocationServices
import com.google.android.gms.tasks.Task
import dagger.assisted.Assisted
import dagger.assisted.AssistedInject
import dagger.hilt.EntryPoint
import dagger.hilt.InstallIn
import dagger.hilt.android.EntryPointAccessors
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import de.akakraftbs.android.workshoponline.MainActivity
import de.akakraftbs.android.workshoponline.R
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.auth.SessionState
import de.akakraftbs.android.workshoponline.core.auth.fetch
import de.akakraftbs.android.workshoponline.core.di.SettingsStore
import de.akakraftbs.android.workshoponline.core.network.ApiError
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.core.network.JsonCoding
import de.akakraftbs.android.workshoponline.model.HallenCheck
import de.akakraftbs.android.workshoponline.model.HallenStandort
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.launch
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.serialization.Serializable
import javax.inject.Inject
import javax.inject.Singleton
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

/**
 * Standort-Check-in an der Halle – Gegenstück zu `HallenGeofence` der iOS-App.
 *
 * Beim Betreten des Umkreises (Geofence) fragt eine Mitteilung, ob man einchecken möchte;
 * beim Verlassen wird automatisch ausgecheckt. Die Halle-Position pflegen Admins
 * (`/halle/standort`); sie wird lokal gemerkt, damit die Geofence nach einem Neustart ohne
 * Netz neu registriert werden kann. Abschaltbar im Profil.
 */
@Singleton
class HallenGeofence @Inject constructor(
    @ApplicationContext private val context: Context,
    @SettingsStore private val settings: DataStore<Preferences>,
    private val session: AppSession,
) {
    /** Nutzer-Einstellung. Standardmäßig an – die Freigaben werden beim ersten Anzeigen angefragt. */
    val isEnabled: Flow<Boolean> = settings.data.map { it[ENABLED] ?: true }

    val standort: Flow<HallenStandort?> = settings.data.map { prefs ->
        prefs[STANDORT]?.let { runCatching { JsonCoding.json.decodeFromString<HallenStandort>(it) }.getOrNull() }
    }

    /** Ob die App schon einmal von sich aus nach den Freigaben gefragt hat (nur einmal, danach übers Profil). */
    val permissionAsked: Flow<Boolean> = settings.data.map { it[PERMISSION_ASKED] ?: false }

    suspend fun markPermissionAsked() {
        settings.edit { it[PERMISSION_ASKED] = true }
    }

    suspend fun setEnabled(enabled: Boolean) {
        settings.edit { it[ENABLED] = enabled }
        apply()
    }

    /** Lädt die Halle-Position vom Server (nach Anmeldung / im Admin-Bereich) und registriert die Geofence neu. */
    suspend fun refreshStandort() {
        if (session.state.value != SessionState.SignedIn) return
        val standort = try {
            val data = session.client.getData("halle/standort")
            JsonCoding.json.decodeFromString<HallenStandort?>(data)
        } catch (error: ApiError) {
            return // offline: zuletzt bekannter Standort bleibt aktiv
        }
        settings.edit { prefs ->
            if (standort == null) prefs.remove(STANDORT) else prefs[STANDORT] = JsonCoding.json.encodeToString(standort)
        }
        apply()
    }

    /** Registriert oder entfernt die Geofence je nach Einstellung, Standort und Freigaben. */
    @SuppressLint("MissingPermission") // durch hasPermissions() geprüft
    suspend fun apply() {
        val client = LocationServices.getGeofencingClient(context)
        val standort = standort.first()
        if (!isEnabled.first() || standort == null || !hasPermissions(context) ||
            session.state.value != SessionState.SignedIn
        ) {
            runCatching { client.removeGeofences(geofencePendingIntent(context)).awaitTask() }
            return
        }
        val geofence = Geofence.Builder()
            .setRequestId(GEOFENCE_ID)
            .setCircularRegion(standort.latitude, standort.longitude, standort.radiusMeter.toFloat())
            .setExpirationDuration(Geofence.NEVER_EXPIRE)
            .setTransitionTypes(Geofence.GEOFENCE_TRANSITION_ENTER or Geofence.GEOFENCE_TRANSITION_EXIT)
            .build()
        val request = GeofencingRequest.Builder()
            .setInitialTrigger(GeofencingRequest.INITIAL_TRIGGER_ENTER)
            .addGeofence(geofence)
            .build()
        runCatching { client.addGeofences(request, geofencePendingIntent(context)).awaitTask() }
    }

    /** Nach dem Abmelden: Überwachung beenden, damit niemand für ein fremdes Konto eingecheckt wird. */
    suspend fun stop() {
        runCatching { LocationServices.getGeofencingClient(context).removeGeofences(geofencePendingIntent(context)).awaitTask() }
        NotificationManagerCompat.from(context).cancel(PROMPT_NOTIFICATION_ID)
    }

    // region Ereignisse (aus dem Worker)

    internal suspend fun onEnter() {
        if (!signedIn()) return
        val lastPrompt = settings.data.first()[LAST_PROMPT] ?: 0L
        if (System.currentTimeMillis() - lastPrompt < PROMPT_COOLDOWN_MS) return
        if (isCheckedIn() == true) return

        settings.edit { it[LAST_PROMPT] = System.currentTimeMillis() }
        val checkIn = PendingIntent.getBroadcast(
            context, 1,
            Intent(context, HallenCheckinActionReceiver::class.java),
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
        )
        notify(
            PROMPT_NOTIFICATION_ID,
            NotificationCompat.Builder(context, CHANNEL_ID)
                .setSmallIcon(R.drawable.ic_notification)
                .setContentTitle("An der Halle?")
                .setContentText("Du bist in der Nähe der Halle. Möchtest du einchecken?")
                .setContentIntent(openAppIntent())
                .setAutoCancel(true)
                .addAction(0, "Einchecken", checkIn)
                .build(),
        )
    }

    internal suspend fun onExit() {
        NotificationManagerCompat.from(context).cancel(PROMPT_NOTIFICATION_ID)
        if (!signedIn() || isCheckedIn() != true) return
        try {
            session.client.send(HttpMethod.DELETE, "halle/checkin")
        } catch (error: ApiError) {
            return
        }
        notify(
            CHECKOUT_NOTIFICATION_ID,
            NotificationCompat.Builder(context, CHANNEL_ID)
                .setSmallIcon(R.drawable.ic_notification)
                .setContentTitle("Ausgecheckt")
                .setContentText("Du hast die Halle verlassen und wurdest automatisch ausgecheckt.")
                .setContentIntent(openAppIntent())
                .setAutoCancel(true)
                .build(),
        )
    }

    internal suspend fun checkIn() {
        NotificationManagerCompat.from(context).cancel(PROMPT_NOTIFICATION_ID)
        if (!signedIn()) return
        try {
            session.client.send(HttpMethod.POST, "halle/checkin", CheckInBody(message = null))
        } catch (_: ApiError) {
            // Ohne Verbindung kein Check-in – die nächste Frage kommt beim erneuten Betreten.
        }
    }

    // endregion

    // region Intern

    /** Im Hintergrund-Start ist die Sitzung noch nicht wiederhergestellt. */
    private suspend fun signedIn(): Boolean {
        session.restore()
        return session.state.value == SessionState.SignedIn
    }

    private suspend fun isCheckedIn(): Boolean? {
        val userId = session.user.value?.id ?: return null
        return try {
            session.fetch<List<HallenCheck>>("halle/anwesend").value.any { it.userId.equals(userId, ignoreCase = true) }
        } catch (error: ApiError) {
            null
        }
    }

    @SuppressLint("MissingPermission") // POST_NOTIFICATIONS wird geprüft
    private fun notify(id: Int, notification: android.app.Notification) {
        ensureChannel(context)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU &&
            ContextCompat.checkSelfPermission(context, Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED
        ) return
        NotificationManagerCompat.from(context).notify(id, notification)
    }

    private fun openAppIntent(): PendingIntent = PendingIntent.getActivity(
        context, 0,
        Intent(context, MainActivity::class.java).addFlags(Intent.FLAG_ACTIVITY_SINGLE_TOP),
        PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
    )

    @Serializable
    private data class CheckInBody(val message: String?)

    // endregion

    companion object {
        private const val GEOFENCE_ID = "halle"
        private const val CHANNEL_ID = "halle"
        private const val PROMPT_NOTIFICATION_ID = 4101
        private const val CHECKOUT_NOTIFICATION_ID = 4102

        /** Am Rand des Umkreises springt das GPS gern hin und her – so oft wird höchstens gefragt. */
        private const val PROMPT_COOLDOWN_MS = 30 * 60 * 1000L

        private val ENABLED = booleanPreferencesKey("halle.geofence.enabled")
        private val STANDORT = stringPreferencesKey("halle.geofence.standort")
        private val LAST_PROMPT = longPreferencesKey("halle.geofence.lastPrompt")
        private val PERMISSION_ASKED = booleanPreferencesKey("halle.geofence.permissionAsked")

        /** Vordergrund-Standort; ohne ihn lässt sich keine Geofence registrieren. */
        fun hasForegroundPermission(context: Context) =
            ContextCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED

        /** „Immer zulassen“ – nötig, damit Ereignisse auch bei geschlossener App ankommen. */
        fun hasBackgroundPermission(context: Context) =
            Build.VERSION.SDK_INT < Build.VERSION_CODES.Q ||
                ContextCompat.checkSelfPermission(context, Manifest.permission.ACCESS_BACKGROUND_LOCATION) == PackageManager.PERMISSION_GRANTED

        fun hasPermissions(context: Context) = hasForegroundPermission(context) && hasBackgroundPermission(context)

        fun ensureChannel(context: Context) {
            val manager = context.getSystemService(NotificationManager::class.java)
            if (manager.getNotificationChannel(CHANNEL_ID) != null) return
            manager.createNotificationChannel(
                NotificationChannel(CHANNEL_ID, "Hallen-Check-in", NotificationManager.IMPORTANCE_DEFAULT).apply {
                    description = "Frage zum Einchecken beim Ankommen und Hinweis beim automatischen Auschecken."
                },
            )
        }

        private fun geofencePendingIntent(context: Context): PendingIntent = PendingIntent.getBroadcast(
            context, 0,
            Intent(context, HallenGeofenceReceiver::class.java),
            // Geofencing schreibt das Ereignis in den Intent → muss veränderbar sein.
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_MUTABLE,
        )
    }
}

/** Geofence-Ereignis → Arbeit im Hintergrund (Netzwerkzugriff braucht mehr Zeit, als ein Receiver hat). */
class HallenGeofenceReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        val event = GeofencingEvent.fromIntent(intent) ?: return
        if (event.hasError()) return
        val transition = when (event.geofenceTransition) {
            Geofence.GEOFENCE_TRANSITION_ENTER -> HallenGeofenceWorker.ENTER
            Geofence.GEOFENCE_TRANSITION_EXIT -> HallenGeofenceWorker.EXIT
            else -> return
        }
        HallenGeofenceWorker.enqueue(context, transition)
    }
}

/** Aktion „Einchecken“ aus der Mitteilung. */
class HallenCheckinActionReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        HallenGeofenceWorker.enqueue(context, HallenGeofenceWorker.CHECK_IN)
    }
}

/** Nach Neustart oder App-Update sind Geofences weg – neu registrieren. */
class HallenBootReceiver : BroadcastReceiver() {
    @EntryPoint
    @InstallIn(SingletonComponent::class)
    interface Dependencies {
        fun session(): AppSession
        fun geofence(): HallenGeofence
    }

    override fun onReceive(context: Context, intent: Intent) {
        val deps = EntryPointAccessors.fromApplication(context.applicationContext, Dependencies::class.java)
        val pending = goAsync()
        CoroutineScope(SupervisorJob() + Dispatchers.Default).launch {
            try {
                deps.session().restore()
                deps.geofence().apply()
            } finally {
                pending.finish()
            }
        }
    }
}

@HiltWorker
class HallenGeofenceWorker @AssistedInject constructor(
    @Assisted context: Context,
    @Assisted params: WorkerParameters,
    private val geofence: HallenGeofence,
) : CoroutineWorker(context, params) {
    override suspend fun doWork(): Result {
        when (inputData.getString(KEY_TRANSITION)) {
            ENTER -> geofence.onEnter()
            EXIT -> geofence.onExit()
            CHECK_IN -> geofence.checkIn()
        }
        return Result.success()
    }

    companion object {
        const val ENTER = "enter"
        const val EXIT = "exit"
        const val CHECK_IN = "checkin"
        private const val KEY_TRANSITION = "transition"

        fun enqueue(context: Context, transition: String) {
            val request = OneTimeWorkRequestBuilder<HallenGeofenceWorker>()
                .setInputData(workDataOf(KEY_TRANSITION to transition))
                .setConstraints(Constraints.Builder().setRequiredNetworkType(NetworkType.CONNECTED).build())
                .build()
            // Betreten/Verlassen ersetzen sich gegenseitig – es zählt das letzte Ereignis.
            val name = if (transition == CHECK_IN) "halle-checkin" else "halle-geofence"
            WorkManager.getInstance(context).enqueueUniqueWork(name, ExistingWorkPolicy.REPLACE, request)
        }
    }
}

/** Play-Services-Task als suspend-Funktion (ohne zusätzliche Bibliothek). */
private suspend fun <T> Task<T>.awaitTask(): T = suspendCancellableCoroutine { continuation ->
    addOnSuccessListener { continuation.resume(it) }
    addOnFailureListener { continuation.resumeWithException(it) }
    addOnCanceledListener { continuation.cancel() }
}
