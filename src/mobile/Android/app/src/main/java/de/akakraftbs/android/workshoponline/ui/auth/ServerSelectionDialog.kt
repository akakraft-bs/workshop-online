package de.akakraftbs.android.workshoponline.ui.auth

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.AssistChip
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.ViewModel
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.server.ServerConfiguration
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class ServerSelectionViewModel @Inject constructor(private val session: AppSession) : ViewModel() {
    val current: ServerConfiguration get() = session.server.value

    suspend fun select(server: ServerConfiguration) = session.switchServer(server)
}

/** Server-Auswahl (nur wenn [de.akakraftbs.android.workshoponline.core.server.AppConfig.allowsCustomServer]). */
@Composable
fun ServerSelectionDialog(onDismiss: () -> Unit, viewModel: ServerSelectionViewModel = hiltViewModel()) {
    val scope = rememberCoroutineScope()
    var input by rememberSaveable { mutableStateOf(viewModel.current.baseUrl) }
    val parsed = ServerConfiguration.fromUserInput(input)

    fun choose(server: ServerConfiguration) {
        scope.launch {
            viewModel.select(server)
            onDismiss()
        }
    }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Server wechseln") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                Text(
                    "Anmeldung und Offline-Daten werden je Server getrennt gespeichert.",
                    style = MaterialTheme.typography.bodyMedium,
                )
                OutlinedTextField(
                    value = input,
                    onValueChange = { input = it },
                    label = { Text("Adresse") },
                    placeholder = { Text("app.akakraft-bs.de") },
                    singleLine = true,
                    isError = input.isNotBlank() && parsed == null,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Uri),
                    modifier = Modifier.fillMaxWidth(),
                )
                FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    AssistChip(onClick = { input = ServerConfiguration.Akakraft.baseUrl }, label = { Text("Vereinsserver") })
                    AssistChip(onClick = { input = ServerConfiguration.Local.baseUrl }, label = { Text("Lokal (adb reverse)") })
                    AssistChip(onClick = { input = "http://10.0.2.2:5153" }, label = { Text("Emulator-Host") })
                }
            }
        },
        confirmButton = {
            TextButton(onClick = { parsed?.let(::choose) }, enabled = parsed != null) { Text("Übernehmen") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Abbrechen") } },
    )
}
