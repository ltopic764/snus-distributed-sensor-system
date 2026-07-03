using SNUSSensorSystem.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SNUSSensorSystem.Shared.Helpers
{
    public static class CryptoHelper
    {
        private const int AesKeySizeBytes = 32;

        public static byte[] GenerateAesKey()
        {
            var key = new byte[AesKeySizeBytes];
            RandomNumberGenerator.Fill(key);
            return key;
        }

        public static (byte[] ciphertext, byte[] iv) AesEncrypt(byte[] plaintext, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

            return (ciphertext, aes.IV);
        }

        public static byte[] AesDecrypt(byte[] ciphertext, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        public static (string publicKeyPem, string privateKeyPem) GenerateRsaKeyPair()
        {
            using var rsa = RSA.Create(2048);
            string publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
            string privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
            return (publicKeyPem, privateKeyPem);
        }

        public static byte[] RsaEncryptKey(byte[] aesKey, string recipientPublicKeyPem)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(recipientPublicKeyPem);
            return rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);
        }

        public static byte[] RsaDecryptKey(byte[] encryptedAesKey, string recipientPrivateKeyPem)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(recipientPrivateKeyPem);
            return rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);
        }

        public static byte[] Sign(byte[] data, string signerPrivateKeyPem)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(signerPrivateKeyPem);
            return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        public static bool Verify(byte[] data, byte[] signature, string signerPublicKeyPem)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(signerPublicKeyPem);
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        public static byte[] BuildSigningData(SecureEnvelopeDto envelope)
        {
            var canonical =
                $"{envelope.SensorId}" + $"{envelope.MessageId}" + $"{envelope.SentAt.ToUniversalTime():O}" + $"{envelope.Ciphertext}";

            return Encoding.UTF8.GetBytes(canonical);
        }
    }
}
