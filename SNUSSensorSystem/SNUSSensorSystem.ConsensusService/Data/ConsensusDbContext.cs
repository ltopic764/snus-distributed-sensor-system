using Microsoft.EntityFrameworkCore;
using SNUSSensorSystem.Shared.Models;

namespace SNUSSensorSystem.ConsensusService.Data;

public class ConsensusDbContext : DbContext
{
    public ConsensusDbContext(DbContextOptions<ConsensusDbContext> options) : base(options)
    {
    }

    public DbSet<Sensor> Sensors => Set<Sensor>();

    public DbSet<SensorReading> SensorReadings =>
        Set<SensorReading>();

    public DbSet<ConsensusValue> ConsensusValues =>
        Set<ConsensusValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.HasKey(sensor => sensor.Id);

            entity.Property(sensor => sensor.Id)
                .HasMaxLength(100);

            entity.Property(sensor => sensor.PublicKey);
        });

        modelBuilder.Entity<SensorReading>(entity =>
        {
            entity.HasKey(reading => reading.Id);

            entity.HasIndex(reading => new
            {
                reading.SensorId,
                reading.Timestamp
            });

            entity.HasIndex(reading => reading.Timestamp);
        });

        modelBuilder.Entity<ConsensusValue>(entity =>
        {
            entity.HasKey(value => value.Id);
            entity.HasIndex(value => value.Timestamp);
        });
    }
}