package br.com.spacestay.viewmodel

import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.ViewModel
import br.com.spacestay.data.AlertResponse
import br.com.spacestay.data.AuthResponse
import br.com.spacestay.data.ExcursionResponse
import br.com.spacestay.data.ModuleResponse
import br.com.spacestay.data.ReadingResponse
import br.com.spacestay.data.Session
import br.com.spacestay.data.SpaceStayRepository
import java.util.concurrent.ConcurrentHashMap

// MVVM: cada ViewModel expõe LiveData que as telas (Compose) observam.
// As ViewModels têm construtor sem argumentos para a factory padrão do viewModel().

class LoginViewModel : ViewModel() {
    private val repo = SpaceStayRepository()

    private val _loading = MutableLiveData(false)
    val loading: LiveData<Boolean> = _loading
    private val _error = MutableLiveData<String?>(null)
    val error: LiveData<String?> = _error
    private val _success = MutableLiveData<AuthResponse?>(null)
    val success: LiveData<AuthResponse?> = _success

    fun login(email: String, password: String) {
        _loading.value = true
        _error.value = null
        repo.login(email.trim(), password) { result ->
            _loading.postValue(false)
            result.onSuccess { auth ->
                Session.set(auth)
                _success.postValue(auth)
            }.onFailure { e -> _error.postValue(e.message ?: "Falha no login.") }
        }
    }

    fun consumeSuccess() { _success.value = null }
}

class RegisterViewModel : ViewModel() {
    private val repo = SpaceStayRepository()

    private val _loading = MutableLiveData(false)
    val loading: LiveData<Boolean> = _loading
    private val _error = MutableLiveData<String?>(null)
    val error: LiveData<String?> = _error
    private val _success = MutableLiveData<AuthResponse?>(null)
    val success: LiveData<AuthResponse?> = _success

    fun register(name: String, email: String, password: String, nationality: String?, medical: Boolean) {
        _loading.value = true
        _error.value = null
        repo.register(name.trim(), email.trim(), password, nationality?.trim()?.ifBlank { null }, medical) { result ->
            _loading.postValue(false)
            result.onSuccess { auth ->
                Session.set(auth)
                _success.postValue(auth)
            }.onFailure { e -> _error.postValue(e.message ?: "Falha no cadastro.") }
        }
    }

    fun consumeSuccess() { _success.value = null }
}

class MissionControlViewModel : ViewModel() {
    private val repo = SpaceStayRepository()

    private val _modules = MutableLiveData<List<ModuleResponse>>(emptyList())
    val modules: LiveData<List<ModuleResponse>> = _modules
    private val _readingsByModule = MutableLiveData<Map<Int, List<ReadingResponse>>>(emptyMap())
    val readingsByModule: LiveData<Map<Int, List<ReadingResponse>>> = _readingsByModule
    private val readingsAcc = ConcurrentHashMap<Int, List<ReadingResponse>>()
    private val _alerts = MutableLiveData<List<AlertResponse>>(emptyList())
    val alerts: LiveData<List<AlertResponse>> = _alerts
    private val _loading = MutableLiveData(false)
    val loading: LiveData<Boolean> = _loading
    private val _error = MutableLiveData<String?>(null)
    val error: LiveData<String?> = _error

    fun refresh() {
        _loading.value = true
        _error.value = null
        repo.modules { result ->
            result.onSuccess { paged ->
                _modules.postValue(paged.items)
                readingsAcc.clear()
                // Busca a telemetria de cada módulo para montar os KPIs do painel.
                paged.items.forEach { m ->
                    repo.readings(m.id) { r ->
                        r.onSuccess { list ->
                            readingsAcc[m.id] = list
                            _readingsByModule.postValue(HashMap(readingsAcc))
                        }
                    }
                }
            }.onFailure { e -> _error.postValue(e.message) }
        }
        repo.openAlerts { result ->
            _loading.postValue(false)
            result.onSuccess { _alerts.postValue(it.items) }
                .onFailure { e -> _error.postValue(e.message) }
        }
    }

    fun acknowledge(id: Int) {
        repo.acknowledge(id) { result ->
            result.onSuccess { reloadAlerts() }
                .onFailure { e -> _error.postValue(e.message) }
        }
    }

    private fun reloadAlerts() {
        repo.openAlerts { it.onSuccess { paged -> _alerts.postValue(paged.items) } }
    }
}

class GuestViewModel : ViewModel() {
    private val repo = SpaceStayRepository()

    private val _moduleName = MutableLiveData<String?>(null)
    val moduleName: LiveData<String?> = _moduleName
    private val _readings = MutableLiveData<List<ReadingResponse>>(emptyList())
    val readings: LiveData<List<ReadingResponse>> = _readings
    private val _excursions = MutableLiveData<List<ExcursionResponse>>(emptyList())
    val excursions: LiveData<List<ExcursionResponse>> = _excursions
    private val _loading = MutableLiveData(false)
    val loading: LiveData<Boolean> = _loading
    private val _error = MutableLiveData<String?>(null)
    val error: LiveData<String?> = _error
    private val _message = MutableLiveData<String?>(null)
    val message: LiveData<String?> = _message
    private val _booked = MutableLiveData<Set<Int>>(emptySet())
    val booked: LiveData<Set<Int>> = _booked
    private val _available = MutableLiveData<List<ModuleResponse>>(emptyList())
    val available: LiveData<List<ModuleResponse>> = _available

    fun load() {
        _loading.postValue(true)
        _error.postValue(null)
        // Descobre o módulo do hóspede pela reserva e busca a telemetria.
        repo.bookings { result ->
            result.onSuccess { paged ->
                val booking = paged.items.firstOrNull { it.status == "active" } ?: paged.items.firstOrNull()
                if (booking != null) {
                    _moduleName.postValue(booking.moduleName)
                    repo.readings(booking.moduleId) { r ->
                        _loading.postValue(false)
                        r.onSuccess { _readings.postValue(it) }
                            .onFailure { e -> _error.postValue(e.message) }
                    }
                } else {
                    _loading.postValue(false)
                    _moduleName.postValue(null)
                    _readings.postValue(emptyList())
                }
            }.onFailure { e ->
                _loading.postValue(false)
                _error.postValue(e.message)
            }
        }
        // Cabines disponíveis (para quem ainda não tem estadia) e excursões.
        repo.modules { result ->
            result.onSuccess { paged -> _available.postValue(paged.items.filter { it.status == "available" }) }
        }
        repo.excursions { result -> result.onSuccess { _excursions.postValue(it) } }
    }

    // Reserva uma cabine para o hóspede (datas padrão: agora ate daqui a 3 dias).
    fun bookModule(moduleId: Int) {
        val now = java.time.LocalDateTime.now()
        repo.createBooking(moduleId, now.toString(), now.plusDays(3).toString()) { result ->
            result.onSuccess { _message.postValue("Cabine reservada!"); load() }
                .onFailure { e -> _message.postValue(e.message) }
        }
    }

    fun book(excursionId: Int) {
        repo.bookExcursion(excursionId) { result ->
            result.onSuccess {
                _booked.postValue((_booked.value ?: emptySet()) + excursionId)
                _message.postValue("Excursão reservada! 🚀")
                repo.excursions { r -> r.onSuccess { list -> _excursions.postValue(list) } }
            }.onFailure { e -> _message.postValue(e.message) }
        }
    }

    // Cancela a reserva da excursão; o card volta a permitir reservar.
    fun cancel(excursionId: Int) {
        repo.cancelExcursion(excursionId) { result ->
            result.onSuccess {
                _booked.postValue((_booked.value ?: emptySet()) - excursionId)
                _message.postValue("Reserva cancelada.")
                repo.excursions { r -> r.onSuccess { list -> _excursions.postValue(list) } }
            }.onFailure { e -> _message.postValue(e.message) }
        }
    }

    fun consumeMessage() { _message.value = null }
}
