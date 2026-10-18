package de.akakraftbs.android.workshoponline.ui.halle

import android.Manifest
import android.annotation.SuppressLint
import android.content.Intent
import android.os.Build
import android.provider.Settings
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.LocationOff
import androidx.compose.material.icons.outlined.Map
import androidx.compose.material.icons.outlined.MyLocation
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.core.net.toUri
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.compose.LifecycleResumeEffect
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import com.google.android.gms.location.LocationServices
import com.google.android.gms.location.Priority
import com.google.android.gms.tasks.CancellationTokenSource
import dagger.hilt.android.lifecycle.HiltViewModel
import dagger.hilt.android.qualifiers.ApplicationContext
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.core.location.HallenGeofence
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.model.HallenStandort
import de.akakraftbs.android.workshoponline.ui.components.ConfirmDialog
import de.akakraftbs.android.workshoponline.ui.components.FeatureViewModel
import de.akakraftbs.android.workshoponline.ui.components.FormHint
import de.akakraftbs.android.workshoponline.ui.components.FormScaffold
import de.akakraftbs.android.workshoponline.ui.components.FormTextField
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import de.akakraftbs.android.workshoponline.ui.components.LocalNavigator
import de.akakraftbs.android.workshoponline.ui.components.SectionHeader
import de.akakraftbs.android.workshoponline.ui.components.openLink
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable
import java.util.Locale
import javax.inject.Inject
import kotlin.coroutines.resume
import kotlin.coroutines.suspendCoroutine

@Serializable data object HallenStandortRoute

// region Freigaben

/**
 * Fragt die Freigaben für den Standort-Check-in an: erst Standort (+ Mitteilungen ab Android 13),
 * danach „Immer zulassen“ – Android verlangt dafür eine zweite, getrennte Anfrage.
 */
@Composable
fun rememberGeofencePermissionRequest(onFinished: () -> Unit): () -> Unit {
    val context = LocalContext.current
    val background = rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) { onFinished() }
    val foreground = rememberLauncherForActivityResult(ActivityResultContracts.RequestMultiplePermissions()) {
        if (HallenGeofence.hasForegroundPermission(context) && !HallenGeofence.hasBackgroundPermission(context) &&
            Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q
        ) {
            background.launch(Manifest.permission.ACCESS_BACKGROUND_LOCATION)
        } else {
            onFinished()
        }
    }
    return remember(context) {
        {
            when {
                !HallenGeofence.hasForegroundPermission(context) -> foreground.launch(
                    buildList {
                        add(Manifest.permission.ACCESS_FINE_LOCATION)
                        add(Manifest.permission.ACCESS_COARSE_LOCATION)
                        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) add(Manifest.permission.POST_NOTIFICATIONS)
                    }.toTypedArray(),
                )
                !HallenGeofence.hasBackgroundPermission(context) && Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q ->
                    background.launch(Manifest.permission.ACCESS_BACKGROUND_LOCATION)
                else -> onFinished()
            }
        }
    }
}

@HiltViewModel
class HallenGeofenceViewModel @Inject constructor(
    session: AppSession,
    private val geofence: HallenGeofence,
) : FeatureViewModel(session) {
    val isEnabled = geofence.isEnabled
    val standort = geofence.standort
    val permissionAsked = geofence.permissionAsked

    fun setEnabled(enabled: Boolean) {
        viewModelScope.launch { geofence.setEnabled(enabled) }
    }

    fun permissionsChanged() {
        viewModelScope.launch {
            geofence.markPermissionAsked()
            geofence.apply()
        }
    }
}

/** Einmalige Abfrage der Freigaben nach der Anmeldung, sobald ein Hallen-Standort hinterlegt ist. */
@Composable
fun HallenGeofencePermissionPrompt(viewModel: HallenGeofenceViewModel = hiltViewModel()) {
    val context = LocalContext.current
    val enabled by viewModel.isEnabled.collectAsStateWithLifecycle(initialValue = false)
    val standort by viewModel.standort.collectAsStateWithLifecycle(initialValue = null)
    val asked by viewModel.permissionAsked.collectAsStateWithLifecycle(initialValue = true)
    val request = rememberGeofencePermissionRequest(viewModel::permissionsChanged)
    LaunchedEffect(enabled, standort, asked) {
        if (enabled && standort != null && !asked && !HallenGeofence.hasPermissions(context)) request()
    }
}

/** Abschnitt „Halle“ im Profil: Schalter und Hinweise zu fehlenden Freigaben. */
@Composable
fun HallenGeofenceSettings(viewModel: HallenGeofenceViewModel = hiltViewModel()) {
    val context = LocalContext.current
    val enabled by viewModel.isEnabled.collectAsStateWithLifecycle(initialValue = false)
    val standort by viewModel.standort.collectAsStateWithLifecycle(initialValue = null)
    // Freigaben können in den Systemeinstellungen geändert werden → beim Zurückkehren neu prüfen.
    var permissionTick by remember { mutableIntStateOf(0) }
    LifecycleResumeEffect(Unit) {
        permissionTick++
        viewModel.permissionsChanged()
        onPauseOrDispose { }
    }
    val request = rememberGeofencePermissionRequest {
        permissionTick++
        viewModel.permissionsChanged()
    }
    val hasForeground = remember(permissionTick) { HallenGeofence.hasForegroundPermission(context) }
    val hasBackground = remember(permissionTick) { HallenGeofence.hasBackgroundPermission(context) }

    SectionHeader("Halle")
    ListItem(
        modifier = Modifier.clickable {
            viewModel.setEnabled(!enabled)
            if (!enabled) request()
        },
        headlineContent = { Text("Check-in per Standort") },
        trailingContent = {
            Switch(checked = enabled, onCheckedChange = {
                viewModel.setEnabled(it)
                if (it) request()
            })
        },
    )
    if (enabled) {
        if (!hasForeground || !hasBackground) {
            ListItem(
                modifier = Modifier.clickable {
                    // „Immer zulassen“ lässt sich nach einer Ablehnung nur noch in den App-Einstellungen setzen.
                    context.startActivity(
                        Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS, "package:${context.packageName}".toUri()),
                    )
                },
                leadingContent = { Icon(Icons.Outlined.LocationOff, contentDescription = null) },
                headlineContent = { Text(if (!hasForeground) "Standortfreigabe erteilen" else "Standort „Immer zulassen“") },
                supportingContent = { Text("In den App-Einstellungen unter Berechtigungen → Standort") },
            )
        }
        if (standort == null) {
            ListItem(headlineContent = {
                Text("Für die Halle ist noch kein Standort hinterlegt.", color = MaterialTheme.colorScheme.onSurfaceVariant)
            })
        }
    }
    Column(Modifier.padding(horizontal = 16.dp)) {
        FormHint(
            "Kommst du in die Nähe der Halle (200 m), fragt die App, ob du einchecken möchtest. Verlässt du den Bereich, " +
                "wirst du automatisch ausgecheckt. Damit das auch bei geschlossener App klappt, braucht sie die " +
                "Standortfreigabe „Immer zulassen“.",
        )
    }
}

// endregion

// region Admin: Standort pflegen

@HiltViewModel
class HallenStandortViewModel @Inject constructor(
    session: AppSession,
    private val geofence: HallenGeofence,
    @ApplicationContext private val context: android.content.Context,
) : FeatureViewModel(session) {
    val remote = remote<HallenStandort?>("halle/standort")
    var latitude by mutableStateOf("")
    var longitude by mutableStateOf("")
    var locating by mutableStateOf(false)
        private set
    private var loaded = false

    val parsed: Pair<Double, Double>?
        get() {
            val lat = latitude.trim().replace(',', '.').toDoubleOrNull() ?: return null
            val lon = longitude.trim().replace(',', '.').toDoubleOrNull() ?: return null
            return if (lat in -90.0..90.0 && lon in -180.0..180.0) lat to lon else null
        }

    override suspend fun reloadAll() {
        remote.load(session)
        val standort = remote.value
        if (!loaded && standort != null) {
            set(standort.latitude, standort.longitude)
            loaded = true
        }
    }

    private fun set(lat: Double, lon: Double) {
        latitude = String.format(Locale.ROOT, "%.6f", lat)
        longitude = String.format(Locale.ROOT, "%.6f", lon)
    }

    @SuppressLint("MissingPermission") // vom Bildschirm vorher angefragt
    fun useCurrentLocation() {
        if (!HallenGeofence.hasForegroundPermission(context)) return
        viewModelScope.launch {
            locating = true
            val location = suspendCoroutine { continuation ->
                LocationServices.getFusedLocationProviderClient(context)
                    .getCurrentLocation(Priority.PRIORITY_HIGH_ACCURACY, CancellationTokenSource().token)
                    .addOnSuccessListener { continuation.resume(it) }
                    .addOnFailureListener { continuation.resume(null) }
            }
            locating = false
            if (location != null) set(location.latitude, location.longitude)
            else action.errorMessage = "Standort konnte nicht ermittelt werden."
        }
    }

    @Serializable private data class Body(val latitude: Double, val longitude: Double)

    fun save(onDone: () -> Unit) {
        val (lat, lon) = parsed ?: return
        act(onSuccess = {
            geofence.refreshStandort()
            onDone()
        }) { send(HttpMethod.PUT, "halle/standort", Body(lat, lon)) }
    }

    fun delete(onDone: () -> Unit) {
        act(onSuccess = {
            geofence.refreshStandort()
            onDone()
        }) { send(HttpMethod.DELETE, "halle/standort") }
    }
}

@Composable
fun HallenStandortScreen(viewModel: HallenStandortViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val context = LocalContext.current
    val isOnline = LocalIsOnline.current
    var confirmDelete by remember { mutableStateOf(false) }
    LaunchedEffect(Unit) { viewModel.reload() }
    val requestLocation = rememberLauncherForActivityResult(ActivityResultContracts.RequestMultiplePermissions()) {
        viewModel.useCurrentLocation()
    }

    FormScaffold(
        title = "Hallen-Standort",
        onClose = nav::back,
        canSave = viewModel.parsed != null,
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        FormHint("Die Apps fragen im Umkreis von 200 m um diese Koordinaten, ob man einchecken möchte, und checken beim Verlassen automatisch aus.")
        FormTextField(viewModel.latitude, { viewModel.latitude = it }, "Breitengrad", keyboardType = KeyboardType.Decimal, placeholder = "52.273000")
        FormTextField(viewModel.longitude, { viewModel.longitude = it }, "Längengrad", keyboardType = KeyboardType.Decimal, placeholder = "10.530000")
        OutlinedButton(
            onClick = {
                if (HallenGeofence.hasForegroundPermission(context)) viewModel.useCurrentLocation()
                else requestLocation.launch(arrayOf(Manifest.permission.ACCESS_FINE_LOCATION, Manifest.permission.ACCESS_COARSE_LOCATION))
            },
            enabled = !viewModel.locating,
        ) {
            Icon(Icons.Outlined.MyLocation, contentDescription = null)
            Text(if (viewModel.locating) "  Standort wird ermittelt …" else "  Aktuellen Standort übernehmen")
        }
        viewModel.parsed?.let { (lat, lon) ->
            OutlinedButton(onClick = { context.openLink("geo:$lat,$lon?q=$lat,$lon(Halle)") }) {
                Icon(Icons.Outlined.Map, contentDescription = null)
                Text("  In Karten-App prüfen")
            }
        }
        if (viewModel.remote.value != null) {
            TextButton(onClick = { confirmDelete = true }, enabled = isOnline) {
                Icon(Icons.Outlined.DeleteOutline, contentDescription = null, tint = MaterialTheme.colorScheme.error)
                Text("  Standort entfernen", color = MaterialTheme.colorScheme.error)
            }
            FormHint("Ohne Standort ist der Check-in per Standort für alle aus.")
        }
    }

    if (confirmDelete) {
        ConfirmDialog(
            title = "Standort entfernen?",
            confirmLabel = "Entfernen",
            onConfirm = { viewModel.delete(nav::back) },
            onDismiss = { confirmDelete = false },
        )
    }
}

// endregion
