using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class Blockchain
{
    public List<Block> Chain { get; set; }
    public List<Transaction> PendingTransactions { get; set; }
    public int Difficulty { get; set; } = 4;
    public decimal MiningReward { get; set; } = 10;
    private const int DifficultyAdjustmentInterval = 5;
    private const int TargetTimePerBlockSeconds = 10;
    private const int TargetTimespan = DifficultyAdjustmentInterval * TargetTimePerBlockSeconds;
    private const string FilePath = "blockchain_data.json";
    private static readonly object _fileLock = new object();

    public Blockchain()
    {
        Chain = new List<Block>();
        PendingTransactions = new List<Transaction>();
        Chain.Add(new Block(0, DateTime.Now, new List<Transaction>(), "0", Difficulty));
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

        if (transaction.Sender != "Sistema")
        {
            decimal balance = GetBalance(transaction.Sender);
            if (balance < transaction.Amount)
            {
                throw new InvalidOperationException("Transacción rechazada: Fondos insuficientes.");
            }
        }
        PendingTransactions.Add(transaction);
        Console.WriteLine($"✅ Transacción aceptada en el Mempool.");
    }

    public async Task MinePendingTransactions(string minerAddress, P2PServer p2pserver)
    {

        Console.WriteLine($"\n⛏️ Iniciando minado de un nuevo bloque con {PendingTransactions.Count} transacciones...");

        Block newBlock = new Block(
            Chain.Count,
            DateTime.Now,
            new List<Transaction>(PendingTransactions),
            GetLatestBlock().Hash,
            Difficulty
        );
        
        newBlock.MineBlock(Difficulty);
        Chain.Add(newBlock);

        if (Chain.Count % DifficultyAdjustmentInterval == 0)
        {
            AdjustDifficulty();
        }

        PendingTransactions.Clear();
        Console.WriteLine($"✅ Bloque {newBlock.Index} minado y agregado a la cadena.");

        // Agregar recompensa
        Console.WriteLine($"💰 Recompensa de minado: {MiningReward} enviada a {minerAddress}");
        var rewardTransaction = new Transaction("Sistema", minerAddress, MiningReward);
        PendingTransactions.Add(rewardTransaction);

        // Guardar en disco
        SaveToFile();

        // Broadcast a todos los nodos
        var message = JsonSerializer.Serialize(new { Type = "NEW_BLOCK", Data = newBlock });
        await p2pserver.BroadcastMessage(message);

        Console.WriteLine($"📡 Bloque broadcast a {P2PServer.GetConnectedSockets().Count} nodos.");
    }

    private void AdjustDifficulty()
    {
        if (Chain.Count < DifficultyAdjustmentInterval + 1) return;

        var lastAdjustmentBlock = Chain[Chain.Count - DifficultyAdjustmentInterval];
        var currentBlock = GetLatestBlock();
        
        long actualTimespan = (long)(currentBlock.Timestamp - lastAdjustmentBlock.Timestamp).TotalSeconds;
        
        if (actualTimespan < TargetTimespan / 2)
        {
            Difficulty++;
            Console.WriteLine($"[SISTEMA]: Dificultad incrementada a {Difficulty}.");
        }
        else if (actualTimespan > TargetTimespan * 2)
        {
            Difficulty = Math.Max(1, Difficulty - 1);
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
        foreach (Block block in Chain)
        {
            foreach (Transaction transaction in block.Transactions)
            {
                if (transaction.Sender == address)
                {
                    balance -= transaction.Amount;
                }
                if (transaction.Receiver == address)
                {
                    balance += transaction.Amount;
                }
            }
        }
        // También verificar transacciones pendientes
        foreach (Transaction transaction in PendingTransactions)
        {
            if (transaction.Sender == address)
            {
                balance -= transaction.Amount;
            }
            if (transaction.Receiver == address)
            {
                balance += transaction.Amount;
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
        if (Chain.Count == 0) return false;

        Block genesis = Chain[0];
        if (genesis.Hash != genesis.CalculateHash() || genesis.PreviousHash != "0") 
            return false;

        for (int i = 1; i < Chain.Count; i++)
        {
            Block currentBlock = Chain[i];
            Block previousBlock = Chain[i - 1];

            if (currentBlock.Hash != currentBlock.CalculateHash()) 
                return false;
            
            if (currentBlock.PreviousHash != previousBlock.Hash) 
                return false;

            string target = new string('0', currentBlock.BlockDifficulty);
            if (currentBlock.Hash.Substring(0, currentBlock.BlockDifficulty) != target)
            {
                Console.WriteLine($"[ALERTA]: Bloque {currentBlock.Index} no cumple con la dificultad requerida.");
                return false;
            }

            foreach (Transaction tx in currentBlock.Transactions)
            {
                if (!tx.IsValid()) 
                    return false;
            }
        }
        return true;
    }

    public void ReplaceChain(List<Block> newChain)
    {
        if (newChain.Count <= Chain.Count) return;
        
        var tempBlockchain = new Blockchain();
        tempBlockchain.Chain = newChain;
        
        if (tempBlockchain.IsValid())
        {
            Console.WriteLine($"🔄 Reemplazando cadena local (bloques: {Chain.Count}) por cadena más larga (bloques: {newChain.Count})");
            Chain = newChain;
            PendingTransactions.Clear();
            SaveToFile();
        }
        else
        {
            Console.WriteLine("❌ Cadena recibida no es válida. No se reemplaza.");
        }
    }

    public void SaveToFile()
    {
        lock (_fileLock)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };

                var data = new BlockchainData
                {
                    Chain = this.Chain,
                    PendingTransactions = this.PendingTransactions,
                    Difficulty = this.Difficulty
                };

                string jsonString = JsonSerializer.Serialize(data, options);
                File.WriteAllText(FilePath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR AL GUARDAR]: {ex.Message}");
            }
        }
    }

    public void LoadFromFile()
    {
        if (!File.Exists(FilePath))
        {
            Console.WriteLine($"[SISTEMA]: No se encontró archivo. Iniciando nueva cadena.");
            SaveToFile();
            return;
        }

        try
        {
            Console.WriteLine("\n[Persistencia] Cargando blockchain desde disco...");
            string jsonString = File.ReadAllText(FilePath);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var data = JsonSerializer.Deserialize<BlockchainData>(jsonString, options);
            
            if (data != null && data.Chain != null && data.Chain.Count > 0)
            {
                this.Chain = data.Chain;
                this.PendingTransactions = data.PendingTransactions ?? new List<Transaction>();
                this.Difficulty = data.Difficulty;

                Console.Write("\n[Validación] Verificando integridad de la cadena... ");
                if (this.IsValid())
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ ¡ÉXITO! Cadena válida.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ ¡ALERTA! Cadena corrupta. Reiniciando.");
                    Console.ResetColor();
                    this.Chain = new List<Block>();
                    this.PendingTransactions = new List<Transaction>();
                    var genesis = new Block(0, DateTime.Now, new List<Transaction>(), "0", this.Difficulty);
                    genesis.MineBlock(this.Difficulty);
                    this.Chain.Add(genesis);
                    SaveToFile();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Fallo al cargar: {ex.Message}");
            // Reiniciar con cadena nueva
            this.Chain = new List<Block>();
            this.PendingTransactions = new List<Transaction>();
            var genesis = new Block(0, DateTime.Now, new List<Transaction>(), "0", this.Difficulty);
            genesis.MineBlock(this.Difficulty);
            this.Chain.Add(genesis);
            SaveToFile();
        }
    }

    // Clase auxiliar para serialización
    private class BlockchainData
    {
        public List<Block> Chain { get; set; } = new List<Block>();
        public List<Transaction> PendingTransactions { get; set; } = new List<Transaction>();
        public int Difficulty { get; set; } = 4;
    }
}