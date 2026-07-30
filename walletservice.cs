using System;
using System.Security.Cryptography;

public class WalletService
{
    public (string publicKey, string privateKey) GenerateWallet()
    {
        using(ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
        {
            byte[] pkcs8PrivateKey = ecdsa.ExportPkcs8PrivateKey();
            string privateKey = Convert.ToBase64String(pkcs8PrivateKey);

            byte[] subjectPublicKey = ecdsa.ExportSubjectPublicKeyInfo();
            string publicKey = Convert.ToBase64String(subjectPublicKey);

            return (publicKey, privateKey);
        }
    }
}