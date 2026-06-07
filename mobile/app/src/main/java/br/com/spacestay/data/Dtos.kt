package br.com.spacestay.data

// DTOs que espelham as respostas JSON da API (camelCase). Os enums chegam como String
// e são interpretados na UI (mantém o mapeamento simples).

data class LoginRequest(val email: String, val password: String)

data class RegisterRequest(
    val name: String,
    val email: String,
    val password: String,
    val nationality: String?,
    val medicalClearance: Boolean
)

data class CreateBookingRequest(
    val moduleId: Int,
    val checkIn: String,
    val checkOut: String
)

data class AuthResponse(
    val token: String,
    val expiresAt: String,
    val userType: String,   // "guest" | "staff"
    val role: String?,      // admin | engineer | concierge (equipe)
    val id: Int,
    val name: String
)

data class PagedResult<T>(
    val items: List<T> = emptyList(),
    val page: Int = 1,
    val pageSize: Int = 0,
    val total: Int = 0,
    val totalPages: Int = 0
)

data class ModuleResponse(
    val id: Int,
    val name: String,
    val type: String,       // suite | common | lab
    val capacity: Int,
    val status: String,     // available | occupied | maintenance
    val openAlerts: Int
)

data class ReadingResponse(
    val sensorId: Int,
    val type: String,       // o2 | co2 | pressure | temperature | humidity | water
    val unit: String,
    val value: Double?,
    val recordedAt: String?,
    val minSafe: Double,
    val maxSafe: Double,
    val status: String,     // safe | warning | critical | no-data
    val comfort: String     // Ideal | Atenção | Crítico | Sem dados
)

data class AlertResponse(
    val id: Int,
    val sensorId: Int,
    val sensorType: String,
    val moduleId: Int,
    val moduleName: String?,
    val severity: String,   // info | warning | critical
    val message: String,
    val status: String,     // open | acknowledged | resolved
    val createdAt: String,
    val resolvedBy: Int?,
    val resolvedByName: String?
)

data class ExcursionResponse(
    val id: Int,
    val name: String,
    val description: String?,
    val capacity: Int,
    val scheduledAt: String,
    val bookedCount: Int,
    val availableSpots: Int,
    val bookedByMe: Boolean = false
)

data class BookExcursionResponse(
    val excursionBookingId: Int,
    val excursionId: Int,
    val guestId: Int,
    val status: String
)

data class BookingResponse(
    val id: Int,
    val guestId: Int,
    val guestName: String?,
    val moduleId: Int,
    val moduleName: String?,
    val checkIn: String,
    val checkOut: String,
    val status: String      // confirmed | active | completed
)
