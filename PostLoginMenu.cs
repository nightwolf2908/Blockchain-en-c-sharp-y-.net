using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using System.Net.WebSockets;

public class PostLoginMenu
{
    private readonly UsuarioSesion _usuarioSesion;
    private readonly Blockchain _blockchain;
    private readonly P2PServer _p2pServer;
    private readonly string _connectionStringBlockchain = "Server=localhost,1433;Database=BlockchainAuth;User Id=sa;Password=MiContraseñaSegura123!;Encrypt=False;";
    public PostLoginMenu(UsuarioSesion usuarioSesion, Blockchain blockchain, P2PServer p2pServer)
    {
        _usuarioSesion = usuarioSesion;
        _blockchain = blockchain;
        _p2pServer = p2pServer;

        Console.WriteLine("Cargando blockchain...");
        _blockchain.LoadFromFile(); // Método sincrónico con mensajes
        Console.WriteLine("✅ Blockchain cargada.");
        Console.WriteLine("Presiona una tecla para continuar...");
        Console.ReadKey();

    }

    private void GuardarBlockchain()
    {
        _blockchain.SaveToFile();
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
                    EnviarTransaccion().GetAwaiter().GetResult();
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

    public bool WalletExists(string publicKey)
    {
        try
        {
            using(SqlConnection connection = new SqlConnection(_connectionStringBlockchain))
            {
                connection.Open();
                using(SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM Usuarios WHERE PublicKey = @PublicKey", connection))
                {
                    command.Parameters.AddWithValue("@PublicKey", publicKey);
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }
        catch(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Error DB Validación] No se pudo verificar la Wallet: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    private async Task EnviarTransaccion()
    {
        Console.Clear();
        Console.WriteLine("--- ENVIAR NUEVA TRANSACCIÓN ---");
        Console.Write("Introduce la dirección de la Wallet destino (Llave Pública): ");
        string destino = Console.ReadLine();
        Console.Write("Verficando que exista la Wallet destino en la Blockchain... ");
        await Task.Delay(1000);
        if (!WalletExists(destino))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[Error] La Wallet destino no existe en la Blockchain. Verifica la dirección.");
            Console.ResetColor();
            return;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[ÉXITO] Wallet destino verificada en la Blockchain.");
            Console.ResetColor();
        }
        Console.Write("Introduce la cantidad a enviar: ");

        if (decimal.TryParse(Console.ReadLine(), out decimal monto))
        {
            // TODO: Aquí enlazaremos la Fase 2 (Importar tu llave privada, crear la Tx, firmarla y agregarla)
            if(monto <= 0 || monto > (decimal)_blockchain.GetBalance(_usuarioSesion.PublicKey))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nMonto inválido o insuficiente balance.");
                Console.ResetColor();
                Console.WriteLine("\nPresiona cualquier tecla para volver...");
                Console.ReadKey();
                return;
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nPreparando envío de {monto} monedas hacia la wallet destino...");
            Console.ResetColor();
            try{
            Transaction nuevaTransaccion = new Transaction(_usuarioSesion.PublicKey, destino, (decimal)monto);
            byte[] privateKeyBytes = Convert.FromBase64String(_usuarioSesion.PrivateKey);
            using(ECDsa ecdsa = ECDsa.Create())
                {
                    ecdsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
                    nuevaTransaccion.SignTransaction(ecdsa);
                }

            if (nuevaTransaccion.IsValid())
            {
                _blockchain.CreateTransaction(nuevaTransaccion);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n¡Transacción creada y firmada con éxito! Ahora puedes minar para incluirla en un bloque.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[Error] La transacción no es válida. Verifica los datos e intenta nuevamente.");
                Console.ResetColor();
            }
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[Error] Ocurrió un problema al crear o firmar la transacción: {ex.Message}");
                Console.ResetColor();
            }
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

        _blockchain.MinePendingTransactions(_usuarioSesion.PublicKey, _p2pServer).GetAwaiter().GetResult();

        GuardarBlockchain();

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
        Console.WriteLine($"Total de nodos conectados: {P2PServer.GetConnectedSockets().Count}");
        Console.WriteLine(new string('-', 40));

        var sockets = P2PServer.GetConnectedSockets();
        var identifiers = P2PServer.GetNodeIdentifiers();

        if(sockets.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nNo hay nodos P2P conectados actualmente.");
            Console.ResetColor();
        }
        else
        {
            int index = 1;
            foreach(var socket in sockets)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\nNodo #{index}:");
                Console.ResetColor();

                if(identifiers.TryGetValue(socket,out var nodeId))
                {
                    Console.WriteLine($"  ID: {nodeId}");
                }

                Console.WriteLine($"  Estado: {socket.State}");
                Console.WriteLine($"  Conexión: {(socket.State == WebSocketState.Open ? "🟢 Activa" : "🔴 Cerrada")}");
                index++;
            }
        }
        Console.WriteLine("\n" + new string('-', 40));
        Console.WriteLine("Presiona una tecla para continuar...");
        Console.ReadKey();
    }

    private void ConectarseAServidorP2P()
    {
        Console.Clear();
        Console.WriteLine("--- CONECTARSE A UN NODO P2P ---");
        Console.WriteLine("\nIngresa la URL del nodo al que deseas conectarte:");
        Console.WriteLine("Ejemplo: ws://localhost:5000/ws");
        Console.Write("\nURL: ");

        string url = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(url))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n❌ URL no válida. Operación cancelada.");
            Console.ResetColor();
            Console.WriteLine("\nPresiona una tecla para continuar...");
            Console.ReadKey();
            return;
        }

        try
        {
            Console.WriteLine($"\nConectando a {url}...");
            P2PClient p2pClient = new P2PClient(_blockchain);
            p2pClient.ConnectToPeer(url).GetAwaiter().GetResult();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✅ Conexión exitosa al nodo P2P.");
            Console.ResetColor();
        }
        catch(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Error al conectarse al nodo P2P: {ex.Message}");
            Console.ResetColor();
        }
        Console.WriteLine("\nPresiona una tecla para continuar...");
        Console.ReadKey();

    }
}


