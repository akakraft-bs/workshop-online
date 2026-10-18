package de.akakraftbs.android.workshoponline.ui.halle

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.OpenInNew
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.DirectionsCar
import androidx.compose.material.icons.outlined.History
import androidx.compose.material.icons.automirrored.outlined.ListAlt
import androidx.compose.material.icons.outlined.LocalParking
import androidx.compose.material.icons.outlined.ReportProblem
import androidx.compose.material.icons.outlined.Schedule
import androidx.compose.material.icons.outlined.WarningAmber
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.FilledIconButton
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.viewModelScope
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.composable
import androidx.navigation.toRoute
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.model.Fahrzeug
import de.akakraftbs.android.workshoponline.model.ParkAccountStatus
import de.akakraftbs.android.workshoponline.model.ParkBerechtigung
import de.akakraftbs.android.workshoponline.model.ParkCheckinInput
import de.akakraftbs.android.workshoponline.model.ParkClaim
import de.akakraftbs.android.workshoponline.model.ParkHistorie
import de.akakraftbs.android.workshoponline.model.ParkKennzeichenAudit
import de.akakraftbs.android.workshoponline.model.ParkKennzeichenListe
import de.akakraftbs.android.workshoponline.model.ParkOverview
import de.akakraftbs.android.workshoponline.model.Tone
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.ConfirmDialog
import de.akakraftbs.android.workshoponline.ui.components.Avatar
import de.akakraftbs.android.workshoponline.ui.components.DateTimeField
import de.akakraftbs.android.workshoponline.ui.components.DropdownField
import de.akakraftbs.android.workshoponline.ui.components.FeatureViewModel
import de.akakraftbs.android.workshoponline.ui.components.FormHint
import de.akakraftbs.android.workshoponline.ui.components.FormScaffold
import de.akakraftbs.android.workshoponline.ui.components.FormTextField
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import de.akakraftbs.android.workshoponline.ui.components.LocalNavigator
import de.akakraftbs.android.workshoponline.ui.components.MenuAction
import de.akakraftbs.android.workshoponline.ui.components.OverflowMenu
import de.akakraftbs.android.workshoponline.ui.components.RemoteList
import de.akakraftbs.android.workshoponline.ui.components.SectionHeader
import de.akakraftbs.android.workshoponline.ui.components.StatusPill
import de.akakraftbs.android.workshoponline.ui.components.SwitchRow
import de.akakraftbs.android.workshoponline.ui.components.dayTimeString
import de.akakraftbs.android.workshoponline.ui.components.emptyItem
import de.akakraftbs.android.workshoponline.ui.components.fromArg
import de.akakraftbs.android.workshoponline.ui.components.openLink
import de.akakraftbs.android.workshoponline.ui.components.sectionHeader
import de.akakraftbs.android.workshoponline.ui.components.timeString
import de.akakraftbs.android.workshoponline.ui.components.toArg
import de.akakraftbs.android.workshoponline.ui.components.toneColor
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable
import java.time.Duration
import java.time.Instant
import javax.inject.Inject

@Serializable data object ParkenRoute
@Serializable data class ParkCheckinRoute(val account: String, val berechtigung: String, val zweitesKonto: Boolean = false)
@Serializable data class ParkClaimEditRoute(val claim: String)
@Serializable data object ParkHistorieRoute
@Serializable data class ParkKennzeichenRoute(val accountId: String, val label: String)

fun NavGraphBuilder.parkenDestinations() {
    composable<ParkenRoute> { ParkenScreen() }
    composable<ParkCheckinRoute> { ParkCheckinScreen() }
    composable<ParkClaimEditRoute> { ParkClaimEditScreen() }
    composable<ParkHistorieRoute> { ParkHistorieScreen() }
    composable<ParkKennzeichenRoute> { ParkKennzeichenScreen() }
}

// region Übersicht

@HiltViewModel
class ParkenViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val overview = remote<ParkOverview>("parkplatz/overview")
    val problemReported = mutableStateListOf<String>()

    override suspend fun reloadAll() = overview.load(session)

    fun isMine(claim: ParkClaim?) = permissions.isOwner(claim?.userId)

    fun release(claim: ParkClaim) = act(onSuccess = { reloadAll() }) {
        send(HttpMethod.POST, "parkplatz/claims/${claim.id}/freigeben")
    }

    fun reportProblem(account: ParkAccountStatus) = act(onSuccess = { problemReported += account.id }) {
        send(HttpMethod.POST, "parkplatz/accounts/${account.id}/problem")
    }
}

@Composable
private fun ParkenScreen(viewModel: ParkenViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(title = "Campus-Parken", onBack = nav::back) { padding ->
        RemoteList(viewModel.overview, padding, viewModel.isRefreshing, viewModel::refresh, bottomSpacing = 24.dp) { overview ->
            sectionHeader("Parkkonten")
            val ownClaims = overview.accounts.mapNotNull { it.belegung }.count { viewModel.isMine(it) }
            items(overview.accounts, key = { it.id }) { account ->
                ParkAccountCard(account, overview.berechtigung, ownClaims, viewModel)
            }
            item(key = "hinweis") {
                overview.berechtigung.hinweis.nonEmpty?.let {
                    Text(
                        it,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        modifier = Modifier.padding(horizontal = 16.dp, vertical = 4.dp),
                    )
                }
            }
            if (overview.anstehendeReservierungen.isNotEmpty()) {
                sectionHeader("Anstehende Reservierungen")
                items(overview.anstehendeReservierungen, key = { "res-${it.eventId}" }) { reservierung ->
                    ListItem(
                        headlineContent = { Text(reservierung.titel) },
                        supportingContent = {
                            Column {
                                reservierung.start?.let { start ->
                                    Text(start.dayTimeString() + (reservierung.end?.let { " – ${it.timeString()}" } ?: ""))
                                }
                                reservierung.fahrzeugLabel.nonEmpty?.let { Text(it, style = MaterialTheme.typography.bodySmall) }
                            }
                        },
                        trailingContent = { if (reservierung.istMeine) StatusPill("Du", Tone.Blue) },
                    )
                }
            }
            item(key = "links") {
                Column(Modifier.padding(top = 12.dp)) {
                    ListItem(
                        modifier = Modifier.clickable { nav.open(ParkHistorieRoute) },
                        leadingContent = { Icon(Icons.Outlined.History, contentDescription = null) },
                        headlineContent = { Text("Verlauf") },
                    )
                    ListItem(
                        modifier = Modifier.clickable { nav.open(FahrzeugeRoute) },
                        leadingContent = { Icon(Icons.Outlined.DirectionsCar, contentDescription = null) },
                        headlineContent = { Text("Meine Fahrzeuge") },
                    )
                }
            }
        }
    }
    ActionErrorDialog(viewModel.action)
}

@Composable
private fun ParkAccountCard(account: ParkAccountStatus, berechtigung: ParkBerechtigung, ownClaims: Int, viewModel: ParkenViewModel) {
    val nav = LocalNavigator.current
    var releasingForOther by remember { mutableStateOf<ParkClaim?>(null) }
    val context = LocalContext.current
    val isOnline = LocalIsOnline.current
    val claim = account.belegung
    val tone = if (account.istFrei) Tone.Green else Tone.Orange

    Card(
        modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 6.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceContainerLow),
    ) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                Icon(
                    if (account.istFrei) Icons.Outlined.LocalParking else Icons.Outlined.DirectionsCar,
                    contentDescription = null,
                    tint = toneColor(tone),
                    modifier = Modifier.size(28.dp),
                )
                Column(Modifier.weight(1f)) {
                    Text(account.label, style = MaterialTheme.typography.titleMedium)
                    account.notiz.nonEmpty?.let {
                        Text(it, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                }
                StatusPill(if (account.istFrei) "Frei" else "Belegt", tone)
            }

            if (claim != null) {
                Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    Avatar(claim.displayName, claim.pictureUrl, size = 32.dp)
                    Column {
                        Text("${claim.displayName} · ${claim.kennzeichen}", style = MaterialTheme.typography.bodyMedium)
                        Text(
                            "seit ${claim.einfahrtAt.timeString()}" + (claim.voraussichtlichBis?.let { ", bis ca. ${it.timeString()}" } ?: ""),
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                }
            }

            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                if (account.istFrei && ownClaims < 2) {
                    Button(
                        onClick = { nav.open(ParkCheckinRoute(account.toArg(), berechtigung.toArg(), zweitesKonto = ownClaims > 0)) },
                        enabled = isOnline,
                    ) { Text("Einchecken") }
                } else if (claim != null && viewModel.isMine(claim)) {
                    Button(onClick = { viewModel.release(claim) }, enabled = isOnline) { Text("Freigeben") }
                    OutlinedButton(onClick = { nav.open(ParkClaimEditRoute(claim.toArg())) }, enabled = isOnline) {
                        Icon(Icons.Outlined.Schedule, contentDescription = null, modifier = Modifier.size(ButtonDefaults.IconSize))
                        Text("  Bis wann?")
                    }
                } else if (claim != null && viewModel.permissions.isPrivileged) {
                    // Vorstand/Admin/Moderation: fremde Belegung freigeben, z. B. wenn vergessen wurde auszuchecken.
                    OutlinedButton(onClick = { releasingForOther = claim }, enabled = isOnline) { Text("Freigeben") }
                }
                Spacer(Modifier.weight(1f))
                val reported = account.id in viewModel.problemReported
                OverflowMenu(
                    enabled = isOnline,
                    actions = listOfNotNull(
                        MenuAction("Kennzeichen", Icons.AutoMirrored.Outlined.ListAlt) { nav.open(ParkKennzeichenRoute(account.id, account.label)) },
                        account.portalUrl.nonEmpty?.let { url ->
                            MenuAction("Parkportal öffnen", Icons.AutoMirrored.Outlined.OpenInNew) { context.openLink(url) }
                        },
                        if (reported) null else MenuAction("Problem melden", Icons.Outlined.ReportProblem) { viewModel.reportProblem(account) },
                    ),
                )
            }

            if (account.istFrei && ownClaims > 0) {
                FormHint(
                    if (ownClaims == 1) {
                        "Ein zweites Konto ist nur für Gastschraubungen erlaubt – auch ohne Hallenbelegung mit Gast-Zusatz."
                    } else {
                        "Du hältst bereits beide Konten. Zum Wechseln zuerst eines freigeben."
                    },
                )
            }
        }
    }

    releasingForOther?.let { other ->
        ConfirmDialog(
            title = "Für ${other.displayName} freigeben?",
            message = "${account.label} (${other.kennzeichen}) – bitte nur, wenn das Fahrzeug den Campus verlassen hat.",
            confirmLabel = "Freigeben",
            onConfirm = {
                viewModel.release(other)
                releasingForOther = null
            },
            onDismiss = { releasingForOther = null },
        )
    }
}

// endregion

// region Einchecken

@HiltViewModel
class ParkCheckinViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    private val route = saved.toRoute<ParkCheckinRoute>()
    /** Die Person hält bereits ein Konto – das zweite ist nur fürs Gastschrauben gedacht. */
    val zweitesKonto = route.zweitesKonto
    val account: ParkAccountStatus = route.account.fromArg()
    val berechtigung: ParkBerechtigung = route.berechtigung.fromArg()
    val fahrzeuge = remote<List<Fahrzeug>>("fahrzeuge")

    var fahrzeugId by mutableStateOf<String?>(null)
    var manualKennzeichen by mutableStateOf("")
    var manualBezeichnung by mutableStateOf("")
    var hasUntil by mutableStateOf(false)
    var until by mutableStateOf(Instant.now().plus(Duration.ofHours(4)))
    var reservierungId by mutableStateOf<String?>(null)
    var bestaetigt by mutableStateOf(false)

    val canSave: Boolean
        get() = (fahrzeugId != null || manualKennzeichen.isNotBlank()) && (!berechtigung.erfordertBestaetigung || bestaetigt)

    init {
        viewModelScope.launch {
            fahrzeuge.load(session)
            // Beim zweiten Konto parkt meist das Gastfahrzeug → Kennzeichen eingeben.
            if (zweitesKonto) return@launch
            fahrzeugId = berechtigung.vorgeschlagenesFahrzeugId?.lowercase()
                ?: fahrzeuge.value?.firstOrNull { it.istStandard }?.id?.lowercase()
        }
    }

    fun submit(onDone: () -> Unit) {
        val input = ParkCheckinInput(
            parkAccountId = account.id,
            fahrzeugId = fahrzeugId,
            kennzeichen = if (fahrzeugId == null) manualKennzeichen.trim().uppercase() else null,
            fahrzeugBezeichnung = if (fahrzeugId == null) manualBezeichnung.nonEmpty else null,
            einfahrtAt = Instant.now(),
            voraussichtlichBis = if (hasUntil) until else null,
            bestaetigungAkzeptiert = bestaetigt || !berechtigung.erfordertBestaetigung,
            bookingEventId = reservierungId,
        )
        act(onSuccess = { onDone() }) { send(HttpMethod.POST, "parkplatz/checkin", input) }
    }
}

@Composable
private fun ParkCheckinScreen(viewModel: ParkCheckinViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val berechtigung = viewModel.berechtigung
    FormScaffold(
        title = "${viewModel.account.label} belegen",
        onClose = nav::back,
        canSave = viewModel.canSave,
        onSave = { viewModel.submit(nav::back) },
        action = viewModel.action,
        saveLabel = "Einchecken",
    ) {
        if (viewModel.zweitesKonto) {
            FormHint("Ein zweites Parkkonto ist nur für Gastschraubungen erlaubt – auch ohne Hallenbelegung mit Gast-Zusatz. Trage hier das Kennzeichen des Gastfahrzeugs ein.")
        }
        DropdownField(
            label = "Fahrzeug",
            options = viewModel.fahrzeuge.value.orEmpty(),
            selected = viewModel.fahrzeuge.value?.firstOrNull { it.id.lowercase() == viewModel.fahrzeugId },
            optionLabel = { it.label },
            onSelect = { viewModel.fahrzeugId = it?.id?.lowercase() },
            noneLabel = "Anderes Fahrzeug",
        )
        if (viewModel.fahrzeugId == null) {
            FormTextField(viewModel.manualKennzeichen, { viewModel.manualKennzeichen = it }, "Kennzeichen", capitalization = KeyboardCapitalization.Characters)
            FormTextField(viewModel.manualBezeichnung, { viewModel.manualBezeichnung = it }, "Fahrzeug (z. B. VW Golf)")
        }
        SwitchRow("Voraussichtliche Ausfahrt", viewModel.hasUntil, { viewModel.hasUntil = it })
        if (viewModel.hasUntil) DateTimeField("Bis", viewModel.until, { viewModel.until = it })
        if (berechtigung.waehlbareReservierungen.isNotEmpty()) {
            DropdownField(
                label = "Reservierung",
                options = berechtigung.waehlbareReservierungen,
                selected = berechtigung.waehlbareReservierungen.firstOrNull { it.eventId == viewModel.reservierungId },
                optionLabel = { r -> r.titel + (r.start?.let { " · ${it.timeString()}" } ?: "") },
                onSelect = { viewModel.reservierungId = it?.eventId },
                noneLabel = "Keine",
            )
        }
        if (berechtigung.erfordertBestaetigung) {
            SwitchRow("Ich bestätige die Berechtigung", viewModel.bestaetigt, { viewModel.bestaetigt = it })
            FormHint(berechtigung.hinweis)
        }
    }
}

// endregion

// region Belegung ändern

@HiltViewModel
class ParkClaimEditViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val claim: ParkClaim = saved.toRoute<ParkClaimEditRoute>().claim.fromArg()
    var until by mutableStateOf(claim.voraussichtlichBis ?: Instant.now().plus(Duration.ofHours(2)))

    @Serializable private data class UntilBody(val voraussichtlichBis: de.akakraftbs.android.workshoponline.core.network.ApiInstant?)

    fun save(onDone: () -> Unit) = act(onSuccess = { onDone() }) {
        send(HttpMethod.PUT, "parkplatz/claims/${claim.id}", UntilBody(until))
    }
}

@Composable
private fun ParkClaimEditScreen(viewModel: ParkClaimEditViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = "Belegung ändern",
        onClose = nav::back,
        canSave = viewModel.until.isAfter(viewModel.claim.einfahrtAt),
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        DateTimeField("Voraussichtlich bis", viewModel.until, { viewModel.until = it })
    }
}

// endregion

// region Verlauf

@HiltViewModel
class ParkHistorieViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val historie = remote<List<ParkHistorie>>("parkplatz/historie", listOf("limit" to "100"))
    override suspend fun reloadAll() = historie.load(session)
}

@Composable
private fun ParkHistorieScreen(viewModel: ParkHistorieViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    LaunchedEffect(Unit) { viewModel.reload() }
    AppScaffold(title = "Verlauf", onBack = nav::back) { padding ->
        RemoteList(viewModel.historie, padding, viewModel.isRefreshing, viewModel::refresh, bottomSpacing = 24.dp) { entries ->
            if (entries.isEmpty()) emptyItem(Icons.Outlined.History, "Kein Verlauf", "Noch keine Belegungen.")
            items(entries, key = { it.id }) { entry ->
                ListItem(
                    leadingContent = {
                        Icon(
                            when (entry.typ) {
                                "KennzeichenHinzugefuegt" -> Icons.Outlined.Add
                                "KennzeichenEntfernt" -> Icons.Outlined.DeleteOutline
                                else -> Icons.Outlined.DirectionsCar
                            },
                            contentDescription = null,
                            tint = MaterialTheme.colorScheme.primary,
                        )
                    },
                    overlineContent = { Text("${entry.title} · ${entry.accountLabel}") },
                    headlineContent = {
                        Text(listOfNotNull(entry.displayName.nonEmpty, entry.kennzeichen.nonEmpty, entry.fahrzeugBezeichnung.nonEmpty).joinToString(" · "))
                    },
                    supportingContent = {
                        Column {
                            val start = entry.einfahrtAt
                            Text(
                                if (start != null) start.dayTimeString() + (entry.freigegebenAt?.let { " – ${it.timeString()}" } ?: "")
                                else entry.zeitpunkt.dayTimeString(),
                            )
                            entry.bestaetigungHinweis.nonEmpty?.let { Text(it, color = toneColor(Tone.Orange)) }
                        }
                    },
                )
            }
        }
    }
}

// endregion

// region Kennzeichen

@HiltViewModel
class ParkKennzeichenViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val route = saved.toRoute<ParkKennzeichenRoute>()
    val liste = remote<ParkKennzeichenListe>("parkplatz/accounts/${route.accountId}/kennzeichen")
    val audit = remote<List<ParkKennzeichenAudit>>("parkplatz/accounts/${route.accountId}/kennzeichen/historie")
    var newKennzeichen by mutableStateOf("")

    @Serializable private data class AddBody(val kennzeichen: String)

    override suspend fun reloadAll() {
        liste.load(session)
        audit.load(session)
    }

    fun add() = act(onSuccess = { newKennzeichen = ""; reloadAll() }) {
        send(HttpMethod.POST, "parkplatz/accounts/${route.accountId}/kennzeichen", AddBody(newKennzeichen.trim().uppercase()))
    }

    fun remove(kennzeichen: String) = act(onSuccess = { reloadAll() }) {
        send(HttpMethod.DELETE, "parkplatz/accounts/${route.accountId}/kennzeichen", query = listOf("code" to kennzeichen))
    }
}

@Composable
private fun ParkKennzeichenScreen(viewModel: ParkKennzeichenViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(title = viewModel.route.label, onBack = nav::back) { padding ->
        RemoteList(viewModel.liste, padding, viewModel.isRefreshing, viewModel::refresh, bottomSpacing = 24.dp) { liste ->
            liste.fehler.nonEmpty?.let { fehler ->
                item(key = "fehler") {
                    ListItem(
                        leadingContent = { Icon(Icons.Outlined.WarningAmber, contentDescription = null, tint = toneColor(Tone.Orange)) },
                        headlineContent = { Text(fehler, color = toneColor(Tone.Orange)) },
                    )
                }
            }
            sectionHeader("Hinterlegt (${liste.kennzeichen.size}/${liste.max})")
            items(liste.kennzeichen, key = { "kz-$it" }) { kennzeichen ->
                ListItem(
                    headlineContent = { Text(kennzeichen, fontFamily = FontFamily.Monospace) },
                    trailingContent = {
                        if (isOnline) {
                            OverflowMenu(listOf(MenuAction("Entfernen", Icons.Outlined.DeleteOutline, destructive = true) { viewModel.remove(kennzeichen) }))
                        }
                    },
                )
            }
            if (liste.kennzeichen.size < liste.max && liste.zugangKonfiguriert) {
                item(key = "add") {
                    Row(
                        Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                    ) {
                        OutlinedTextField(
                            value = viewModel.newKennzeichen,
                            onValueChange = { viewModel.newKennzeichen = it },
                            label = { Text("Kennzeichen hinzufügen") },
                            singleLine = true,
                            enabled = isOnline,
                            keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Characters),
                            modifier = Modifier.weight(1f),
                        )
                        FilledIconButton(
                            onClick = viewModel::add,
                            enabled = isOnline && viewModel.newKennzeichen.isNotBlank() && !viewModel.action.isRunning,
                        ) { Icon(Icons.Outlined.Add, contentDescription = "Hinzufügen") }
                    }
                }
            }
            if (!liste.zugangKonfiguriert) {
                item(key = "kein-zugang") { FormHintPadded("Für dieses Konto sind keine Portal-Zugangsdaten hinterlegt.") }
            }
            val entries = viewModel.audit.value.orEmpty()
            if (entries.isNotEmpty()) {
                item(key = "audit-header") { SectionHeader("Änderungen") }
                items(entries, key = { "audit-${it.id}" }) { entry ->
                    ListItem(
                        headlineContent = { Text("${entry.aktion}: ${entry.kennzeichen}") },
                        supportingContent = { Text("${entry.ausgefuehrtVon} · ${entry.createdAt.dayTimeString()}") },
                    )
                }
            }
        }
    }
    ActionErrorDialog(viewModel.action)
}

@Composable
private fun FormHintPadded(text: String) {
    Column(Modifier.padding(horizontal = 16.dp, vertical = 4.dp)) { FormHint(text) }
}

// endregion
