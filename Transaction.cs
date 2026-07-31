using System;
using System.Security.Cryptography;
using System.Text;

public class Transaction
{
    public string Sender { get; set; }
    public string Receiver { get; set; }
    public decimal Amount { get; set; }
    public byte[]? Signature { get; set; }

    public Transaction(string sender, string receiver, decimal amount)
    {
        Sender = sender;
        Receiver = receiver;
        Amount = amount;
    }

    public byte[] CalculateHash()
    {
        using(SHA256 sha256 = SHA256.Create())
        {
            string rawData = $"{Sender}-{Receiver}-{Amount}";
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        }
    }

    public void SignTransaction(ECDsa privateKey)
    {
        if(Sender == "Sistema") return;

        string publicKey = Convert.ToHexString(privateKey.ExportSubjectPublicKeyInfo());

        if(publicKey != Sender)
        {
            throw new InvalidOperationException("No puedes firmar transacciones para otras billeteras.");
        }

        byte[] txHash = CalculateHash();
        Signature = privateKey.SignHash(txHash);
    }

    public bool IsValid()
    {
        if(Sender == "Sistema") return true;
        if(Signature == null || Signature.Length == 0) return false;

        try
        {
            using(ECDsa publicKey = ECDsa.Create())
            {
                publicKey.ImportSubjectPublicKeyInfo(Convert.FromHexString(Sender), out _);
                byte[] txHash = CalculateHash();
                return publicKey.VerifyHash(txHash, Signature);
            }
        }
        catch
        {
            return false;
        }
    }

    public override string ToString()
    {
        string shortSender = Sender.Length > 10 ? Sender.Substring(0,10)+"..." : Sender;
        string shortReceiver = Receiver.Length > 10 ? Receiver.Substring(0,10)+"..." : Receiver;

        return $"{shortSender} -> {shortReceiver}: {Amount}";
    }
}