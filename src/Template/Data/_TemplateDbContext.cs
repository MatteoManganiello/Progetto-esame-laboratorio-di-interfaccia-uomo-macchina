using Microsoft.EntityFrameworkCore;
using Template.Entities; // Per vedere User, Postazione, Prenotazione

namespace Template.Data
{
    public class TemplateDbContext : DbContext
    {
        public TemplateDbContext(DbContextOptions<TemplateDbContext> options)
            : base(options)
        {
        }

        // Definizione delle Tabelle
        public DbSet<User> Users { get; set; }
        public DbSet<Postazione> Postazioni { get; set; }
        public DbSet<Prenotazione> Prenotazioni { get; set; }
        public DbSet<MenuSettimanale> MenuSettimanali { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // QUI C'ERA L'ERRORE: DataGenerator.InitializeUsers(...)
            // L'abbiamo rimosso perché ora i dati vengono caricati da Program.cs
            
            // Configurazioni aggiuntive (opzionali)
            // Es. definire chiavi primarie composte o relazioni specifiche se servono
            modelBuilder.Entity<MenuSettimanale>()
                .HasIndex(m => m.WeekStart)
                .IsUnique();
        }
    }
}