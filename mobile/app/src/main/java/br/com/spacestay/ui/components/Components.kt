package br.com.spacestay.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import br.com.spacestay.data.AlertResponse
import br.com.spacestay.data.ModuleResponse
import br.com.spacestay.data.ReadingResponse
import br.com.spacestay.ui.theme.Cyan
import br.com.spacestay.ui.theme.CritRed
import br.com.spacestay.ui.theme.OkGreen
import br.com.spacestay.ui.theme.SpaceSurface2
import br.com.spacestay.ui.theme.TextDim
import br.com.spacestay.ui.theme.WarnAmber

fun statusColor(status: String): Color = when (status) {
    "safe" -> OkGreen
    "warning" -> WarnAmber
    "critical" -> CritRed
    else -> TextDim
}

fun severityColor(severity: String): Color = when (severity) {
    "critical" -> CritRed
    "warning" -> WarnAmber
    "info" -> Cyan
    else -> TextDim
}

fun moduleStatusColor(status: String): Color = when (status) {
    "available" -> OkGreen
    "occupied" -> Cyan
    "maintenance" -> WarnAmber
    else -> TextDim
}

fun sensorShort(type: String): String = when (type) {
    "o2" -> "O₂"
    "co2" -> "CO₂"
    "pressure" -> "Pressão"
    "temperature" -> "Temperatura"
    "humidity" -> "Umidade"
    "water" -> "Água"
    else -> type
}

@Composable
fun SectionTitle(text: String) {
    Text(
        text = text,
        color = MaterialTheme.colorScheme.onBackground,
        fontWeight = FontWeight.Bold,
        fontSize = 18.sp,
        modifier = Modifier.padding(top = 8.dp, bottom = 4.dp)
    )
}

@Composable
fun InfoBanner(text: String) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = SpaceSurface2)
    ) {
        Text(text, color = TextDim, modifier = Modifier.padding(16.dp))
    }
}

// Card KPI compacto de um sensor: rótulo, valor, rótulo de conforto e uma barrinha de faixa.
@Composable
fun KpiCard(reading: ReadingResponse, modifier: Modifier = Modifier) {
    val color = statusColor(reading.status)
    val fraction = if (reading.value == null || reading.maxSafe <= reading.minSafe) 0f
    else ((reading.value - reading.minSafe) / (reading.maxSafe - reading.minSafe))
        .coerceIn(0.0, 1.0).toFloat()

    Card(
        modifier = modifier,
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
        Column(Modifier.padding(14.dp)) {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
                Text(sensorShort(reading.type), color = TextDim, fontWeight = FontWeight.SemiBold, fontSize = 13.sp)
                Box(Modifier.size(8.dp).background(color, CircleShape))
            }
            Spacer(Modifier.height(6.dp))
            Text(
                text = reading.value?.let { "${trimNumber(it)} ${reading.unit}" } ?: "--",
                color = MaterialTheme.colorScheme.onSurface,
                fontSize = 20.sp,
                fontWeight = FontWeight.Bold,
                maxLines = 1
            )
            Text(reading.comfort, color = color, fontWeight = FontWeight.Bold, fontSize = 11.sp)
            Spacer(Modifier.height(8.dp))
            Box(
                Modifier.fillMaxWidth().height(6.dp).background(SpaceSurface2, RoundedCornerShape(3.dp))
            ) {
                Box(Modifier.fillMaxWidth(fraction).height(6.dp).background(color, RoundedCornerShape(3.dp)))
            }
        }
    }
}

// Os 4 sensores que viram KPI (em grade 2x2), compartilhados pelo hóspede e pela equipe.
val KPI_TYPES = listOf("o2", "co2", "temperature", "humidity")

// Painel largo de um módulo (visão da equipe): cabeçalho com status e alertas, e a mesma
// grade de KPIs de conforto que o hóspede vê.
@Composable
fun ModulePanel(module: ModuleResponse, readings: List<ReadingResponse>) {
    Card(
        modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
        Column(Modifier.padding(14.dp)) {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Box(Modifier.size(10.dp).background(moduleStatusColor(module.status), CircleShape))
                    Spacer(Modifier.width(8.dp))
                    Column {
                        Text(module.name, color = MaterialTheme.colorScheme.onSurface, fontWeight = FontWeight.Bold, fontSize = 16.sp)
                        Text("${module.type.uppercase()} - cap. ${module.capacity}", color = TextDim, fontSize = 11.sp)
                    }
                }
                if (module.openAlerts > 0) {
                    Box(Modifier.background(CritRed, RoundedCornerShape(10.dp)).padding(horizontal = 8.dp, vertical = 2.dp)) {
                        Text("${module.openAlerts} alerta(s)", color = Color.White, fontSize = 11.sp, fontWeight = FontWeight.Bold)
                    }
                } else {
                    Text(module.status, color = moduleStatusColor(module.status), fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
                }
            }
            Spacer(Modifier.height(10.dp))
            val kpis = KPI_TYPES.mapNotNull { t -> readings.firstOrNull { it.type == t } }
            if (kpis.isEmpty()) {
                Text("Sem leituras disponíveis.", color = TextDim, fontSize = 12.sp)
            } else {
                kpis.chunked(2).forEach { pair ->
                    Row(Modifier.fillMaxWidth().padding(vertical = 4.dp), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                        pair.forEach { KpiCard(it, Modifier.weight(1f)) }
                        if (pair.size == 1) Spacer(Modifier.weight(1f))
                    }
                }
            }
        }
    }
}

@Composable
fun AlertCard(alert: AlertResponse, onAcknowledge: () -> Unit) {
    val color = severityColor(alert.severity)
    Card(
        modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
        Row(Modifier.padding(0.dp)) {
            Box(Modifier.width(6.dp).height(96.dp).background(color))
            Column(Modifier.padding(14.dp).fillMaxWidth()) {
                Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
                    Text(alert.severity.uppercase(), color = color, fontWeight = FontWeight.Bold, fontSize = 12.sp)
                    Text(alert.moduleName ?: "Módulo ${alert.moduleId}", color = TextDim, fontSize = 12.sp)
                }
                Spacer(Modifier.height(6.dp))
                Text(alert.message, color = MaterialTheme.colorScheme.onSurface, fontSize = 14.sp)
                Spacer(Modifier.height(8.dp))
                Button(onClick = onAcknowledge) { Text("Reconhecer") }
            }
        }
    }
}

// Remove zeros à direita (ex.: 20.90 vira 20.9; 1000.00 vira 1000).
private fun trimNumber(v: Double): String {
    return if (v == v.toLong().toDouble()) v.toLong().toString()
    else v.toString().trimEnd('0').trimEnd('.')
}
