using DocumentFormat.OpenXml.Bibliography;
using Microsoft.EntityFrameworkCore;
using TaxiAPI.Models;

namespace TaxiAPI.Data
{
    public class TaxiDbContext : DbContext
    {
        public TaxiDbContext(DbContextOptions<TaxiDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Driver> Drivers => Set<Driver>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Tariff> Tariffs => Set<Tariff>();
        public DbSet<Car> Cars => Set<Car>();
        public DbSet<Trip> Trips => Set<Trip>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Связи
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Client)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Driver)
                .WithMany(d => d.Orders)
                .HasForeignKey(o => o.DriverId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Tariff)
                .WithMany(t => t.Orders)
                .HasForeignKey(o => o.TariffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Car>()
                .HasOne(c => c.Driver)
                .WithMany()
                .HasForeignKey(c => c.DriverId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Driver>()
                .HasOne(d => d.Car)
                .WithMany()
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

