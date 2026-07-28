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
        }
        
        
        }    
}