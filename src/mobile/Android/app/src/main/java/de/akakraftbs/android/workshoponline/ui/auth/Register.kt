package de.akakraftbs.android.workshoponline.ui.auth

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.outlined.MarkEmailRead
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.autofill.ContentType
import androidx.compose.ui.semantics.contentType
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.network.ApiError
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.core.network.JsonCoding
import de.akakraftbs.android.workshoponline.ui.components.FormHint
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import javax.inject.Inject

@HiltViewModel
class RegisterViewModel @Inject constructor(private val session: AppSession) : ViewModel() {
    var displayName by mutableStateOf("")
    var email by mutableStateOf("")
    var password by mutableStateOf("")
    var passwordRepeat by mutableStateOf("")
    var isSubmitting by mutableStateOf(false)
        private set
    var errorMessage by mutableStateOf<String?>(null)
        private set
    var success by mutableStateOf(false)
        private set
    var resetSent by mutableStateOf(false)
        private set

    val canRegister: Boolean
        get() = displayName.isNotBlank() && "@" in email && password.length >= 8 && password == passwordRepeat

    @Serializable private data class RegisterBody(val email: String, val password: String, val displayName: String)

    fun register() {
        viewModelScope.launch {
            isSubmitting = true
            errorMessage = null
            try {
                val body = JsonCoding.json.encodeToString(RegisterBody.serializer(), RegisterBody(email.trim(), password, displayName.trim()))
                session.client.sendPublic(HttpMethod.POST, "auth/register", body)
                success = true
            } catch (error: ApiError) {
                errorMessage = error.message
            } finally {
                isSubmitting = false
            }
        }
    }

    fun requestPasswordReset() {
        viewModelScope.launch {
            isSubmitting = true
            val body = buildJsonObject { put("email", email.trim()) }.toString()
            runCatching { session.client.sendPublic(HttpMethod.POST, "auth/request-password-reset", body) }
            isSubmitting = false
            resetSent = true
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun AuthSubpage(title: String, onBack: () -> Unit, content: @Composable () -> Unit) {
    BackHandler(onBack = onBack)
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(title) },
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Zurück") }
                },
            )
        },
    ) { padding ->
        Box(Modifier.fillMaxSize().padding(padding).imePadding().verticalScroll(rememberScrollState()), contentAlignment = Alignment.TopCenter) {
            Column(
                Modifier.widthIn(max = 480.dp).fillMaxWidth().padding(24.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) { content() }
        }
    }
}

@Composable
fun RegisterScreen(prefilledEmail: String, onBack: () -> Unit, viewModel: RegisterViewModel = hiltViewModel(key = "register")) {
    val isOnline = LocalIsOnline.current
    LaunchedEffect(Unit) { if (viewModel.email.isEmpty()) viewModel.email = prefilledEmail }

    AuthSubpage("Registrieren", onBack) {
        if (viewModel.success) {
            Row(horizontalArrangement = Arrangement.spacedBy(12.dp), verticalAlignment = Alignment.CenterVertically) {
                Icon(Icons.Outlined.MarkEmailRead, contentDescription = null, tint = MaterialTheme.colorScheme.primary)
                Text("Fast geschafft! Bitte bestätige deine E-Mail-Adresse über den Link in der Mail.")
            }
            Button(onClick = onBack, modifier = Modifier.fillMaxWidth()) { Text("Zur Anmeldung") }
            return@AuthSubpage
        }
        OutlinedTextField(
            value = viewModel.displayName,
            onValueChange = { viewModel.displayName = it },
            label = { Text("Anzeigename") },
            singleLine = true,
            supportingText = { Text("Für andere Mitglieder sichtbar.") },
            modifier = Modifier.fillMaxWidth().semantics { contentType = ContentType.PersonFullName },
        )
        OutlinedTextField(
            value = viewModel.email,
            onValueChange = { viewModel.email = it },
            label = { Text("E-Mail") },
            singleLine = true,
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email),
            modifier = Modifier.fillMaxWidth().semantics { contentType = ContentType.EmailAddress },
        )
        OutlinedTextField(
            value = viewModel.password,
            onValueChange = { viewModel.password = it },
            label = { Text("Passwort") },
            singleLine = true,
            visualTransformation = PasswordVisualTransformation(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
            modifier = Modifier.fillMaxWidth().semantics { contentType = ContentType.NewPassword },
        )
        OutlinedTextField(
            value = viewModel.passwordRepeat,
            onValueChange = { viewModel.passwordRepeat = it },
            label = { Text("Passwort wiederholen") },
            singleLine = true,
            isError = viewModel.passwordRepeat.isNotEmpty() && viewModel.passwordRepeat != viewModel.password,
            visualTransformation = PasswordVisualTransformation(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
            modifier = Modifier.fillMaxWidth().semantics { contentType = ContentType.NewPassword },
        )
        FormHint("Mindestens 8 Zeichen. Nach der Bestätigung deiner E-Mail schaltet dich der Vorstand frei.")
        viewModel.errorMessage?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        Button(
            onClick = viewModel::register,
            enabled = isOnline && viewModel.canRegister && !viewModel.isSubmitting,
            modifier = Modifier.fillMaxWidth().height(52.dp),
        ) {
            if (viewModel.isSubmitting) CircularProgressIndicator(Modifier.size(20.dp), strokeWidth = 2.dp) else Text("Konto erstellen")
        }
    }
}

@Composable
fun ForgotPasswordScreen(prefilledEmail: String, onBack: () -> Unit, viewModel: RegisterViewModel = hiltViewModel(key = "forgot")) {
    val isOnline = LocalIsOnline.current
    LaunchedEffect(Unit) { if (viewModel.email.isEmpty()) viewModel.email = prefilledEmail }

    AuthSubpage("Passwort vergessen", onBack) {
        OutlinedTextField(
            value = viewModel.email,
            onValueChange = { viewModel.email = it },
            label = { Text("E-Mail") },
            singleLine = true,
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email),
            modifier = Modifier.fillMaxWidth().semantics { contentType = ContentType.EmailAddress },
        )
        FormHint(
            if (viewModel.resetSent) "Falls ein Konto existiert, haben wir dir einen Link zum Zurücksetzen geschickt."
            else "Du erhältst eine E-Mail mit einem Link zum Zurücksetzen deines Passworts.",
        )
        Button(
            onClick = viewModel::requestPasswordReset,
            enabled = isOnline && "@" in viewModel.email && !viewModel.isSubmitting,
            modifier = Modifier.fillMaxWidth().height(52.dp),
        ) { Text("Link senden") }
    }
}
