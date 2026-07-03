using SNUSSensorSystem.Shared.DTOs;

namespace SNUSSensorSystem.IngestionService.Security
{
    // result of security check of a message
    public class SecurityCheckResult
    {
        // passed every check?
        public bool IsValid { get; init; }

        // reason why it failed
        public string? Error { get; init; }

        // decrypted message body
        public SensorReadingDto? Payload {get; init; }

        public static SecurityCheckResult Fail(string error) => new() { IsValid = false, Error = error };

        public static SecurityCheckResult Success(SensorReadingDto payload) => new() { IsValid = true, Payload = payload };
    }

    public interface IMessageSecurityService
    {
        // returns securitycheckresult and does the whole verifications and decryption of a message
        Task<SecurityCheckResult> VerifyAndDecryptAsync(SecureEnvelopeDto envelope, CancellationToken cancellationToken = default);
    }
}
