package br.com.spacestay.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

// Paleta "espacial" (escura) com acento magenta, alinhada à identidade do projeto.
val SpaceBg = Color(0xFF0B0B12)
val SpaceSurface = Color(0xFF161622)
val SpaceSurface2 = Color(0xFF20202E)
val Magenta = Color(0xFFE6005C)
val Cyan = Color(0xFF35D0D6)
val OkGreen = Color(0xFF2ECC71)
val WarnAmber = Color(0xFFF1C40F)
val CritRed = Color(0xFFE74C3C)
val TextMain = Color(0xFFF2F2F7)
val TextDim = Color(0xFFB8B8C6)

private val SpaceColors = darkColorScheme(
    primary = Magenta,
    onPrimary = Color.White,
    secondary = Cyan,
    background = SpaceBg,
    onBackground = TextMain,
    surface = SpaceSurface,
    onSurface = TextMain,
    surfaceVariant = SpaceSurface2,
    onSurfaceVariant = TextDim,
    error = CritRed
)

@Composable
fun SpaceStayTheme(content: @Composable () -> Unit) {
    MaterialTheme(colorScheme = SpaceColors, content = content)
}
