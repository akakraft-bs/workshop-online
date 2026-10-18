package de.akakraftbs.android.workshoponline.ui.theme

import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.ui.graphics.Color

/**
 * Material-3-Farbschema aus dem AkaKraft-Navy (#293748), abgestimmt auf die Web-App
 * (`--mat-sys-primary` & Co. in `styles.scss`). Neutrale Töne leicht blau getönt,
 * damit keine Standard-Lila-Flächen aus dem M3-Basisschema durchscheinen.
 */
val AkakraftLightColors = lightColorScheme(
    primary = Color(0xFF293748),
    onPrimary = Color(0xFFFFFFFF),
    primaryContainer = Color(0xFFD0E4F5),
    onPrimaryContainer = Color(0xFF0A1C2A),
    inversePrimary = Color(0xFF9AB5CC),
    secondary = Color(0xFF52606F),
    onSecondary = Color(0xFFFFFFFF),
    secondaryContainer = Color(0xFFD6E4F6),
    onSecondaryContainer = Color(0xFF0F1D2A),
    tertiary = Color(0xFF69587A),
    onTertiary = Color(0xFFFFFFFF),
    tertiaryContainer = Color(0xFFF0DBFF),
    onTertiaryContainer = Color(0xFF241532),
    background = Color(0xFFF8F9FC),
    onBackground = Color(0xFF191C20),
    surface = Color(0xFFF8F9FC),
    onSurface = Color(0xFF191C20),
    surfaceVariant = Color(0xFFDFE2EB),
    onSurfaceVariant = Color(0xFF43474E),
    surfaceTint = Color(0xFF293748),
    inverseSurface = Color(0xFF2E3135),
    inverseOnSurface = Color(0xFFEFF0F5),
    outline = Color(0xFF73777F),
    outlineVariant = Color(0xFFC3C6CF),
    surfaceBright = Color(0xFFF8F9FC),
    surfaceDim = Color(0xFFD8DADD),
    surfaceContainerLowest = Color(0xFFFFFFFF),
    surfaceContainerLow = Color(0xFFF2F3F7),
    surfaceContainer = Color(0xFFECEEF1),
    surfaceContainerHigh = Color(0xFFE6E8EB),
    surfaceContainerHighest = Color(0xFFE1E2E6),
)

val AkakraftDarkColors = darkColorScheme(
    primary = Color(0xFFA8C8E6),
    onPrimary = Color(0xFF0E3249),
    primaryContainer = Color(0xFF2A4860),
    onPrimaryContainer = Color(0xFFD0E4F5),
    inversePrimary = Color(0xFF293748),
    secondary = Color(0xFFBAC8DA),
    onSecondary = Color(0xFF243240),
    secondaryContainer = Color(0xFF3B4857),
    onSecondaryContainer = Color(0xFFD6E4F6),
    tertiary = Color(0xFFD4BFE6),
    onTertiary = Color(0xFF3A2A4A),
    tertiaryContainer = Color(0xFF514061),
    onTertiaryContainer = Color(0xFFF0DBFF),
    background = Color(0xFF111418),
    onBackground = Color(0xFFE1E2E8),
    surface = Color(0xFF111418),
    onSurface = Color(0xFFE1E2E8),
    surfaceVariant = Color(0xFF43474E),
    onSurfaceVariant = Color(0xFFC3C6CF),
    surfaceTint = Color(0xFFA8C8E6),
    inverseSurface = Color(0xFFE1E2E8),
    inverseOnSurface = Color(0xFF2E3135),
    outline = Color(0xFF8D9199),
    outlineVariant = Color(0xFF43474E),
    surfaceBright = Color(0xFF37393E),
    surfaceDim = Color(0xFF111418),
    surfaceContainerLowest = Color(0xFF0C0E12),
    surfaceContainerLow = Color(0xFF191C20),
    surfaceContainer = Color(0xFF1D2024),
    surfaceContainerHigh = Color(0xFF282A2E),
    surfaceContainerHighest = Color(0xFF323539),
)
