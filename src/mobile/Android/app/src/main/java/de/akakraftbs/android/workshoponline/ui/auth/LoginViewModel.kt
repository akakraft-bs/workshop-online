package de.akakraftbs.android.workshoponline.ui.auth

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import de.akakraftbs.android.workshoponline.core.auth.AppSession
import de.akakraftbs.android.workshoponline.core.auth.AuthRedirects
import de.akakraftbs.android.workshoponline.core.auth.Pkce
import de.akakraftbs.android.workshoponline.core.network.ApiError
import de.akakraftbs.android.workshoponline.core.network.HttpMethod
import kotlinx.coroutines.flow.filterNotNull
import kotlinx.coroutines.launch
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import javax.inject.Inject

@HiltViewModel
class LoginViewModel @Inject constructor(
    private val session: AppSession,
    private val redirects: AuthRedirects,
    private val savedState: SavedStateHandle,
) : ViewModel() {
    var email by mutableStateOf("")
    var password by mutableStateOf("")
    var isSubmitting by mutableStateOf(false)
        private set
    var errorMessage by mutableStateOf<String?>(null)
        private set
    var needsConfirmation by mutableStateOf(false)
        private set
    var confirmationSent by mutableStateOf(false)
        private set

    val signOutReason = session.signOutReason
    val server = session.server

    init {
        // Rücksprung aus dem Custom Tab – auch nach einem Prozess-Neustart (Verifier im SavedState).
        viewModelScope.launch {
            redirects.pendingCode.filterNotNull().collect { code ->
                redirects.consume()
                finishGoogleLogin(code)
            }
        }
    }

    fun canSubmit(isOnline: Boolean) = !isSubmitting && isOnline && "@" in email && password.isNotEmpty()

    fun submit() {
        if (isSubmitting) return
        viewModelScope.launch {
            isSubmitting = true
            errorMessage = null
            needsConfirmation = false
            try {
                session.login(email, password)
            } catch (error: ApiError.EmailNotConfirmed) {
                needsConfirmation = true
                errorMessage = error.message
            } catch (error: ApiError) {
                errorMessage = error.message
            } finally {
                isSubmitting = false
            }
        }
    }

    /** Erzeugt eine neue PKCE-Challenge und liefert die Startadresse für den Custom Tab. */
    fun startGoogleLogin(): String {
        val pkce = Pkce()
        savedState[VERIFIER_KEY] = pkce.verifier
        errorMessage = null
        return session.client.googleLoginUrl(pkce.challenge).toString()
    }

    fun resendConfirmation() {
        viewModelScope.launch {
            val body = buildJsonObject { put("email", email.trim()) }.toString()
            runCatching { session.client.sendPublic(HttpMethod.POST, "auth/resend-confirmation", body) }
            confirmationSent = true
        }
    }

    fun webUrl(route: String): String = session.server.value.webUrl(route)

    private suspend fun finishGoogleLogin(code: String) {
        val verifier = savedState.get<String>(VERIFIER_KEY)
        savedState.remove<String>(VERIFIER_KEY)
        if (code.isEmpty() || verifier == null) {
            errorMessage = "Google-Anmeldung fehlgeschlagen. Bitte versuche es erneut."
            return
        }
        isSubmitting = true
        try {
            session.loginWithGoogle(code, verifier)
        } catch (error: ApiError) {
            errorMessage = error.message
        } finally {
            isSubmitting = false
        }
    }

    private companion object {
        const val VERIFIER_KEY = "pkce.verifier"
    }
}
