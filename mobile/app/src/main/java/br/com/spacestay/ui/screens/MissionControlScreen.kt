package br.com.spacestay.ui.screens

import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.livedata.observeAsState
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import br.com.spacestay.ui.components.AlertCard
import br.com.spacestay.ui.components.InfoBanner
import br.com.spacestay.ui.components.ModulePanel
import br.com.spacestay.ui.components.SectionTitle
import br.com.spacestay.viewmodel.MissionControlViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MissionControlScreen(onLogout: () -> Unit) {
    val vm: MissionControlViewModel = viewModel()
    val modules by vm.modules.observeAsState(emptyList())
    val readingsByModule by vm.readingsByModule.observeAsState(emptyMap())
    val alerts by vm.alerts.observeAsState(emptyList())
    val loading by vm.loading.observeAsState(false)
    val error by vm.error.observeAsState()

    LaunchedEffect(Unit) { vm.refresh() }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Mission Control", fontWeight = FontWeight.Bold) },
                actions = {
                    IconButton(onClick = { vm.refresh() }) {
                        Icon(Icons.Filled.Refresh, contentDescription = "Atualizar")
                    }
                    TextButton(onClick = onLogout) { Text("Sair") }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = MaterialTheme.colorScheme.surface,
                    titleContentColor = MaterialTheme.colorScheme.onSurface,
                    actionIconContentColor = MaterialTheme.colorScheme.onSurface
                )
            )
        },
        containerColor = MaterialTheme.colorScheme.background
    ) { padding ->
        LazyColumn(Modifier.padding(padding).padding(horizontal = 16.dp).fillMaxSize()) {
            if (loading) item { LinearProgressIndicator(Modifier.fillMaxWidth()) }
            error?.let { item { Spacer(Modifier.height(8.dp)); InfoBanner(it) } }

            item { SectionTitle("Módulos") }
            items(modules) { m -> ModulePanel(m, readingsByModule[m.id] ?: emptyList()) }

            item { Spacer(Modifier.height(12.dp)); SectionTitle("Alertas em aberto") }
            if (alerts.isEmpty() && !loading) {
                item { InfoBanner("Nenhum alerta em aberto. Tudo sob controle. 🛰️") }
            } else {
                items(alerts) { alert -> AlertCard(alert) { vm.acknowledge(alert.id) } }
            }
            item { Spacer(Modifier.height(24.dp)) }
        }
    }
}
