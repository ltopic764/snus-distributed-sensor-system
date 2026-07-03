namespace SNUSSensorSystem.IngestionService.Security
{

    public class ServerCryptoOptions
    {
        // section name in config
        public const string SectionName = "ServerCrypto";

        // server public key, shared with services
        public string PublicKeyPem { get; set; } = string.Empty;

        public string PrivateKeyPem { get; set; } = string.Empty;

        // anti-replay tolerance
        // how many seconds will we tolarete between message before flagging 
        public int ReplayToleranceSeconds { get; set; } = 30; // 30s
    }
}
