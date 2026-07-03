using AspNetCoreRateLimit;
using Microsoft.EntityFrameworkCore;
using SNUSSensorSystem.IngestionService.Data;
using SNUSSensorSystem.IngestionService.Security;
using SNUSSensorSystem.IngestionService.Services;
using SNUSSensorSystem.Shared.Helpers;

var builder = WebApplication.CreateBuilder(args);

// MVC controller, swagger
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// db
var connectionString = builder.Configuration.GetConnectionString("SensorDb");
builder.Services.AddDbContext<SensorDbContext>(options => options.UseNpgsql(connectionString));

var keysDir = Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(keysDir);
var privPath = Path.Combine(keysDir, "server_private.pem");
var pubPath = Path.Combine(keysDir, "server_public.pem");

string privPem, pubPem;
if (File.Exists(privPath) && File.Exists(pubPath))
{
    privPem = File.ReadAllText(privPath);
    pubPem = File.ReadAllText(pubPath);
}
else
{
    (pubPem, privPem) = CryptoHelper.GenerateRsaKeyPair();
    File.WriteAllText(privPath, privPem);
    File.WriteAllText(pubPath, pubPem);
}

builder.Services.Configure<ServerCryptoOptions>(opts =>
{
    opts.PublicKeyPem = pubPem;
    opts.PrivateKeyPem = privPem;
    opts.ReplayToleranceSeconds =
        builder.Configuration.GetValue<int?>("ServerCrypto:ReplayToleranceSeconds") ?? 30;
});

builder.Services.AddScoped<IMessageSecurityService, MessageSecurityService>();
builder.Services.AddSingleton<ISensorRateLimiter, SensorRateLimiter>();

var notificationBaseUrl = builder.Configuration["NotificationService:BaseUrl"]
                          ?? "http://localhost:5175";
builder.Services.AddHttpClient<IAlarmService, AlarmService>(client =>
{
    client.BaseAddress = new Uri(notificationBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(
    builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SensorDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "DB migration failed at start.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseIpRateLimiting();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Logger.LogInformation("IngestionService started. Servers public key in keys/server_public.pem");

app.Run();
