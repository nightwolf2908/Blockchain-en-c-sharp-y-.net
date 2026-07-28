using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("  SIMULADOR DE BLOCKCHAIN CON DIFICULTAD DINÁMICA");
        Console.WriteLine("==================================================\n");

        Blockchain blockchain = new Blockchain();
        
        using ECDsa minerKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa userKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        string minerAddress = Convert.ToHexString(minerKey.ExportSubjectPublicKeyInfo());
        string userAddress = Convert.ToHexString(userKey.ExportSubjectPublicKeyInfo());

        Console.WriteLine("[Billeteras] Llaves criptográficas generadas e indexadas correctamente.");
        Console.WriteLine($"   -> Miner Address (Hex): {minerAddress.Substring(0, 20)}...");
        Console.WriteLine($"   -> User Address (Hex):  {userAddress.Substring(0, 20)}...\n");

        Console.WriteLine("[Fondeo] Generando bloques iniciales para obtener recompensas de sistema...");

        blockchain.MinePendingTransactions(minerAddress);
        blockchain.MinePendingTransactions(minerAddress);

        Console.WriteLine($"   -> Fondeo exitoso. Procesando bucle de dificultad...\n");

        for(int i = 1; i<=9; i++)
        {
            Console.WriteLine($"--------------------------------------------------");
            Console.WriteLine($"[Bloque Dinámico #{i}]");

            try
            {
                Transaction tx = new Transaction(minerAddress, userAddress, 1);

                tx.SignTransaction(minerKey);

                blockchain.CreateTransaction(tx);
                Console.WriteLine("   [Mempool] Transacción firmada y validada con éxito.");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[Error Mempool] La transacción fue rechazada por el sistema: {ex.Message}");
            }

            Console.WriteLine($"   [Minando] Ejecutando MinePendingTransactions...");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            blockchain.MinePendingTransactions(minerAddress);
            watch.Stop();

            Block latestBlock = blockchain.GetLatestBlock();
            Console.WriteLine($"   [Resultado] Bloque #{latestBlock.Index} cerrado.");
            Console.WriteLine($"      -> Dificultad Histórica: {latestBlock.BlockDifficulty} ceros.");
            Console.WriteLine($"      -> Tiempo registrado: {latestBlock.MiningDurationSecond}s.");

            Console.Write($"   [Validación IsValid] Verificando estado criptográfico... ");
            if (blockchain.IsValid())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Cadena de bloques válida.");
                Console.ResetColor();

                Console.WriteLine("   [Persistencia] Guardando estado actual en blockchain.json...");
                blockchain.SaveToFile();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Cadena de bloques inválida.");
                Console.ResetColor();
            }

            if(i>=3 && i <= 5)
            {
                Console.WriteLine("   [Simulación] Forzando retraso de hardware de 3 segundos...");
                Thread.Sleep(3000);
            }
            else
            {
                Thread.Sleep(100);
            }
            Console.WriteLine();

        }
        
        
        
        }    
}