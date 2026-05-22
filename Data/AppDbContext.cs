using GerenciadorRede.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorRede.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Dispositivo> Dispositivos { get; set; }
        public DbSet<HistoricoRede> HistoricosRede { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Dispositivo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.MACAddress).IsUnique();
                entity.Property(e => e.MACAddress).HasMaxLength(50).IsRequired();
                entity.Property(e => e.IPAddress).HasMaxLength(50).IsRequired();
                entity.Property(e => e.NomeAmigavel).HasMaxLength(100);
            });

            modelBuilder.Entity<HistoricoRede>(entity =>
            {
                entity.HasKey(e => e.Id);


                entity.HasOne<Dispositivo>()
                      .WithMany()
                      .HasForeignKey(e => e.DispositivoId)
                      .OnDelete(DeleteBehavior.Cascade); 
            });
        }
    }
}