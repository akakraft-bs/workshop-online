package de.akakraftbs.android.workshoponline.ui.profil

import android.os.Build
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Favorite
import androidx.compose.material3.Button
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.navigation.NavGraphBuilder
import androidx.navigation.compose.composable
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.BuildConfig
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import de.akakraftbs.android.workshoponline.model.Tone
import de.akakraftbs.android.workshoponline.ui.components.ActionErrorDialog
import de.akakraftbs.android.workshoponline.ui.components.AppScaffold
import de.akakraftbs.android.workshoponline.ui.components.FeatureViewModel
import de.akakraftbs.android.workshoponline.ui.components.FormHint
import de.akakraftbs.android.workshoponline.ui.components.FormTextField
import de.akakraftbs.android.workshoponline.ui.components.LocalIsOnline
import de.akakraftbs.android.workshoponline.ui.components.LocalNavigator
import de.akakraftbs.android.workshoponline.ui.components.toneColor
import kotlinx.serialization.Serializable
import javax.inject.Inject

@Serializable data object FeedbackRoute

fun NavGraphBuilder.feedbackDestinations() {
    composable<FeedbackRoute> { FeedbackScreen() }
}

@HiltViewModel
class FeedbackViewModel @Inject constructor(session: AppSession) : FeatureViewModel(session) {
    var text by mutableStateOf("")
    var sent by mutableStateOf(false)
        private set

    @Serializable private data class FeedbackBody(val text: String, val pageUrl: String, val appVersion: String?)

    fun send() = act(onSuccess = { sent = true }) {
        val version = "Android ${BuildConfig.VERSION_NAME} (${BuildConfig.VERSION_CODE}) · API ${Build.VERSION.SDK_INT}"
        send(HttpMethod.POST, "feedback", FeedbackBody(text.trim(), "android-app", version))
    }
}

@Composable
private fun FeedbackScreen(viewModel: FeedbackViewModel = hiltViewModel()) {
    val nav = LocalNavigator.current
    val isOnline = LocalIsOnline.current
    AppScaffold(title = "Feedback", onBack = nav::back) { padding ->
        Column(
            Modifier.fillMaxSize().padding(top = padding.calculateTopPadding()).padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            if (viewModel.sent) {
                Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    Icon(Icons.Filled.Favorite, contentDescription = null, tint = toneColor(Tone.Red))
                    Text("Danke für dein Feedback!")
                }
            } else {
                FormTextField(viewModel.text, { viewModel.text = it }, "Was können wir verbessern? Hast du einen Fehler gefunden?", minLines = 5)
                FormHint("Wird mit der App-Version an die Administratoren gesendet.")
                Button(onClick = viewModel::send, enabled = isOnline && viewModel.text.isNotBlank() && !viewModel.action.isRunning) {
                    Text("Senden")
                }
            }
        }
    }
    ActionErrorDialog(viewModel.action)
}
