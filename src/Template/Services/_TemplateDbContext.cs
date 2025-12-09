using Microsoft.EntityFrameworkCore;
using Template.Infrastructure; 
// Nota: Assicurati che 'Template.Infrastructure' esista nel progetto per usare DataGenerator
// Se DataGenerator ti da errore, commenta la riga nel costruttore.

namespace Template.Services.Shared
{
    public class TemplateDbContext : DbContext
    {
        public TemplateDbContext()
        {
        }

        public TemplateDbContext(DbContextOptions<TemplateDbContext> options) : base(options)
        {
            // Attenzione: Inizializzare dati nel costruttore può rallentare l'avvio.
            // Se ti da errore su DataGenerator, commenta questa riga:
            DataGenerator.InitializeUsers(this);
        }

        // Tabelle del Database
        public DbSet<User> Users { get; set; }
        public DbSet<Postazione> Postazioni { get; set; }
        public DbSet<Prenotazione> Prenotazioni { get; set; }
    }
}