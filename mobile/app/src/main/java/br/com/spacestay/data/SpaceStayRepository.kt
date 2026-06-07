package br.com.spacestay.data

import retrofit2.Call
import retrofit2.Callback
import retrofit2.Response

// Erro de API com o código HTTP, mapeado para uma mensagem amigável.
class ApiException(val code: Int, message: String) : Exception(message)

// Repositório do app: encapsula o ApiService e entrega o resultado por callback
// (kotlin.Result), traduzindo os erros HTTP em mensagens. As ViewModels usam isto.
class SpaceStayRepository(private val api: ApiService = ApiClient.service) {

    private fun <T> enqueue(call: Call<T>, onResult: (Result<T>) -> Unit) {
        call.enqueue(object : Callback<T> {
            override fun onResponse(call: Call<T>, response: Response<T>) {
                val body = response.body()
                if (response.isSuccessful && body != null) {
                    onResult(Result.success(body))
                } else {
                    val msg = when (response.code()) {
                        401 -> "E-mail ou senha inválidos."
                        403 -> "Acesso negado para o seu perfil."
                        404 -> "Recurso não encontrado."
                        409 -> "Operação em conflito (sem vaga ou duplicada)."
                        else -> "Erro ${response.code()}."
                    }
                    onResult(Result.failure(ApiException(response.code(), msg)))
                }
            }

            override fun onFailure(call: Call<T>, t: Throwable) {
                onResult(Result.failure(ApiException(0, "Falha de conexão. A API está rodando?")))
            }
        })
    }

    fun login(email: String, password: String, onResult: (Result<AuthResponse>) -> Unit) =
        enqueue(api.login(LoginRequest(email, password)), onResult)

    fun register(name: String, email: String, password: String, nationality: String?,
                 medicalClearance: Boolean, onResult: (Result<AuthResponse>) -> Unit) =
        enqueue(api.register(RegisterRequest(name, email, password, nationality, medicalClearance)), onResult)

    fun createBooking(moduleId: Int, checkIn: String, checkOut: String,
                      onResult: (Result<BookingResponse>) -> Unit) =
        enqueue(api.createBooking(CreateBookingRequest(moduleId, checkIn, checkOut)), onResult)

    fun modules(onResult: (Result<PagedResult<ModuleResponse>>) -> Unit) =
        enqueue(api.modules(), onResult)

    fun readings(moduleId: Int, onResult: (Result<List<ReadingResponse>>) -> Unit) =
        enqueue(api.readings(moduleId), onResult)

    fun openAlerts(onResult: (Result<PagedResult<AlertResponse>>) -> Unit) =
        enqueue(api.alerts("open"), onResult)

    fun acknowledge(id: Int, onResult: (Result<AlertResponse>) -> Unit) =
        enqueue(api.acknowledge(id), onResult)

    fun excursions(onResult: (Result<List<ExcursionResponse>>) -> Unit) =
        enqueue(api.excursions(), onResult)

    fun bookExcursion(id: Int, onResult: (Result<BookExcursionResponse>) -> Unit) =
        enqueue(api.bookExcursion(id), onResult)

    fun cancelExcursion(id: Int, onResult: (Result<ExcursionResponse>) -> Unit) =
        enqueue(api.cancelExcursion(id), onResult)

    fun bookings(onResult: (Result<PagedResult<BookingResponse>>) -> Unit) =
        enqueue(api.bookings(), onResult)
}
