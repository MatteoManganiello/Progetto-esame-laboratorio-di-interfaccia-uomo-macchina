using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Template.Services.Shared
{
    // 1. IL "PACCHETTO" DI DATI IN INGRESSO
    // Questo è l'oggetto che contiene le info necessarie per prenotare
    public class PrenotaPostazioneCommand
    {
        public int PostazioneId { get; set; }
        public DateTime Data { get; set; }
        public Guid UserId { get; set; } // L'ID dell'utente che sta facendo l'azione
    }

    // 2. LA LOGICA (Estensione di SharedService)
    public partial class SharedService
    {
        /// <summary>
        /// Effettua la prenotazione salvandola nel database.
        /// Se il posto è occupato o non esiste, lancia un'eccezione.
        /// </summary>
        public async Task<bool> Command(PrenotaPostazioneCommand cmd)
        {
            // A. VALIDAZIONE: Il posto esiste davvero?
            var postazione = await _dbContext.Postazioni.FindAsync(cmd.PostazioneId);
            if (postazione == null)
            {
                throw new Exception("Errore: La postazione richiesta non esiste.");
            }

            // B. CONTROLLO CONFLITTI: È già occupato in quella data?
            // Cerchiamo nella tabella Prenotazioni se c'è già una riga con:
            // Stessa Postazione AND Stessa Data
            var isOccupata = await _dbContext.Prenotazioni
                .AnyAsync(p => p.PostazioneId == cmd.PostazioneId && p.DataPrenotazione.Date == cmd.Data.Date);

            if (isOccupata)
            {
                throw new Exception("Spiacente, questa postazione è già prenotata per la data selezionata.");
            }

            // C. SALVATAGGIO
            // Se siamo arrivati qui, è tutto ok. Creiamo la nuova prenotazione.
            var nuovaPrenotazione = new Prenotazione
            {
                PostazioneId = cmd.PostazioneId,
                DataPrenotazione = cmd.Data,
                UserId = cmd.UserId.ToString() // Salviamo l'ID utente come stringa
            };

            _dbContext.Prenotazioni.Add(nuovaPrenotazione);
            
            // Questo è il comando che scrive fisicamente nel Database
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}