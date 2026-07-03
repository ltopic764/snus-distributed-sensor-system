using Microsoft.EntityFrameworkCore;
using SNUSSensorSystem.Shared.Models;

namespace SNUSSensorSystem.IngestionService.Data
{
    public class SensorDbContext : DbContext
    {
        public SensorDbContext(DbContextOptions<SensorDbContext> options) : base(options) { }

        // sensor set
        public DbSet<Sensor> Sensors => Set<Sensor>();

        // raw readings
        public DbSet<SensorReading> SensorReadings => Set<SensorReading>();

        public DbSet<ConsensusValue> ConsensusValues => Set<ConsensusValue>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Sensor
            modelBuilder.Entity<Sensor>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Id).HasMaxLength(100);
                entity.Property(s => s.PublicKey);
            });

            // SensorReading
            modelBuilder.Entity<SensorReading>(entity =>
            {
                entity.HasKey(r => r.Id);
                // index by (sensorId, timestamp) for filtering
                entity.HasIndex(r => new { r.SensorId, r.Timestamp });
                // index by timestamp (group by minute)
                entity.HasIndex(r => r.Timestamp);
            });

            // ConsensusValue
            modelBuilder.Entity<ConsensusValue>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.Timestamp);
            });
        }
    }
}
