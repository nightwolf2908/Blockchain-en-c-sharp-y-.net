using System;
using System.Text.RegularExpressions;

class AuthService
{
    static void Main(string[] args)
    {
        bool ejecutando = true;

        while (ejecutando)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("     NODO BLOCKCHAIN - AUTENTICACIÓN     ");
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.WriteLine("1. Iniciar Sesión");
            Console.WriteLine("2. Registrarse");
            Console.WriteLine("3. Salir");
            Console.Write("\nSelecciona una opción: ");

            string opcion = Console.ReadLine();

            switch (opcion)
            {
                case "1":
                    IniciarSesionMenu();
                    break;
                case "2":
                    RegistrarMenu();
                    break;
                case "3":
                    ejecutando = false;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Opción inválida. Presiona cualquier tecla para continuar...");
                    Console.ReadKey();
                    Console.ResetColor();
                    break;
            }
        }
    }

    static void IniciarSesionMenu()
    {
        Console.Clear();
        Console.WriteLine("--- INICIO DE SESIÓN ---");
        Console.Write("Correo: ");
        string email = Console.ReadLine();
        Console.Write("Contraseña: ");
        string password = LeerContrasenaOculta();

        // TODO: Validar contra la base de datos de Docker más adelante
        Console.WriteLine("\n[Funcionalidad en desarrollo] Presiona una tecla para volver...");
        Console.ReadKey();
    }

    static void RegistrarMenu()
    {
        Console.Clear();
        Console.WriteLine("--- REGISTRO DE NUEVO USUARIO ---");
        Console.Write("Introduce tu correo: ");
        string email = Console.ReadLine();

        if (!EsCorreoValido(email))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Correo inválido. Presiona cualquier tecla para volver...");
            Console.ReadKey();
            Console.ResetColor();
            return;
        }

        Console.Write("Introduce tu contraseña: ");
        string password = LeerContrasenaOculta();
        Console.Write("\nEscribe de nuevo tu contraseña para confirmar: ");
        string confirmPassword = LeerContrasenaOculta();
        if(password != confirmPassword)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nLas contraseñas no coinciden. Presiona cualquier tecla para volver...");
            Console.ReadKey();
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n¡Correo y contraseña válidos en formato!");
        Console.ResetColor();

        // Aquí es donde dispararemos el código de 5 minutos en el Paso 2
        Console.WriteLine("Presiona una tecla para continuar...");
        Console.ReadKey();

        static bool EsCorreoValido(string correo)
        {
            string modeloRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(correo, modeloRegex);
        }
    }

    static string LeerContrasenaOculta()
    {
        string pass = "";
        ConsoleKeyInfo key;

        while (true)
        {
            key = Console.ReadKey(true);

            if(key.Key == ConsoleKey.Enter)
            {
                break;
            }
            if(key.Key == ConsoleKey.Backspace)
            {
                if(pass.Length > 0)
                {
                    pass = pass.Substring(0,(pass.Length-1));
                    Console.Write("\b \b");
                }
            }
            else
            {
                if(!char.IsControl(key.KeyChar))
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
            }
        }
        Console.WriteLine();
        return pass;
    }
}