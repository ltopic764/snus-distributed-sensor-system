using SNUSSensorSystem.NotificationService.Hubs;
using SNUSSensorSystem.NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "SignalRCors";

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();

builder.Services.AddScoped<INotificationBroadcastService, NotificationBroadcastService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();

        if (allowedOrigins.Length == 0 || allowedOrigins.Contains("*"))
        {
            policy.SetIsOriginAllowed(_ => true);
        }
        else
        {
            policy.WithOrigins(allowedOrigins);
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors(CorsPolicyName);
app.UseAuthorization();

app.MapControllers();
app.MapHub<AlarmHub>("/hub/alarms");
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Ok(new
{
    service = "SNUS NotificationService",
    notifyEndpoint = "/api/notify",
    alarmHub = "/hub/alarms",
    clientMethod = NotificationBroadcastService.AlarmClientMethod
}));

app.Run();
