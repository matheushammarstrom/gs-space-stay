package br.com.spacestay

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.runtime.Composable
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import br.com.spacestay.data.Session
import br.com.spacestay.ui.screens.GuestStayScreen
import br.com.spacestay.ui.screens.LoginScreen
import br.com.spacestay.ui.screens.MissionControlScreen
import br.com.spacestay.ui.screens.RegisterScreen
import br.com.spacestay.ui.theme.SpaceStayTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            SpaceStayTheme { SpaceStayApp() }
        }
    }
}

@Composable
fun SpaceStayApp() {
    val nav = rememberNavController()

    NavHost(navController = nav, startDestination = "login") {
        composable("login") {
            LoginScreen(
                onLoggedIn = { isStaff ->
                    nav.navigate(if (isStaff) "staff" else "guest") {
                        popUpTo("login") { inclusive = true }
                    }
                },
                onCreateAccount = { nav.navigate("register") }
            )
        }
        composable("register") {
            RegisterScreen(
                onRegistered = {
                    nav.navigate("guest") { popUpTo("login") { inclusive = true } }
                },
                onBack = { nav.popBackStack() }
            )
        }
        composable("guest") {
            GuestStayScreen(onLogout = {
                Session.clear()
                nav.navigate("login") { popUpTo(0) }
            })
        }
        composable("staff") {
            MissionControlScreen(onLogout = {
                Session.clear()
                nav.navigate("login") { popUpTo(0) }
            })
        }
    }
}
