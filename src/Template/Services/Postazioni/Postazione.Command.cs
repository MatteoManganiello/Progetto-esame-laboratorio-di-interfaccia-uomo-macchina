using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


using Template.Data;     
using Template.Entities;  

namespace Template.Services.Prenotazioni 
{
   
    public class PrenotaPostazioneCommand
    {
        public int PostazioneId { get; set; }
        public DateTime Data { get; set; }
        public Guid UserId { get; set; }
    }

    
    public class PostazioneCommandService
    {
        private readonly TemplateDbContext _dbContext;

        public PostazioneCommandService(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> EseguiPrenotazioneAsync(PrenotaPostazioneCommand cmd)
        {

            var postazione = await _dbContext.Postazioni.FindAsync(cmd.PostazioneId);
            if (postazione == null)
            {
                throw new Exception("Errore: La postazione richiesta non esiste.");
            }

            var isOccupata = await _dbContext.Prenotazioni
                .AnyAsync(p => p.PostazioneId == cmd.PostazioneId && p.DataPrenotazione.Date == cmd.Data.Date);

            if (isOccupata)
            {
                throw new Exception("Spiacente, questa postazione è già prenotata per la data selezionata.");
            }

            var nuovaPrenotazione = new Prenotazione
            {
                PostazioneId = cmd.PostazioneId,
                DataPrenotazione = cmd.Data,
                UserId = cmd.UserId.ToString(),
                DataCreazione = DateTime.UtcNow,
                NumeroPersone = 1,
                IsCancellata = false,
                Prezzo = CalcolaPrezzo(postazione.Tipo, 1)
            };

            _dbContext.Prenotazioni.Add(nuovaPrenotazione);
            
            await _dbContext.SaveChangesAsync();

            return true;
        }

        private static decimal CalcolaPrezzo(string tipo, int numeroPersone)
        {
            decimal basePrezzo = tipo switch
            {
                "Singola" => 25m,
                "Team" => 150m,
                "Riunioni" => 80m,
                "Eventi" => 500m,
                "Ristorante" => 15m,
                _ => 0m
            };

            return tipo == "Ristorante" ? basePrezzo * Math.Max(numeroPersone, 1) : basePrezzo;
        }
    }
}