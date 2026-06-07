package br.com.spacestay.ui.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Close
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.livedata.observeAsState
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import br.com.spacestay.data.ExcursionResponse
import br.com.spacestay.data.ModuleResponse
import br.com.spacestay.ui.components.InfoBanner
import br.com.spacestay.ui.components.KPI_TYPES
import br.com.spacestay.ui.components.KpiCard
import br.com.spacestay.ui.components.SectionTitle
import br.com.spacestay.ui.theme.OkGreen
import br.com.spacestay.ui.theme.TextDim
import br.com.spacestay.viewmodel.GuestViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun GuestStayScreen(onLogout: () -> Unit) {
    val vm: GuestViewModel = viewModel()
    val moduleName by vm.moduleName.observeAsState()
    val readings by vm.readings.observeAsState(emptyList())
    val excursions by vm.excursions.observeAsState(emptyList())
    val booked by vm.booked.observeAsState(emptySet())
    val available by vm.available.observeAsState(emptyList())
    val loading by vm.loading.observeAsState(false)
    val error by vm.error.observeAsState()
    val message by vm.message.observeAsState()
    val snackbar = remember { SnackbarHostState() }

    LaunchedEffect(Unit) { vm.load() }
    LaunchedEffect(message) {
        message?.let { snackbar.showSnackbar(it); vm.consumeMessage() }
    }

    val kpis = KPI_TYPES.mapNotNull { t -> readings.firstOrNull { it.type == t } }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text("Minha Estadia", fontWeight = FontWeight.Bold)
                        moduleName?.let { Text(it, fontSize = 13.sp, color = TextDim) }
                    }
                },
                actions = { TextButton(onClick = onLogout) { Text("Sair") } },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = MaterialTheme.colorScheme.surface,
                    titleContentColor = MaterialTheme.colorScheme.onSurface
                )
            )
        },
        snackbarHost = { SnackbarHost(snackbar) },
        containerColor = MaterialTheme.colorScheme.background
    ) { padding ->
        LazyColumn(
            modifier = Modifier.padding(padding).padding(horizontal = 16.dp).fillMaxSize()
        ) {
            item { Spacer(Modifier.height(8.dp)) }
            if (loading && readings.isEmpty()) item { LinearProgressIndicator(Modifier.fillMaxWidth()) }
            error?.let { e -> item { InfoBanner(e); Spacer(Modifier.height(8.dp)) } }
            if (moduleName == null && !loading) {
                item { SectionTitle("Escolher uma cabine") }
                item { Text("Reserve uma cabine para começar a sua estadia.", color = TextDim, fontSize = 13.sp, modifier = Modifier.padding(vertical = 4.dp)) }
                if (available.isEmpty()) item { InfoBanner("Nenhuma cabine disponível no momento.") }
                items(available) { m -> CabinCard(m) { vm.bookModule(m.id) } }
            } else {
                item { SectionTitle("Conforto da cabine") }
                items(kpis.chunked(2)) { pair ->
                    Row(
                        Modifier.fillMaxWidth().padding(vertical = 4.dp),
                        horizontalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        pair.forEach { KpiCard(it, Modifier.weight(1f)) }
                        if (pair.size == 1) Spacer(Modifier.weight(1f))
                    }
                }
            }

            item { Spacer(Modifier.height(16.dp)); SectionTitle("Excursões") }
            items(excursions) { exc ->
                ExcursionRow(
                    excursion = exc,
                    isBooked = exc.bookedByMe || exc.id in booked,
                    onBook = { vm.book(exc.id) },
                    onCancel = { vm.cancel(exc.id) }
                )
            }
            item { Spacer(Modifier.height(24.dp)) }
        }
    }
}

@Composable
private fun ExcursionRow(
    excursion: ExcursionResponse,
    isBooked: Boolean,
    onBook: () -> Unit,
    onCancel: () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
        Column(Modifier.padding(14.dp)) {
            Text(excursion.name, color = MaterialTheme.colorScheme.onSurface, fontWeight = FontWeight.SemiBold, fontSize = 15.sp)
            excursion.description?.let { Text(it, color = TextDim, fontSize = 12.sp) }
            Spacer(Modifier.height(8.dp))
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text("${excursion.availableSpots} vaga(s)", color = TextDim, fontSize = 12.sp)
                if (isBooked) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Surface(color = OkGreen.copy(alpha = 0.16f), shape = RoundedCornerShape(20.dp)) {
                            Row(
                                Modifier.padding(horizontal = 12.dp, vertical = 6.dp),
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Icon(Icons.Filled.Check, contentDescription = null, tint = OkGreen, modifier = Modifier.size(18.dp))
                                Spacer(Modifier.size(6.dp))
                                Text("Reservado", color = OkGreen, fontWeight = FontWeight.Bold, fontSize = 13.sp)
                            }
                        }
                        IconButton(onClick = onCancel) {
                            Icon(Icons.Filled.Close, contentDescription = "Cancelar reserva", tint = TextDim)
                        }
                    }
                } else {
                    Button(onClick = onBook, enabled = excursion.availableSpots > 0) { Text("Reservar") }
                }
            }
        }
    }
}

@Composable
private fun CabinCard(module: ModuleResponse, onBook: () -> Unit) {
    Card(
        modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
        Column(Modifier.padding(14.dp)) {
            Text(module.name, color = MaterialTheme.colorScheme.onSurface, fontWeight = FontWeight.SemiBold, fontSize = 15.sp)
            Text("${module.type.uppercase()} - capacidade ${module.capacity}", color = TextDim, fontSize = 12.sp)
            Spacer(Modifier.height(10.dp))
            Button(onClick = onBook, modifier = Modifier.fillMaxWidth()) { Text("Reservar esta cabine") }
        }
    }
}
