using System;

public class PostLoginMenu
{
    private readonly UsuarioSesion _usuarioSesion;
    private readonly Blockchain _blockchain;
    public PostLoginMenu(UsuarioSesion usuarioSesion, Blockchain blockchain)
    {
        _usuarioSesion = usuarioSesion;
        _blockchain = blockchain;
    }

    public void Mostrar()
    {
        bool enSesion = true;
        while (enSesion)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("========================================");
            Console.WriteLine("     BIENVENIDO AL NODO BLOCKCHAIN      ");
            Console.WriteLine("========================================");
            Console.ResetColor();

            Console.WriteLine($"Usuario: {_usuarioSesion.Email}");
        }
    }
}


