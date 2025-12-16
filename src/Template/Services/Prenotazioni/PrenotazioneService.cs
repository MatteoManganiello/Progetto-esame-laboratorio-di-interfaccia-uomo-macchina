using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Template.Entities;
using Template.Data;

namespace Template.Services.Prenotazioni
{
    public class EsitoPrenotazione
    {
        public bool Successo { get; set; }
        public string Messaggio { get; set; }
    }

    public class PrenotazioneService
    {
        private readonly TemplateDbContext _dbContext;
        private readonly ILogger<PrenotazioneService> _logger;

        public PrenotazioneService(TemplateDbContext dbContext, ILogger<PrenotazioneService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<EsitoPrenotazione> EseguiPrenotazioneMultiplaAsync(PrenotaRequest request, string userId)
        {
            // 1. Validazione aggiornata per la lista 'Elementi'
            if (request.Elementi == null || !request.Elementi.Any())
            {
                return new EsitoPrenotazione { Successo = false, Messaggio = "Il carrello è vuoto, selezionare almeno una postazione." };
            }

            if (request.Data.Date < DateTime.UtcNow.Date)
            {
                 return new EsitoPrenotazione { Successo = false, Messaggio = "Non puoi prenotare nel passato." };
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Cicliamo su ogni elemento del carrello
                foreach (var item in request.Elementi)
                {
                    // Recuperiamo la singola postazione
                    var postazione = await _dbContext.Postazioni.FindAsync(item.PostazioneId);

                    if (postazione == null)
                    {
                        return new EsitoPrenotazione { Successo = false, Messaggio = $"La postazione ID {item.PostazioneId} non esiste." };
                    }

                    // 2. Calcolo occupazione (SOMMA delle persone già prenotate)
                    // Questo risolve il problema dei posti del ristorante
                    int postiGiaOccupati = await _dbContext.Prenotazioni
                        .Where(p => p.PostazioneId == postazione.Id
                                    && p.DataPrenotazione.Date == request.Data.Date
                                    && !p.IsCancellata)
                        .SumAsync(p => p.NumeroPersone);

                    // 3. Logica differenziata (Ristorante vs Ufficio)
                    if (postazione.Tipo == "Ristorante")
                    {
                        // Ristorante: Controllo capienza numerica
                        int capienzaMax = postazione.PostiTotali > 0 ? postazione.PostiTotali : 1;
                        
                        // Controllo: Posti attuali + Nuovi posti richiesti > Capienza?
                        if (postiGiaOccupati + item.NumeroPersone > capienzaMax)
                        {
                            int postiRimasti = capienzaMax - postiGiaOccupati;
                            return new EsitoPrenotazione { Successo = false, Messaggio = $"'{postazione.Nome}': posti insufficienti (Richiesti: {item.NumeroPersone}, Rimasti: {postiRimasti})." };
                        }
                    }
                    else
                    {
                        // Uffici/Meeting/Team: Uso esclusivo
                        // Se c'è anche solo 1 persona, è occupata per gli altri
                        if (postiGiaOccupati > 0)
                        {
                            return new EsitoPrenotazione { Successo = false, Messaggio = $"La postazione '{postazione.Nome}' è già occupata per questa data." };
                        }
                    }

                    // 4. Creazione della Prenotazione
                    var nuovaPrenotazione = new Prenotazione 
                    {
                        PostazioneId = postazione.Id,
                        DataPrenotazione = request.Data,
                        UserId = userId,
                        NumeroPersone = item.NumeroPersone, // Usiamo il numero specifico dell'item del carrello
                        DataCreazione = DateTime.UtcNow,
                        IsCancellata = false
                    };

                    _dbContext.Prenotazioni.Add(nuovaPrenotazione);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return new EsitoPrenotazione { Successo = true, Messaggio = $"{request.Elementi.Count} spazi prenotati con successo!" };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Errore durante la prenotazione multipla per User {UserId}", userId);
                
                return new EsitoPrenotazione { Successo = false, Messaggio = "Errore tecnico durante il salvataggio: " + ex.Message };
            }
        }
    }
}