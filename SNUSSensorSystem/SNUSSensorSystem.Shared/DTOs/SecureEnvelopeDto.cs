using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNUSSensorSystem.Shared.DTOs
{
    public class SecureEnvelopeDto
    {
        public string SensorId { get; set; } = string.Empty;

        // messageId is incremental number used to check for replay attacks
        // every next message has to be different id sent from this sensorid
        public long MessageId { get; set; }

        public DateTime SentAt { get; set; }

        // AES initialization vector, Base64
        public string Iv { get; set; } = string.Empty;

        // AES message key, encrypted with public RSA server key
        public string EncryptedKey { get; set; } = string.Empty;

        public string Ciphertext { get; set; } = string.Empty;

        // digital signature, created with private key sensor
        public string Signature { get; set; } = string.Empty;

        public string? SenderPublicKey { get; set; }
    }
}
