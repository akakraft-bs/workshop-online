package de.akakraftbs.android.workshoponline

import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import dagger.hilt.android.AndroidEntryPoint
import de.akakraftbs.android.workshoponline.core.auth.AuthRedirects
import de.akakraftbs.android.workshoponline.ui.RootScreen
import de.akakraftbs.android.workshoponline.ui.theme.AkakraftTheme
import javax.inject.Inject

@AndroidEntryPoint
class MainActivity : ComponentActivity() {
    @Inject lateinit var authRedirects: AuthRedirects

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        if (savedInstanceState == null) authRedirects.handle(intent?.data)
        setContent {
            AkakraftTheme {
                RootScreen()
            }
        }
    }

    /** Rücksprung aus dem Google-Login (`akakraft://auth?code=…`), Activity ist `singleTop`. */
    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        authRedirects.handle(intent.data)
    }
}
