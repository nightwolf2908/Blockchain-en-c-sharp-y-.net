using System;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

public class AuthService
{
    private static string _connectionStringBlockchain = "Server=localhost,1433;Database=BlockchainAuth;User Id=sa;Password=MiContraseñaSegura123!;Encrypt=False;";
    private readonly int _p2pPort;
    private readonly Blockchain _blockchain;
    private readonly P2PServer _p2pServer;

    public AuthService(int p2pPort = 5000)
    {
        _p2pPort = p2pPort;
        _blockchain = new Blockchain();
        _blockchain.LoadFromFile(); // Cargar blockchain existente
        _p2pServer = new P2PServer(_blockchain);
    }

    public Blockchain GetBlockchain() => _blockchain;
    public P2PServer GetP2PServer() => _p2pServer;
    public int GetP2PPort() => _p2pPort;

    public void autenticacion()
    {
        bool ejecutando = true;

        while (ejecutando)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("     NODO BLOCKCHAIN - AUTENTICACIÓN     ");
            Console.WriteLine($"     🌐 P2P: ws://localhost:{_p2pPort}/ws");
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
                    _blockchain.SaveToFile();
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

    private void IniciarSesionMenu()
    {
        Console.Clear();
        Console.WriteLine("--- INICIO DE SESIÓN ---");
        Console.Write("Correo: ");
        string email = Console.ReadLine();
        
        Console.Write("Contraseña: ");
        string password = LeerContrasenaOculta();

        bool loginExitoso = new DatabaseBind().ValidarLoginEnLaBaseDeDatos(email, CryptoUtils.HashPassword(password));
        if (loginExitoso)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✅ ¡Acceso concedido! Bienvenido al Nodo Blockchain.");
            Console.ReadKey();
            Console.ResetColor();

            UsuarioSesion usuarioSesion = new UsuarioSesion().ObtenerDatosSesion(email);
            if (usuarioSesion != null)
            {
                // Pasar la misma instancia de blockchain y p2pserver
                PostLoginMenu postLoginMenu = new PostLoginMenu(usuarioSesion, _blockchain, _p2pServer);
                postLoginMenu.Mostrar();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n❌ Correo o contraseña incorrectos. Presiona cualquier tecla para volver...");
            Console.ReadKey();
            Console.ResetColor();
        }
    }

    private void RegistrarMenu()
    {
        CodigoOTP codigoOTP = new CodigoOTP();

        Console.Clear();
        Console.WriteLine("--- REGISTRO DE NUEVO USUARIO ---");
        Console.Write("Introduce tu correo: ");
        string email = Console.ReadLine();

        if (!EsCorreoValido(email))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Correo inválido. Presiona cualquier tecla para volver...");
            Console.ReadKey();
            Console.ResetColor();
            return;
        }

        Console.Write("Introduce tu contraseña: ");
        string password = LeerContrasenaOculta();
        Console.Write("\nEscribe de nuevo tu contraseña para confirmar: ");
        string confirmPassword = LeerContrasenaOculta();
        if (password != confirmPassword)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n❌ Las contraseñas no coinciden. Presiona cualquier tecla para volver...");
            Console.ReadKey();
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✅ ¡Correo y contraseña válidos en formato!");
        Console.ResetColor();

        string codigoToken = codigoOTP.GenerarCodigoVerificacion();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[Procesando] Enviando código de verificación a tu correo...");
        Console.ResetColor();

        bool envioExitoso = codigoOTP.EnviarCorreoReal(email, codigoToken).GetAwaiter().GetResult();
        if (!envioExitoso)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n❌ Error al enviar el correo de verificación.");
            Console.ReadKey();
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ Código enviado a {email}");
        Console.ResetColor();

        Console.Write("\nIntroduce el código de 6 dígitos recibido: ");
        string codigoIntroducido = Console.ReadLine();

        if (codigoOTP.validarCodigo(codigoIntroducido))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✅ ¡Registro exitoso!");
            Console.ReadKey();
            Console.ResetColor();

            bool guardadoExitoso = new DatabaseBind().GuardarUsuarioEnLaBaseDeDatos(email, CryptoUtils.HashPassword(password));
            if (guardadoExitoso)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Usuario guardado en la base de datos.");
                Console.ResetColor();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n❌ Código inválido. Registro fallido.");
            Console.ReadKey();
            Console.ResetColor();
        }
    }

    private static bool EsCorreoValido(string correo)
    {
        string modeloRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(correo, modeloRegex);
    }

    private static string LeerContrasenaOculta()
    {
        string pass = "";
        ConsoleKeyInfo key;

        while (true)
        {
            key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (pass.Length > 0)
                {
                    pass = pass.Substring(0, (pass.Length - 1));
                    Console.Write("\b \b");
                }
            }
            else
            {
                if (!char.IsControl(key.KeyChar))
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