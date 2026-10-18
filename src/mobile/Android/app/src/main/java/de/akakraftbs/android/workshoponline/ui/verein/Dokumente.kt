package de.akakraftbs.android.workshoponline.ui.verein

import android.content.Intent
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.CreateNewFolder
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.Description
import androidx.compose.material.icons.outlined.Folder
import androidx.compose.material.icons.outlined.Image
import androidx.compose.material.icons.automirrored.outlined.InsertDriveFile
import androidx.compose.material.icons.outlined.PictureAsPdf
import androidx.compose.material.icons.outlined.Share
import androidx.compose.material.icons.outlined.TableChart
import androidx.compose.material.icons.outlined.Upload
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.composable
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.cache.remote
import de.akakraftbs.android.workshoponline.core.network.ApiError
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.core.network.PickedFile
import de.akakraftbs.android.workshoponline.core.network.Uploads
import de.akakraftbs.android.workshoponline.model.Dokument
import de.akakraftbs.android.workshoponline.model.DokumentInput
import de.akakraftbs.android.workshoponline.model.DokumentOrdner
import de.akakraftbs.android.workshoponline.model.FileUploadResult
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.ConfirmDialog
import de.akakraftbs.android.workshoponline.ui.components.FeatureViewModel
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import de.akakraftbs.android.workshoponline.ui.components.LocalNavigator
import de.akakraftbs.android.workshoponline.ui.components.MenuAction
import de.akakraftbs.android.workshoponline.ui.components.OverflowMenu
import de.akakraftbs.android.workshoponline.ui.components.RemoteList
import de.akakraftbs.android.workshoponline.ui.components.SectionHeader
import de.akakraftbs.android.workshoponline.ui.components.byteString
import de.akakraftbs.android.workshoponline.ui.components.dayString
import de.akakraftbs.android.workshoponline.ui.components.emptyItem
import de.akakraftbs.android.workshoponline.ui.components.openLink
import de.akakraftbs.android.workshoponline.ui.inventar.SearchField
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable
import java.text.Collator
import javax.inject.Inject

@Serializable data object DokumenteRoute

fun NavGraphBuilder.dokumenteDestinations() {
    composable<DokumenteRoute> { DokumenteScreen() }
}

val Dokument.icon: ImageVector
    get() = when (extension) {
        "pdf" -> Icons.Outlined.PictureAsPdf
        "jpg", "jpeg", "png", "heic", "gif", "webp" -> Icons.Outlined.Image
        "xls", "xlsx", "csv", "numbers", "ods" -> Icons.Outlined.TableChart
        "doc", "docx", "pages", "txt", "odt" -> Icons.Outlined.Description
        else -> Icons.AutoMirrored.Outlined.InsertDriveFile
    }

private val collator: Collator = Collator.getInstance()

@HiltViewModel
class DokumenteViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    val ordner = remote<List<DokumentOrdner>>("verein/dokumente")
    var search by mutableStateOf("")
    var isUploading by mutableStateOf(false)
        private set

    override suspend fun reloadAll() = ordner.load(session)

    fun filtered(docs: List<Dokument>): List<Dokument> {
        val query = search.trim().lowercase()
        return docs.filter { query.isEmpty() || it.fileName.lowercase().contains(query) }.sortedWith(compareBy(collator) { it.fileName })
    }

    @Serializable private data class FolderBody(val name: String)

    fun createFolder(name: String) {
        if (name.isBlank()) return
        act(onSuccess = { reloadAll() }) { send(HttpMethod.POST, "verein/dokumente/ordner", FolderBody(name.trim())) }
    }

    fun deleteFolder(folder: DokumentOrdner) = act(onSuccess = { reloadAll() }) {
        send(HttpMethod.DELETE, "verein/dokumente/ordner/${folder.id}")
    }

    fun deleteDocument(dokument: Dokument) = act(onSuccess = { reloadAll() }) {
        send(HttpMethod.DELETE, "verein/dokumente/${dokument.id}")
    }

    fun upload(file: PickedFile?, folder: DokumentOrdner) {
        isUploading = true
        act(onSuccess = { isUploading = false; reloadAll() }) {
            try {
                if (file == null) throw ApiError.Transport("Die Datei konnte nicht gelesen werden.")
                val response = upload("uploads/dokument", file.bytes, file.name, file.mimeType)
                val url = decode(FileUploadResult.serializer(), response).resolvedUrl ?: throw ApiError.Transport("Upload fehlgeschlagen.")
                send(HttpMethod.POST, "verein/dokumente", DokumentInput(folder.id, file.name, url, file.bytes.size.toLong()))
            } finally {
                isUploading = false
            }
        }
    }
}

@Composable
private fun DokumenteScreen(viewModel: DokumenteViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val isOnline = LocalIsOnline.current
    val privileged = viewModel.permissions.isPrivileged
    var showNewFolder by rememberSaveable { mutableStateOf(false) }
    var uploadTarget by rememberSaveable { mutableStateOf<String?>(null) }
    var deletingDoc by rememberSaveable { mutableStateOf<String?>(null) }
    LaunchedEffect(Unit) { viewModel.reload() }

    val picker = rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()) { uri ->
        val folder = viewModel.ordner.value?.firstOrNull { it.id == uploadTarget }
        uploadTarget = null
        if (uri != null && folder != null) scope.launch { viewModel.upload(Uploads.read(context, uri), folder) }
    }

    AppScaffold(
        title = "Dokumente",
        onBack = nav::back,
        actions = {
            if (privileged) {
                IconButton(onClick = { showNewFolder = true }, enabled = isOnline) {
                    Icon(Icons.Outlined.CreateNewFolder, contentDescription = "Neuer Ordner")
                }
            }
        },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(top = padding.calculateTopPadding())) {
            SearchField(viewModel.search, { viewModel.search = it }, "Dokumente durchsuchen")
            if (viewModel.isUploading) {
                ListItem(
                    leadingContent = { CircularProgressIndicator(Modifier.size(20.dp), strokeWidth = 2.dp) },
                    headlineContent = { Text("Datei wird hochgeladen …") },
                )
            }
            RemoteList(viewModel.ordner, PaddingValues(0.dp), viewModel.isRefreshing, viewModel::refresh, bottomSpacing = 24.dp) { ordner ->
                if (ordner.isEmpty()) emptyItem(Icons.Outlined.Folder, "Keine Dokumente", "")
                ordner.sortedWith(compareBy(collator) { it.name }).forEach { folder ->
                    val docs = viewModel.filtered(folder.dokumente)
                    if (viewModel.search.isBlank() || docs.isNotEmpty()) {
                        item(key = "folder-${folder.id}") {
                            ListItem(
                                leadingContent = { Icon(Icons.Outlined.Folder, contentDescription = null, tint = MaterialTheme.colorScheme.primary) },
                                headlineContent = { Text(folder.name, style = MaterialTheme.typography.titleSmall, color = MaterialTheme.colorScheme.primary) },
                                trailingContent = {
                                    if (privileged && isOnline) {
                                        OverflowMenu(
                                            listOfNotNull(
                                                MenuAction("Datei hochladen", Icons.Outlined.Upload) {
                                                    uploadTarget = folder.id
                                                    picker.launch(arrayOf("*/*"))
                                                },
                                                if (folder.dokumente.isEmpty()) {
                                                    MenuAction("Ordner löschen", Icons.Outlined.DeleteOutline, destructive = true) { viewModel.deleteFolder(folder) }
                                                } else null,
                                            ),
                                        )
                                    }
                                },
                            )
                        }
                        if (docs.isEmpty()) {
                            item(key = "empty-${folder.id}") {
                                Text("Leerer Ordner", modifier = Modifier.padding(start = 56.dp, bottom = 8.dp), color = MaterialTheme.colorScheme.onSurfaceVariant)
                            }
                        }
                        items(docs, key = { it.id }) { dokument ->
                            ListItem(
                                modifier = Modifier.clickable { context.openLink(dokument.fileUrl) },
                                leadingContent = { Icon(dokument.icon, contentDescription = null) },
                                headlineContent = { Text(dokument.fileName, maxLines = 2) },
                                supportingContent = {
                                    Text(listOfNotNull(dokument.uploadedByName, dokument.uploadedAt.dayString(), dokument.fileSizeBytes?.byteString()).joinToString(" · "))
                                },
                                trailingContent = {
                                    OverflowMenu(
                                        listOfNotNull(
                                            MenuAction("Teilen", Icons.Outlined.Share) {
                                                val intent = Intent(Intent.ACTION_SEND).setType("text/plain").putExtra(Intent.EXTRA_TEXT, dokument.fileUrl)
                                                context.startActivity(Intent.createChooser(intent, dokument.fileName))
                                            },
                                            if (privileged && isOnline) {
                                                MenuAction("Löschen", Icons.Outlined.DeleteOutline, destructive = true) { deletingDoc = dokument.id }
                                            } else null,
                                        ),
                                    )
                                },
                            )
                        }
                    }
                }
            }
        }
    }

    if (showNewFolder) {
        NewFolderDialog(onCreate = viewModel::createFolder, onDismiss = { showNewFolder = false })
    }
    deletingDoc?.let { id ->
        val dokument = viewModel.ordner.value?.flatMap { it.dokumente }?.firstOrNull { it.id == id }
        ConfirmDialog("„${dokument?.fileName.orEmpty()}“ löschen?", onConfirm = { dokument?.let(viewModel::deleteDocument) }, onDismiss = { deletingDoc = null })
    }
    ActionErrorDialog(viewModel.action)
}

@Composable
private fun NewFolderDialog(onCreate: (String) -> Unit, onDismiss: () -> Unit) {
    var name by rememberSaveable { mutableStateOf("") }
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Neuer Ordner") },
        text = { OutlinedTextField(value = name, onValueChange = { name = it }, label = { Text("Name") }, singleLine = true) },
        confirmButton = { TextButton(onClick = { onDismiss(); onCreate(name) }, enabled = name.isNotBlank()) { Text("Anlegen") } },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Abbrechen") } },
    )
}
