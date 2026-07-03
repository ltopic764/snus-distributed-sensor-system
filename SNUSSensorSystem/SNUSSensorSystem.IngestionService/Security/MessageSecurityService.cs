using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SNUSSensorSystem.IngestionService.Data;
using SNUSSensorSystem.Shared.DTOs;
using SNUSSensorSystem.Shared.Helpers;
using SNUSSensorSystem.Shared.Models;
using System.Text;
using System.Text.Json;

namespace SNUSSensorSystem.IngestionService.Security
{
    // complete security check of an incoming message
    public class MessageSecurityService : IMessageSecurityService
    {
        private readonly SensorDbContext _db;
        private readonly ServerCryptoOptions _options;
        private readonly ILogger<MessageSecurityService> _logger;

        public MessageSecurityService(SensorDbContext db, IOptions<ServerCryptoOptions> options, ILogger<MessageSecurityService> logger)
        {
            _db = db;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<SecurityCheckResult> VerifyAndDecryptAsync(SecureEnvelopeDto envelope, CancellationToken cancellationToken = default)
        {
            // validation
            if (string.IsNullOrWhiteSpace(envelope.SensorId))
            {
                return SecurityCheckResult.Fail("Missing SensorId");
            }

            // sensor validation
            var sensor = await _db.Sensors.FirstOrDefaultAsync(s => s.Id == envelope.SensorId, cancellationToken);

            if (sensor is null)
            {
                if (string.IsNullOrWhiteSpace(envelope.SenderPublicKey))
                {
                    return SecurityCheckResult.Fail("Unknow sensor without public key (registration not possible)");
                }

                sensor = new Sensor
                {
                    Id = envelope.SensorId,
                    PublicKey = envelope.SenderPublicKey,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageId = 0
                };
                _db.Sensors.Add(sensor);
                _logger.LogInformation("New sensor registered {SensorId}", sensor.Id);
            }
            else if (!string.IsNullOrWhiteSpace(envelope.SenderPublicKey) && sensor.PublicKey != envelope.SenderPublicKey)
            {
                // sensor exists, but a different public key received then expected
                return SecurityCheckResult.Fail("Given public key does not match with the public key of the sensor");
            }

            if (string.IsNullOrWhiteSpace(sensor.PublicKey))
            {
                return SecurityCheckResult.Fail("Sensor has no registered public key");
            }

            // anti-replay

            // messageId has to be larger then last seen id
            if (envelope.MessageId <= sensor.LastMessageId)
            {
                return SecurityCheckResult.Fail($"Replay: MessageId {envelope.MessageId} <= last {sensor.LastMessageId}");
            }

            var age = DateTime.UtcNow - envelope.SentAt.ToUniversalTime();
            if (Math.Abs(age.TotalSeconds) > _options.ReplayToleranceSeconds)
            {
                return SecurityCheckResult.Fail($"Message timestamp is outside allowed time window ({age.TotalSeconds:F0}s)");
            }

            // signature verification
            byte[] signingData = CryptoHelper.BuildSigningData(envelope);
            byte[] signature;

            try
            {
                signature = Convert.FromBase64String(envelope.Signature);
            }
            catch
            {
                return SecurityCheckResult.Fail("Signature is not valid Base64");
            }

            bool signatureOk = CryptoHelper.Verify(signingData, signature, sensor.PublicKey!);
            if (!signatureOk)
            {
                return SecurityCheckResult.Fail("Signature is not okay");
            }

            // decrypting body
            SensorReadingDto? payload = null;
            try
            {
                // decryption with RSA server private key
                byte[] encryptedKey = Convert.FromBase64String(envelope.EncryptedKey);
                byte[] aesKey = CryptoHelper.RsaDecryptKey(encryptedKey, _options.PrivateKeyPem);

                // decrypt message body with AES and IV
                byte[] iv = Convert.FromBase64String(envelope.Iv);
                byte[] cipher = Convert.FromBase64String(envelope.Ciphertext);
                byte[] plain = CryptoHelper.AesDecrypt(cipher, aesKey, iv);

                // map JSON to Dto
                string json = Encoding.UTF8.GetString(plain);
                payload = JsonSerializer.Deserialize<SensorReadingDto>(json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error decrypting sensor message {SensorId}", envelope.SensorId);
            }

            if (payload is null)
            {
                return SecurityCheckResult.Fail("Empty or invalid message body");
            }

            // check matching SensorId
            if (!string.Equals(payload.SensorId, envelope.SensorId, StringComparison.Ordinal))
            {
                return SecurityCheckResult.Fail("SensorId in payload is not a match with SensorId envelope");
            }

            // remember new lastmessageid
            sensor.LastMessageId = envelope.MessageId;

            return SecurityCheckResult.Success(payload);
        }
    }
}
