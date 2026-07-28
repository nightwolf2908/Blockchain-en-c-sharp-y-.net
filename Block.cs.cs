#nullable disable
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

public class Block
{
    public int Index { get; set; }
    public DateTime Timestamp {get;set;}
    public string PreviousHash { get; set; }
    public string Hash { get; set; }
    public List<Transaction> Transactions {get;set;}
    public int Nonce { get; set; }
    public long MiningDurationSecond {get;set;}
    public int BlockDifficulty {get; set;}

    [JsonConstructor]
    public Block()
    {
    }

    public Block(int index, DateTime timestamp, List<Transaction> transactions, string previousHash="",int currentDifficulty=2)
    {
        Index = index;
        Timestamp = timestamp;
        PreviousHash = previousHash;
        Transactions = transactions;
        BlockDifficulty = currentDifficulty;
        Nonce = 0;
        Hash = CalculateHash();
    }

    public string CalculateHash()
    {
        using(SHA256 sha256 = SHA256.Create())
        {
            string rawData = $"{Index}-{Timestamp}-{PreviousHash}-{string.Join(",", Transactions)}-{Nonce}";
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            StringBuilder builder = new StringBuilder();
            foreach(byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }

    public void MineBlock(int difficulty)
    {
        string leadingZeros = new string('0', this.BlockDifficulty);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while(Hash.Substring(0,this.BlockDifficulty) != leadingZeros)
        {
            Nonce++;
            Hash = CalculateHash();
        }
        watch.Stop();
        this.MiningDurationSecond = Math.Max(1, watch.ElapsedMilliseconds / 1000);
        if(this.MiningDurationSecond == 0)  this.MiningDurationSecond = 1;
        Console.WriteLine($"!Bloque minado¡ Hash: {Hash}");
    }
}



#nullable enable