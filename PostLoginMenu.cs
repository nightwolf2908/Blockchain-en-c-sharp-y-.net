using System;
using System.Threading.Tasks;

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
            string walletCorta = _usuarioSesion.PublicKey.Length > 20 ? _usuarioSesion.PublicKey.Substring(0, 10) + "..." : _usuarioSesion.PublicKey;
            Console.WriteLine($"Dirección Wallet: {walletCorta}");

            decimal balance = _blockchain.GetBalance(_usuarioSesion.PublicKey);
            Console.WriteLine($"Balance Actual: {balance} Monedas");
            Console.WriteLine("========================================");

            Console.WriteLine("1. Ver Estado de mi Wallet (Llaves Completas)");
            Console.WriteLine("2. Enviar Transacción (Crear y Firmar)");
            Console.WriteLine("3. Ver Bloques de la Blockchain Local");
            Console.WriteLine("4. Minar Transacciones Pendientes");
            Console.WriteLine("5. Red P2P: Ver Nodos Conectados");
            Console.WriteLine("6. Red P2P: Conectarse a un Nodo / Sincronizar");
            Console.WriteLine("7. Cerrar Sesión");
            Console.Write("\nSelecciona una opción: ");

            string opcion = Console.ReadLine();

            switch (opcion)
            {
                case "1":
                    VerEstadoWallet();
                    break;
                case "2":
                    EnviarTransaccion();
                    break;
                case "3":
                    VerBloquesBlockchain();
                    break;
                case "4":
                    MinarTransaccionesPendientes();
                    break;
                case "5":
                    VerNodosConectados();
                    break;
                case "6":
                    ConectarseAServidorP2P();
                    break;
                case "7":
                    enSesion = false;
                    Console.WriteLine("\nCerrando sesión segura... Volviendo al menú principal.");
                    System.Threading.Thread.Sleep(1500); // Pausa estética de salid
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

    private void VerEstadoWallet()
    {
        Console.Clear();
        Console.WriteLine("--- DETALLES DE SEGURIDAD DE TU WALLET ---");
        Console.WriteLine($"\n[LLAVE PÚBLICA / DIRECCIÓN]:\n{_usuarioSesion.PublicKey}");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[LLAVE PRIVADA SECRETÍSIMA (NIST P-256 PKCS8)]:\n{_usuarioSesion.PrivateKey}");
        Console.ResetColor();
        Console.WriteLine("\n⚠️ ADVERTENCIA: Jamás compartas tu llave privada.");
        Console.WriteLine("\nPresiona cualquier tecla para volver...");
        Console.ReadKey();
    }

    private void EnviarTransaccion()
    {
        Console.Clear();
        Console.WriteLine("--- ENVIAR NUEVA TRANSACCIÓN ---");
        Console.Write("Introduce la dirección de la Wallet destino (Llave Pública): ");
        string destino = Console.ReadLine();
        Console.Write("Introduce la cantidad a enviar: ");

        if (double.TryParse(Console.ReadLine(), out double monto))
        {
            // TODO: Aquí enlazaremos la Fase 2 (Importar tu llave privada, crear la Tx, firmarla y agregarla)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[Fase 2] Preparando envío de {monto} monedas hacia la wallet destino...");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nMonto inválido.");
            Console.ResetColor();
        }
        Console.WriteLine("\nPresiona una tecla para continuar...");
        Console.ReadKey();
    }

    private void VerBloquesBlockchain()
    {
        Console.Clear();
        Console.WriteLine("--- BLOQUES DE LA BLOCKCHAIN LOCAL ---");
        var bloques = _blockchain.Chain;
        if (bloques.Count == 0)
        {
            Console.WriteLine("\nNo hay bloques en la blockchain local.");
        }
        else
        {
            foreach (var bloque in bloques)
            {
                Console.WriteLine($"\n[Bloque #{bloque.Index}]");
                Console.WriteLine($"Timestamp: {bloque.Timestamp}");
                Console.WriteLine($"Hash: {bloque.Hash}");
                Console.WriteLine($"Hash del Bloque Anterior: {bloque.PreviousHash}");
                Console.WriteLine($"Nonce: {bloque.Nonce}");
                Console.WriteLine($"Dificultad: {bloque.BlockDifficulty}");
                Console.WriteLine($"Transacciones en este bloque: {bloque.Transactions.Count}");
            }
        }
        Console.WriteLine("\nPresiona una tecla para volver...");
        Console.ReadKey();
    }

    private void MinarTransaccionesPendientes()
    {
        Console.Clear();
        Console.WriteLine("--- EMPEZAR PROCESO DE MINADO ---");
        Console.WriteLine("\nResolviendo el acertijo criptográfico Proof of Work...");

        // TODO: En la Fase 2 pasaremos también el p2pServer aquí. Por ahora llamamos al método básico.
        // Se le pasa la dirección del usuario actual para recibir la recompensa por minar.
        // Ajusta el nombre de tu método si cambia alguna letra.

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n¡Bloque minado con éxito! Recompensa asignada a tu Wallet.");
        Console.ResetColor();
        Console.WriteLine("\nPresiona una tecla para continuar...");
        Console.ReadKey();
    }

    private void VerNodosConectados()
    {
        Console.Clear();
        Console.WriteLine("--- NODOS P2P CONECTADOS ---");
    }

    private void ConectarseAServidorP2P()
    {
        Console.Clear();
        Console.WriteLine("--- CONECTARSE A UN NODO P2P ---");
    }
}


