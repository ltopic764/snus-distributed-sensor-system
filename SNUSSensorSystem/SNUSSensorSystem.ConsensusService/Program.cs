using Microsoft.EntityFrameworkCore;
using SNUSSensorSystem.ConsensusService;
using SNUSSensorSystem.ConsensusService.Algorithms;
using SNUSSensorSystem.ConsensusService.Data;
using SNUSSensorSystem.ConsensusService.Services;
using SNUSSensorSystem.ConsensusService.Workers;

var builder = Host.CreateApplicationBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("SensorDb")
    ?? throw new InvalidOperationException(
        "Connection string 'SensorDb' is not configured.");

builder.Services.Configure<ConsensusOptions>(
    builder.Configuration.GetSection(
        ConsensusOptions.SectionName));

builder.Services.AddDbContext<ConsensusDbContext>(
    options =>
        options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IBftConsensusAlgorithm, BftConsensusAlgorithm>();

builder.Services.AddScoped<IConsensusCalculatorService, ConsensusCalculatorService>();

builder.Services.AddScoped<ConsensusWorker>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();