package de.akakraftbs.android.workshoponline.ui.verwaltung

import androidx.compose.foundation.clickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.ArrowForward
import androidx.compose.material.icons.automirrored.outlined.Chat
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.AddComment
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.Edit
import androidx.compose.material.icons.outlined.Email
import androidx.compose.material.icons.outlined.Handshake
import androidx.compose.material.icons.outlined.Language
import androidx.compose.material.icons.outlined.PersonAdd
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.SavedStateHandle
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.composable
import androidx.navigation.toRoute
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.model.Ansprechpartner
import de.akakraftbs.android.workshoponline.model.AnsprechpartnerInput
import de.akakraftbs.android.workshoponline.model.KontaktKanal
import de.akakraftbs.android.workshoponline.model.KontaktReaktion
import de.akakraftbs.android.workshoponline.model.Kontakteintrag
import de.akakraftbs.android.workshoponline.model.KontakteintragInput
import de.akakraftbs.android.workshoponline.model.PartnerDetail
import de.akakraftbs.android.workshoponline.model.PartnerInput
import de.akakraftbs.android.workshoponline.model.PartnerOverview
import de.akakraftbs.android.workshoponline.model.PartnerStatus
import de.akakraftbs.android.workshoponline.model.nonEmpty
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.ConfirmDialog
import de.akakraftbs.android.workshoponline.ui.components.DateField
import de.akakraftbs.android.workshoponline.ui.components.DropdownField
import de.akakraftbs.android.workshoponline.ui.components.FeatureViewModel
import de.akakraftbs.android.workshoponline.ui.components.FormScaffold
import de.akakraftbs.android.workshoponline.ui.components.FormTextField
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import de.akakraftbs.android.workshoponline.ui.components.LocalNavigator
import de.akakraftbs.android.workshoponline.ui.components.MenuAction
import de.akakraftbs.android.workshoponline.ui.components.OverflowMenu
import de.akakraftbs.android.workshoponline.ui.components.RemoteList
import de.akakraftbs.android.workshoponline.ui.components.StatusPill
import de.akakraftbs.android.workshoponline.ui.components.dayString
import de.akakraftbs.android.workshoponline.ui.components.emptyItem
import de.akakraftbs.android.workshoponline.ui.components.fromArg
import de.akakraftbs.android.workshoponline.ui.components.localDate
import de.akakraftbs.android.workshoponline.ui.components.mail
import de.akakraftbs.android.workshoponline.ui.components.openLink
import de.akakraftbs.android.workshoponline.ui.components.relativeString
import de.akakraftbs.android.workshoponline.ui.components.sectionHeader
import de.akakraftbs.android.workshoponline.ui.components.startInstant
import de.akakraftbs.android.workshoponline.ui.components.toArg
import de.akakraftbs.android.workshoponline.ui.inventar.SearchField
import de.akakraftbs.android.workshoponline.ui.verein.ContactLinks
import kotlinx.serialization.Serializable
import java.text.Collator
import java.time.LocalDate
import javax.inject.Inject

@Serializable data object CrmRoute
@Serializable data class CrmDetailRoute(val partnerId: String, val name: String)
@Serializable data class PartnerFormRoute(val partner: String? = null)
@Serializable data class AnsprechpartnerFormRoute(val partnerId: String, val contact: String? = null)
@Serializable data class KontaktFormRoute(val partnerId: String, val contacts: String, val entry: String? = null)

fun NavGraphBuilder.crmDestinations() {
    composable<CrmRoute> { CrmScreen() }
    composable<CrmDetailRoute> { CrmDetailScreen() }
    composable<PartnerFormRoute> { PartnerFormScreen() }
    composable<AnsprechpartnerFormRoute> { AnsprechpartnerFormScreen() }
    composable<KontaktFormRoute> { KontaktFormScreen() }
}

// region Liste

@HiltViewModel
class CrmViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val partner = remote<List<PartnerOverview>>("crm/partner")
    var search by mutableStateOf("")
    var status by mutableStateOf<PartnerStatus?>(null)

    override suspend fun reloadAll() = partner.load(session)

    fun filtered(items: List<PartnerOverview>): List<PartnerOverview> {
        val query = search.trim().lowercase()
        return items
            .filter { status == null || it.status == status }
            .filter { query.isEmpty() || it.name.lowercase().contains(query) || it.kategorie.orEmpty().lowercase().contains(query) }
            .sortedWith(compareBy(Collator.getInstance()) { it.name })
    }
}

@Composable
private fun CrmScreen(viewModel: CrmViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    var statusMenu by remember { mutableStateOf(false) }
    LaunchedEffect(Unit) { viewModel.reload() }

    AppScaffold(
        title = "Partner & Sponsoren",
        onBack = nav::back,
        floatingActionButton = {
            if (isOnline) {
                ExtendedFloatingActionButton(
                    text = { Text("Partner") },
                    icon = { Icon(Icons.Outlined.Add, contentDescription = null) },
                    onClick = { nav.open(PartnerFormRoute()) },
                )
            }
        },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding())) {
            SearchField(viewModel.search, { viewModel.search = it }, "Partner suchen")
            Row(Modifier.horizontalScroll(rememberScrollState()).padding(horizontal = 16.dp)) {
                Box {
                    FilterChip(
                        selected = viewModel.status != null,
                        onClick = { statusMenu = true },
                        label = { Text(viewModel.status?.label ?: "Alle Status") },
                    )
                    DropdownMenu(expanded = statusMenu, onDismissRequest = { statusMenu = false }) {
                        DropdownMenuItem(text = { Text("Alle") }, onClick = { viewModel.status = null; statusMenu = false })
                        PartnerStatus.selectable.forEach { status ->
                            DropdownMenuItem(text = { Text(status.label) }, onClick = { viewModel.status = status; statusMenu = false })
                        }
                    }
                }
            }
            RemoteList(viewModel.partner, PaddingValues(0.dp), viewModel.isRefreshing, viewModel::refresh) { items ->
                val visible = viewModel.filtered(items)
                if (visible.isEmpty()) emptyItem(Icons.Outlined.Handshake, "Keine Partner", "")
                items(visible, key = { it.id }) { partner ->
                    ListItem(
                        modifier = Modifier.clickable { nav.open(CrmDetailRoute(partner.id, partner.name)) },
                        headlineContent = { Text(partner.name) },
                        supportingContent = {
                            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
                                partner.kategorie.nonEmpty?.let { Text(it) }
                                Icon(Icons.AutoMirrored.Outlined.Chat, contentDescription = null, modifier = Modifier.size(14.dp))
                                Text("${partner.anzahlKontakte}")
                                partner.letzterKontakt?.let { Text("zuletzt ${it.relativeString()}") }
                            }
                        },
                        trailingContent = { StatusPill(partner.status.label, partner.status.tone) },
                    )
                }
            }
        }
    }
}

// endregion

// region Detail

@HiltViewModel
class CrmDetailViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val route = saved.toRoute<CrmDetailRoute>()
    val partner = remote<PartnerDetail>("crm/partner/${route.partnerId}")

    override suspend fun reloadAll() = partner.load(session)

    fun deleteContact(contact: Ansprechpartner) = act(onSuccess = { reloadAll() }) {
        send(HttpMethod.DELETE, "crm/partner/${route.partnerId}/ansprechpartner/${contact.id}")
    }

    fun deleteEntry(entry: Kontakteintrag) = act(onSuccess = { reloadAll() }) {
        send(HttpMethod.DELETE, "crm/partner/${route.partnerId}/kontakt/${entry.id}")
    }

    fun deletePartner(onDone: () -> Unit) = act(onSuccess = { onDone() }) {
        send(HttpMethod.DELETE, "crm/partner/${route.partnerId}")
    }
}

@Composable
private fun CrmDetailScreen(viewModel: CrmDetailViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val context = LocalContext.current
    val isOnline = LocalIsOnline.current
    val partnerId = viewModel.route.partnerId
    var confirmDelete by rememberSaveable { mutableStateOf(false) }
    LaunchedEffect(Unit) { viewModel.reload() }
    val current = viewModel.partner.value

    AppScaffold(
        title = current?.name ?: viewModel.route.name,
        onBack = nav::back,
        actions = {
            if (current != null) {
                OverflowMenu(
                    enabled = isOnline,
                    actions = listOf(
                        MenuAction("Kontakt erfassen", Icons.Outlined.AddComment) {
                            nav.open(KontaktFormRoute(partnerId, current.ansprechpartner.toArg()))
                        },
                        MenuAction("Ansprechpartner hinzufügen", Icons.Outlined.PersonAdd) { nav.open(AnsprechpartnerFormRoute(partnerId)) },
                        MenuAction("Partner bearbeiten", Icons.Outlined.Edit) { nav.open(PartnerFormRoute(current.toArg())) },
                        MenuAction("Partner löschen", Icons.Outlined.DeleteOutline, destructive = true) { confirmDelete = true },
                    ),
                )
            }
        },
    ) { padding ->
        RemoteList(viewModel.partner, padding, viewModel.isRefreshing, viewModel::refresh, bottomSpacing = 24.dp) { partner ->
            item(key = "header") {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                    StatusPill(partner.status.label, partner.status.tone)
                    Text(partner.name, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.SemiBold)
                    partner.kategorie.nonEmpty?.let { Text(it, color = MaterialTheme.colorScheme.onSurfaceVariant) }
                    partner.notizen.nonEmpty?.let { Text(it) }
                }
            }
            partner.website.nonEmpty?.let { url ->
                item(key = "website") {
                    ListItem(
                        modifier = Modifier.clickable { context.openLink(if ("://" in url) url else "https://$url") },
                        leadingContent = { Icon(Icons.Outlined.Language, contentDescription = null) },
                        headlineContent = { Text(url) },
                    )
                }
            }

            sectionHeader("Ansprechpartner")
            if (partner.ansprechpartner.isEmpty()) {
                item(key = "no-contacts") {
                    Text("Noch keine Ansprechpartner.", modifier = Modifier.padding(16.dp), color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            }
            items(partner.ansprechpartner, key = { "contact-${it.id}" }) { contact ->
                ListItem(
                    headlineContent = { Text(contact.name) },
                    supportingContent = {
                        Column {
                            contact.position.nonEmpty?.let { Text(it) }
                            contact.email.nonEmpty?.let { email ->
                                Row(
                                    Modifier.clickable { context.mail(email) }.padding(vertical = 4.dp),
                                    verticalAlignment = Alignment.CenterVertically,
                                    horizontalArrangement = Arrangement.spacedBy(6.dp),
                                ) {
                                    Icon(Icons.Outlined.Email, contentDescription = null, modifier = Modifier.size(16.dp), tint = MaterialTheme.colorScheme.primary)
                                    Text(email, color = MaterialTheme.colorScheme.primary)
                                }
                            }
                            ContactLinks(contact.telefon, null)
                            contact.notizen.nonEmpty?.let { Text(it, style = MaterialTheme.typography.bodySmall) }
                        }
                    },
                    trailingContent = {
                        if (isOnline) {
                            OverflowMenu(
                                listOf(
                                    MenuAction("Bearbeiten", Icons.Outlined.Edit) { nav.open(AnsprechpartnerFormRoute(partnerId, contact.toArg())) },
                                    MenuAction("Löschen", Icons.Outlined.DeleteOutline, destructive = true) { viewModel.deleteContact(contact) },
                                ),
                            )
                        }
                    },
                )
            }

            sectionHeader("Kontakthistorie")
            if (partner.kontakteintraege.isEmpty()) {
                item(key = "no-entries") {
                    Text("Noch keine Kontakte erfasst.", modifier = Modifier.padding(16.dp), color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            }
            items(partner.kontakteintraege.sortedByDescending { it.datum }, key = { "entry-${it.id}" }) { entry ->
                ListItem(
                    overlineContent = { Text(entry.datum.dayString() + (entry.ansprechpartnerName?.let { " · $it" } ?: "")) },
                    headlineContent = { Text(entry.kanal.label, fontWeight = FontWeight.SemiBold) },
                    supportingContent = {
                        Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                            StatusPill(entry.reaktion.label, entry.reaktion.tone)
                            Text(entry.zusammenfassung, color = MaterialTheme.colorScheme.onSurface)
                            entry.naechsteSchritte.nonEmpty?.let { next ->
                                Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                                    Icon(Icons.AutoMirrored.Outlined.ArrowForward, contentDescription = null, modifier = Modifier.size(16.dp), tint = MaterialTheme.colorScheme.primary)
                                    Text(next, color = MaterialTheme.colorScheme.primary)
                                }
                            }
                        }
                    },
                    trailingContent = {
                        if (isOnline) {
                            OverflowMenu(
                                listOf(
                                    MenuAction("Bearbeiten", Icons.Outlined.Edit) {
                                        nav.open(KontaktFormRoute(partnerId, partner.ansprechpartner.toArg(), entry.toArg()))
                                    },
                                    MenuAction("Löschen", Icons.Outlined.DeleteOutline, destructive = true) { viewModel.deleteEntry(entry) },
                                ),
                            )
                        }
                    },
                )
            }
        }
    }

    if (confirmDelete) {
        ConfirmDialog(
            "Partner löschen?",
            "Ansprechpartner und Kontakthistorie werden ebenfalls gelöscht.",
            onConfirm = { viewModel.deletePartner(nav::back) },
            onDismiss = { confirmDelete = false },
        )
    }
    ActionErrorDialog(viewModel.action)
}

// endregion

// region Formulare

@HiltViewModel
class PartnerFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    val partner: PartnerDetail? = saved.toRoute<PartnerFormRoute>().partner?.fromArg()
    var name by mutableStateOf(partner?.name.orEmpty())
    var kategorie by mutableStateOf(partner?.kategorie.orEmpty())
    var status by mutableStateOf(partner?.status?.takeIf { it != PartnerStatus.Unknown } ?: PartnerStatus.Potentiell)
    var website by mutableStateOf(partner?.website.orEmpty())
    var notizen by mutableStateOf(partner?.notizen.orEmpty())

    fun save(onDone: () -> Unit) {
        val input = PartnerInput(name.trim(), kategorie.nonEmpty, status, website.nonEmpty, notizen.nonEmpty)
        act(onSuccess = { onDone() }) {
            if (partner != null) send(HttpMethod.PUT, "crm/partner/${partner.id}", input)
            else send(HttpMethod.POST, "crm/partner", input)
        }
    }
}

@Composable
private fun PartnerFormScreen(viewModel: PartnerFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.partner == null) "Neuer Partner" else "Partner bearbeiten",
        onClose = nav::back,
        canSave = viewModel.name.isNotBlank(),
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        FormTextField(viewModel.name, { viewModel.name = it }, "Name")
        FormTextField(viewModel.kategorie, { viewModel.kategorie = it }, "Kategorie")
        DropdownField(
            label = "Status",
            options = PartnerStatus.selectable,
            selected = viewModel.status,
            optionLabel = { it.label },
            onSelect = { it?.let { s -> viewModel.status = s } },
        )
        FormTextField(viewModel.website, { viewModel.website = it }, "Website", keyboardType = KeyboardType.Uri, capitalization = KeyboardCapitalization.None)
        FormTextField(viewModel.notizen, { viewModel.notizen = it }, "Notizen", minLines = 3)
    }
}

@HiltViewModel
class AnsprechpartnerFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    private val route = saved.toRoute<AnsprechpartnerFormRoute>()
    val contact: Ansprechpartner? = route.contact?.fromArg()
    var name by mutableStateOf(contact?.name.orEmpty())
    var position by mutableStateOf(contact?.position.orEmpty())
    var email by mutableStateOf(contact?.email.orEmpty())
    var telefon by mutableStateOf(contact?.telefon.orEmpty())
    var notizen by mutableStateOf(contact?.notizen.orEmpty())

    fun save(onDone: () -> Unit) {
        val input = AnsprechpartnerInput(name.trim(), position.nonEmpty, email.nonEmpty, telefon.nonEmpty, notizen.nonEmpty)
        act(onSuccess = { onDone() }) {
            if (contact != null) send(HttpMethod.PUT, "crm/partner/${route.partnerId}/ansprechpartner/${contact.id}", input)
            else send(HttpMethod.POST, "crm/partner/${route.partnerId}/ansprechpartner", input)
        }
    }
}

@Composable
private fun AnsprechpartnerFormScreen(viewModel: AnsprechpartnerFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.contact == null) "Neuer Ansprechpartner" else "Ansprechpartner bearbeiten",
        onClose = nav::back,
        canSave = viewModel.name.isNotBlank(),
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        FormTextField(viewModel.name, { viewModel.name = it }, "Name", capitalization = KeyboardCapitalization.Words)
        FormTextField(viewModel.position, { viewModel.position = it }, "Position")
        FormTextField(viewModel.email, { viewModel.email = it }, "E-Mail", keyboardType = KeyboardType.Email, capitalization = KeyboardCapitalization.None)
        FormTextField(viewModel.telefon, { viewModel.telefon = it }, "Telefon", keyboardType = KeyboardType.Phone)
        FormTextField(viewModel.notizen, { viewModel.notizen = it }, "Notizen", minLines = 2)
    }
}

@HiltViewModel
class KontaktFormViewModel @Inject constructor(session: AppSession, saved: SavedStateHandle) : FeatureViewModel(session) {
    private val route = saved.toRoute<KontaktFormRoute>()
    val entry: Kontakteintrag? = route.entry?.fromArg()
    val contacts: List<Ansprechpartner> = route.contacts.fromArg()
    var ansprechpartnerId by mutableStateOf(entry?.ansprechpartnerId?.lowercase())
    var datum by mutableStateOf(entry?.datum?.localDate() ?: LocalDate.now())
    var kanal by mutableStateOf(entry?.kanal?.takeIf { it != KontaktKanal.Unknown } ?: KontaktKanal.Email)
    var reaktion by mutableStateOf(entry?.reaktion?.takeIf { it != KontaktReaktion.Unknown } ?: KontaktReaktion.KeineAntwort)
    var zusammenfassung by mutableStateOf(entry?.zusammenfassung.orEmpty())
    var naechsteSchritte by mutableStateOf(entry?.naechsteSchritte.orEmpty())

    fun save(onDone: () -> Unit) {
        val input = KontakteintragInput(ansprechpartnerId, datum.startInstant(), kanal, reaktion, zusammenfassung.trim(), naechsteSchritte.nonEmpty)
        act(onSuccess = { onDone() }) {
            if (entry != null) send(HttpMethod.PUT, "crm/partner/${route.partnerId}/kontakt/${entry.id}", input)
            else send(HttpMethod.POST, "crm/partner/${route.partnerId}/kontakt", input)
        }
    }
}

@Composable
private fun KontaktFormScreen(viewModel: KontaktFormViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    FormScaffold(
        title = if (viewModel.entry == null) "Kontakt erfassen" else "Kontakt bearbeiten",
        onClose = nav::back,
        canSave = viewModel.zusammenfassung.isNotBlank(),
        onSave = { viewModel.save(nav::back) },
        action = viewModel.action,
    ) {
        DateField("Datum", viewModel.datum, { viewModel.datum = it })
        DropdownField("Kanal", KontaktKanal.selectable, viewModel.kanal, { it.label }, { it?.let { k -> viewModel.kanal = k } })
        DropdownField("Reaktion", KontaktReaktion.selectable, viewModel.reaktion, { it.label }, { it?.let { r -> viewModel.reaktion = r } })
        DropdownField(
            label = "Ansprechpartner",
            options = viewModel.contacts,
            selected = viewModel.contacts.firstOrNull { it.id.lowercase() == viewModel.ansprechpartnerId },
            optionLabel = { it.name },
            onSelect = { viewModel.ansprechpartnerId = it?.id?.lowercase() },
            noneLabel = "Keiner",
        )
        FormTextField(viewModel.zusammenfassung, { viewModel.zusammenfassung = it }, "Zusammenfassung", minLines = 3)
        FormTextField(viewModel.naechsteSchritte, { viewModel.naechsteSchritte = it }, "Nächste Schritte", minLines = 2)
    }
}

// endregion
