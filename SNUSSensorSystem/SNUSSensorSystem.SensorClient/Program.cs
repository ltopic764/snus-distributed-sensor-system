using System.Text.Json;
using System.Text.Json.Serialization;
using SNUSSensorSystem.SensorClient.Config;
using SNUSSensorSystem.SensorClient.Security;
using SNUSSensorSystem.SensorClient.Sensor;

var applicationDirectory = AppContext.BaseDirectory;

var settingsPathFromEnvironment =
    Environment.GetEnvironmentVariable("SENSOR_CONFIG_PATH");

var settingsPath = string.IsNullOrWhiteSpace(settingsPathFromEnvironment)
    ? Path.Combine(applicationDirectory, "appsettings.json")
    : Path.GetFullPath(settingsPathFromEnvironment);

if (!File.Exists(settingsPath))
{
    throw new FileNotFoundException(
        $"Sensor configuration file was not found: {settingsPath}");
}

var settingsJson = await File.ReadAllTextAsync(settingsPath);

using var settingsDocument = JsonDocument.Parse(settingsJson);

var root = settingsDocument.RootElement;

var ingressBaseUrl =
    Environment.GetEnvironmentVariable("INGRESS_BASE_URL")
    ?? root.GetProperty("IngressBaseUrl").GetString()
    ?? "http://localhost:5115/";

if (!ingressBaseUrl.EndsWith('/'))
{
    ingressBaseUrl += "/";
}

var configuredServerPublicKeyPath =
    Environment.GetEnvironmentVariable("SERVER_PUBLIC_KEY_PATH")
    ?? root.GetProperty("ServerPublicKeyPath").GetString()
    ?? "keys/server_public.pem";

var configuredSensorKeysPath =
    Environment.GetEnvironmentVariable("SENSOR_KEYS_PATH")
    ?? root.GetProperty("SensorKeysPath").GetString()
    ?? "keys";

var serverPublicKeyPath = Path.IsPathRooted(configuredServerPublicKeyPath)
    ? configuredServerPublicKeyPath
    : Path.GetFullPath(
        Path.Combine(applicationDirectory, configuredServerPublicKeyPath));

var sensorKeysPath = Path.IsPathRooted(configuredSensorKeysPath)
    ? configuredSensorKeysPath
    : Path.GetFullPath(
        Path.Combine(applicationDirectory, configuredSensorKeysPath));

var requiredActiveSensors =
    root.TryGetProperty(
        "RequiredActiveSensors",
        out var requiredActiveElement)
        ? requiredActiveElement.GetInt32()
        : 5;

var serializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters =
    {
        new JsonStringEnumConverter()
    }
};

var sensorConfigs =
    root.GetProperty("Sensors")
        .Deserialize<List<SensorConfig>>(serializerOptions)
    ?? throw new InvalidOperationException(
        "No sensor configurations were supplied.");

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(ingressBaseUrl),
    Timeout = TimeSpan.FromSeconds(10)
};

var sensors = sensorConfigs
    .Select(config =>
        new SensorSimulator(
            config,
            httpClient,
            new ClientCryptoService(
                config.SensorId,
                sensorKeysPath,
                serverPublicKeyPath)))
    .ToList();

using var shutdown = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};

Console.WriteLine(
    $"SensorClient starting {requiredActiveSensors} active sensors " +
    $"out of {sensors.Count} configured sensors.");

Console.WriteLine($"Ingress: {ingressBaseUrl}");
Console.WriteLine($"Server public key: {serverPublicKeyPath}");
Console.WriteLine($"Sensor keys: {sensorKeysPath}");

var fleetManager = new SensorFleetManager(
    sensors,
    requiredActiveSensors,
    TimeSpan.FromSeconds(10));

try
{
    await fleetManager.RunAsync(shutdown.Token);
}
catch (OperationCanceledException)
    when (shutdown.IsCancellationRequested)
{
    Console.WriteLine("SensorClient stopped.");
}