package br.com.spacestay.data

// Sessão simples em memória: token JWT e dados do usuário logado.
// O interceptor do OkHttp (ApiClient) anexa o token às requisições protegidas.
object Session {
    var token: String? = null
    var userType: String? = null
    var userId: Int = 0
    var userName: String? = null

    val isStaff: Boolean get() = userType == "staff"

    fun set(auth: AuthResponse) {
        token = auth.token
        userType = auth.userType
        userId = auth.id
        userName = auth.name
    }

    fun clear() {
        token = null; userType = null; userId = 0; userName = null
    }
}
