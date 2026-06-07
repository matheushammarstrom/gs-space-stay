package br.com.spacestay.data

import retrofit2.Call
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

// Interface Retrofit espelhando os endpoints da API. Usa Call<T> (enqueue) em vez de
// funções suspend, mantendo o consumo via callbacks e LiveData, dentro do escopo do curso.
interface ApiService {

    @POST("api/auth/login")
    fun login(@Body body: LoginRequest): Call<AuthResponse>

    @POST("api/auth/register")
    fun register(@Body body: RegisterRequest): Call<AuthResponse>

    @POST("api/bookings")
    fun createBooking(@Body body: CreateBookingRequest): Call<BookingResponse>

    @GET("api/modules")
    fun modules(@Query("pageSize") pageSize: Int = 50): Call<PagedResult<ModuleResponse>>

    @GET("api/modules/{id}/readings")
    fun readings(@Path("id") moduleId: Int): Call<List<ReadingResponse>>

    @GET("api/alerts")
    fun alerts(@Query("status") status: String? = "open"): Call<PagedResult<AlertResponse>>

    @PUT("api/alerts/{id}/acknowledge")
    fun acknowledge(@Path("id") id: Int): Call<AlertResponse>

    @GET("api/excursions")
    fun excursions(): Call<List<ExcursionResponse>>

    @POST("api/excursions/{id}/book")
    fun bookExcursion(@Path("id") id: Int): Call<BookExcursionResponse>

    @DELETE("api/excursions/{id}/book")
    fun cancelExcursion(@Path("id") id: Int): Call<ExcursionResponse>

    @GET("api/bookings")
    fun bookings(): Call<PagedResult<BookingResponse>>
}
