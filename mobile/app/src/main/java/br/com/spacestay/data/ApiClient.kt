package br.com.spacestay.data

import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

// Cliente Retrofit. No emulador Android, o host (onde roda a API .NET) é 10.0.2.2.
// Para device físico, troque por http://<IP-da-maquina>:5080/.
object ApiClient {
    const val BASE_URL = "http://10.0.2.2:5080/"

    val service: ApiService by lazy {
        val client = OkHttpClient.Builder()
            .addInterceptor { chain ->
                val builder = chain.request().newBuilder()
                Session.token?.let { builder.addHeader("Authorization", "Bearer $it") }
                chain.proceed(builder.build())
            }
            .build()

        Retrofit.Builder()
            .baseUrl(BASE_URL)
            .client(client)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(ApiService::class.java)
    }
}
