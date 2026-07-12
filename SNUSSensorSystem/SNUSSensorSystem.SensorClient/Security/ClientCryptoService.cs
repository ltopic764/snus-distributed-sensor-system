using System.Text;
using System.Text.Json;
using SNUSSensorSystem.Shared.DTOs;
using SNUSSensorSystem.Shared.Helpers;

namespace SNUSSensorSystem.SensorClient.Security;

public sealed class ClientCryptoService
{
    private readonly string _sensorId;
    private readonly string _serverPublicKeyPath;
    private readonly string _messageIdPath;

    private readonly object _messageIdLock = new();

    private readonly string _privateKeyPem;
    private readonly string _publicKeyPem;

    private long _messageId;

    public ClientCryptoService(
        string sensorId,
        string keysRoot,
        string serverPublicKeyPath)
    {
        _sensorId = sensorId;
        _serverPublicKeyPath = serverPublicKeyPath;

        var safeId = string.Concat(
            sensorId.Select(character =>
                char.IsLetterOrDigit(character) ||
                character is '-' or '_'
                    ? character
                    : '_'));

        var sensorKeysDirectory =
            Path.Combine(keysRoot, safeId);

        Directory.CreateDirectory(sensorKeysDirectory);

        var privateKeyPath =
            Path.Combine(
                sensorKeysDirectory,
                "private.pem");

        var publicKeyPath =
            Path.Combine(
                sensorKeysDirectory,
                "public.pem");

        _messageIdPath =
            Path.Combine(
                sensorKeysDirectory,
                "message-id.txt");

        if (File.Exists(privateKeyPath) &&
            File.Exists(publicKeyPath))
        {
            _privateKeyPem =
                File.ReadAllText(privateKeyPath);

            _publicKeyPem =
                File.ReadAllText(publicKeyPath);
        }
        else
        {
            (_publicKeyPem, _privateKeyPem) =
                CryptoHelper.GenerateRsaKeyPair();

            File.WriteAllText(
                privateKeyPath,
                _privateKeyPem);

            File.WriteAllText(
                publicKeyPath,
                _publicKeyPem);
        }

        if (File.Exists(_messageIdPath) &&
            long.TryParse(
                File.ReadAllText(_messageIdPath),
                out var storedMessageId))
        {
            _messageId = storedMessageId;
        }
        else
        {
            _messageId = 0;
        }
    }

    public SecureEnvelopeDto Protect(
        SensorReadingDto payload)
    {
        var serverPublicKey =
            ReadServerPublicKey();

        var serializedPayload =
            JsonSerializer.Serialize(payload);

        var plaintext =
            Encoding.UTF8.GetBytes(
                serializedPayload);

        var aesKey =
            CryptoHelper.GenerateAesKey();

        var encryptionResult =
            CryptoHelper.AesEncrypt(
                plaintext,
                aesKey);

        /*
         * CryptoHelper vraća imenovani tuple:
         *
         * (byte[] ciphertext, byte[] iv)
         *
         * Zbog toga su nazivi malim slovima.
         */
        var ciphertext =
            encryptionResult.ciphertext;

        var iv =
            encryptionResult.iv;

        var encryptedAesKey =
            CryptoHelper.RsaEncryptKey(
                aesKey,
                serverPublicKey);

        var envelope =
            new SecureEnvelopeDto
            {
                SensorId = _sensorId,

                MessageId =
                    NextMessageId(),

                SentAt =
                    DateTime.UtcNow,

                Iv =
                    Convert.ToBase64String(iv),

                EncryptedKey =
                    Convert.ToBase64String(
                        encryptedAesKey),

                Ciphertext =
                    Convert.ToBase64String(
                        ciphertext),

                SenderPublicKey =
                    _publicKeyPem
            };

        var signingData =
            CryptoHelper.BuildSigningData(
                envelope);

        var signature =
            CryptoHelper.Sign(
                signingData,
                _privateKeyPem);

        envelope.Signature =
            Convert.ToBase64String(signature);

        return envelope;
    }

    private string ReadServerPublicKey()
    {
        if (!File.Exists(_serverPublicKeyPath))
        {
            throw new FileNotFoundException(
                "Server public key was not found at " +
                $"'{Path.GetFullPath(_serverPublicKeyPath)}'. " +
                "Start IngestionService first so it can " +
                "generate keys/server_public.pem.");
        }

        return File.ReadAllText(
            _serverPublicKeyPath);
    }

    private long NextMessageId()
    {
        lock (_messageIdLock)
        {
            _messageId++;

            File.WriteAllText(
                _messageIdPath,
                _messageId.ToString());

            return _messageId;
        }
    }
}