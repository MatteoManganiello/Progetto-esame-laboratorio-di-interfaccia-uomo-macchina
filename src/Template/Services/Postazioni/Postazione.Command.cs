using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

// 1. IMPORTIAMO I NUOVI NAMESPACE
using Template.Data;      // Per il DbContext
using Template.Entities;  // Per l'entità Prenotazione e Postazione

namespace Template.Services.Prenotazioni // 2. Namespace coerente con gli altri servizi
{
    // IL "PACCHETTO" DI DATI IN INGRESSO
    public class PrenotaPostazioneCommand
    {
        public int PostazioneId { get; set; }
        public DateTime Data { get; set; }
        public Guid UserId { get; set; }
    }

    // 3. LA LOGICA (Non è più partial class SharedService, ma una classe autonoma)
    public class PostazioneCommandService
    {
        private readonly TemplateDbContext _dbContext;

        // Costruttore per iniettare il Database
        public PostazioneCommandService(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Effettua la prenotazione singola salvandola nel database.
        /// </summary>
        public async Task<bool> EseguiPrenotazioneAsync(PrenotaPostazioneCommand cmd)
        {
            // A. VALIDAZIONE: Il posto esiste davvero?
            var postazione = await _dbContext.Postazioni.FindAsync(cmd.PostazioneId);
            if (postazione == null)
            {
                throw new Exception("Errore: La postazione richiesta non esiste.");
            }

            // B. CONTROLLO CONFLITTI
            var isOccupata = await _dbContext.Prenotazioni
                .AnyAsync(p => p.PostazioneId == cmd.PostazioneId && p.DataPrenotazione.Date == cmd.Data.Date);

            if (isOccupata)
            {
                throw new Exception("Spiacente, questa postazione è già prenotata per la data selezionata.");
            }

            // C. SALVATAGGIO (Usando la nuova entità Template.Entities.Prenotazione)
            var nuovaPrenotazione = new Prenotazione
            {
                PostazioneId = cmd.PostazioneId,
                DataPrenotazione = cmd.Data,
                UserId = cmd.UserId.ToString(),
                DataCreazione = DateTime.UtcNow // Meglio UtcNow
            };

            _dbContext.Prenotazioni.Add(nuovaPrenotazione);
            
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}