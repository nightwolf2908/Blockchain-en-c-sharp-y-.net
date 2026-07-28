using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class Blockchain
{
    public List<Block> Chain {get; private set;}
    public List<Transaction> PendingTransactions {get; private set;}
    public int Difficulty {get; set;} = 2;
    public decimal MiningReward {get; set;} = 10;
    private const int DifficultyAdjustmentInterval = 5;
    private const int TargetTimePerBlockSeconds = 10;
    private const int TargetTimespan = DifficultyAdjustmentInterval * TargetTimePerBlockSeconds;

    public Blockchain()
    {
        Chain = new List<Block>();
        PendingTransactions = new List<Transaction>();
        Chain.Add(new Block(0, DateTime.Now, new List<Transaction>(), "0"));
    }

    

    public Block GetLatestBlock()
    {
        return Chain[Chain.Count - 1];
    }

    public void CreateTransaction(Transaction transaction)
    {
        if (!transaction.IsValid())
        {
            throw new InvalidOperationException("Transacción rechazada: Firma digital inválida.");
        }

        if(transaction.Sender != "Sistema")
        {
            decimal balance = GetBalance(transaction.Sender);
            if(balance < transaction.Amount)
            {
                throw new InvalidOperationException("Transacción rechazada: Fondos insuficientes.");
            }
        }
        PendingTransactions.Add(transaction);
        Console.WriteLine($"Transacción aceptada en el Mempool.");
    }

    public void MinePendingTransactions(string minerAddress)
    {
        

        Console.WriteLine($"\nIniciando minado de un nuevo bloque con {PendingTransactions.Count} transacciones...");

        Block newBlock = new Block(Chain.Count,DateTime.Now, new List<Transaction>(PendingTransactions), GetLatestBlock().Hash,this.Difficulty);
        newBlock.MineBlock(Difficulty);
        Chain.Add(newBlock);

        if(Chain.Count % DifficultyAdjustmentInterval == 0)
        {
            AdjustDifficulty();
        }

        PendingTransactions.Clear();
        Console.WriteLine("Bloque minado y agregado a la cadena de bloques.");

        Console.WriteLine($"Recompensa de minado: {MiningReward} enviada a {minerAddress}");
        PendingTransactions.Add(new Transaction("Sistema", minerAddress, MiningReward));
    }

    private void AdjustDifficulty()
    {
        var lastAdjustmentBlock = Chain.Skip(Chain.Count - DifficultyAdjustmentInterval);
        long actualTimespan = lastAdjustmentBlock.Sum(b => b.MiningDurationSecond);
        if(actualTimespan < TargetTimespan / 2)
        {
            Difficulty++;
            Console.WriteLine($"[SISTEMA]: Dificultad incrementada a {Difficulty}.");
        }
        else if(actualTimespan > TargetTimespan * 2)
        {
            Difficulty--;
            Console.WriteLine($"[SISTEMA]: Dificultad decrementada a {Difficulty}.");
        }
        else
        {
            Console.WriteLine($"[SISTEMA]: Dificultad permanece en {Difficulty}.");
        }
    }

    public decimal GetBalance(string address)
    {
        decimal balance = 0;
        foreach(Block block in Chain)
        {
            foreach(Transaction transaction in block.Transactions)
            {
                if(transaction.Sender == address)
                {
                    balance -= transaction.Amount;
                }
                if(transaction.Receiver == address)
                {
                    balance += transaction.Amount;
                }
            }
        }
        return balance;
    }

    public void AddBlock(Block newBlock)
    {
        newBlock.PreviousHash = GetLatestBlock().Hash;
        newBlock.MineBlock(Difficulty);
        Chain.Add(newBlock);
    }

    public bool IsValid()
    {
        Block genesis = Chain[0];
        if(genesis.Hash != genesis.CalculateHash() || genesis.PreviousHash != "0") return false;
        for(int i = 1; i<Chain.Count; i++)
        {
            Block currentBlock = Chain[i];
            Block previousBlock = Chain[i-1];

            if(currentBlock.Hash != currentBlock.CalculateHash()) return false;
            if(currentBlock.PreviousHash != previousBlock.Hash) return false;

            string target = new string('0', currentBlock.BlockDifficulty);
            if(currentBlock.Hash.Substring(0, currentBlock.BlockDifficulty) != target)
            {
                Console.WriteLine($"[ALERTA]: Bloque {currentBlock.Index} no cumple con la dificultad requerida.");
                return false;
            }

            foreach(Transaction tx in currentBlock.Transactions)
            {
                if(!tx.IsValid()) return false;
            }
        }
        return true;
    }

    private const string FilePath = "blockchain_data.json";

    public void SaveToFile()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

            string jsonString = JsonSerializer.Serialize(this.Chain, options);
            System.IO.File.WriteAllText("blockchain.json", jsonString);
            Console.WriteLine("[SISTEMA]: Blockchain guardada exitosamente en 'blockchain.json'.");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[ERROR AL GUARDAR]: {ex.Message}");
        }
    }

    public void LoadFromFile()
    {
        string filePath = "blockchain.json";
        if (!File.Exists(filePath))
        {
            Console.WriteLine("[SISTEMA]: No se encontró el archivo 'blockchain.json'. Se iniciará una nueva cadena de bloques.");
            this.Chain = new List<Block>();
            var genesisTransactions = new List<Transaction>();
            Block genesisBlock = new Block(0, DateTime.Now, genesisTransactions, "0", this.Difficulty);
            genesisBlock.MineBlock(this.Difficulty);
            this.Chain.Add(genesisBlock);
            this.SaveToFile();
            return;
        }
        try
        {
            Console.WriteLine("[Persistencia] Cargando el historial de bloques desde disco...");
            string jsonString = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var loadedChain = JsonSerializer.Deserialize<List<Block>>(jsonString, options);
            if (loadedChain != null && loadedChain.Count > 0)
            {
                this.Chain = loadedChain;
                Console.Write("[Validación] Ejecutando auditoría exhaustiva de la cadena cargada... ");
                if (this.IsValid())
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("¡ÉXITO! Historial íntegro, hashes correctos y firmas válidas.");
                    Console.ResetColor();

                    this.Difficulty = this.GetLatestBlock().BlockDifficulty;
                    Console.WriteLine($"[SISTEMA]: Dificultad ajustada a {this.Difficulty} según el último bloque cargado.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("¡ALERTA CRÍTICA! El archivo JSON está corrupto o fue manipulado.");
                    Console.ResetColor();

                    throw new System.Security.SecurityException("La cadena de bloques cargada no es válida. Se requiere intervención manual.");
                }

            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[Error Inicio] Fallo catastrófico al cargar el archivo: {ex.Message}");
            Environment.Exit(1);
        }
    }
}