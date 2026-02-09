// DbContext principale dell’applicazione: definisce le tabelle EF Core tramite DbSet
// e applica configurazioni al modello (es. indici, vincoli, relazioni).
// Gestisce le entità di dominio per utenti, prenotazioni, postazioni, menu e notifiche.


using Microsoft.EntityFrameworkCore;
using Template.Entities;

namespace Template.Data
{
    public class TemplateDbContext : DbContext
    {
        public TemplateDbContext(DbContextOptions<TemplateDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Postazione> Postazioni { get; set; }
        public DbSet<Prenotazione> Prenotazioni { get; set; }
        public DbSet<MenuSettimanale> MenuSettimanali { get; set; }
        public DbSet<Notifica> Notifiche { get; set; }
        public DbSet<MessaggioSuperAdmin> MessaggiSuperAdmin { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MenuSettimanale>()
                .HasIndex(m => m.WeekStart)
                .IsUnique();
        }
    }
}