using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Template.Data;
using Template.Entities;

namespace Template.Services.Ristorazione
{
    public class EsitoPrenotazioneTavolo
    {
        public bool Successo { get; set; }
        public string Messaggio { get; set; }
    }

    public class RistorazioneService
    {
        private readonly TemplateDbContext _dbContext;
        private readonly ILogger<RistorazioneService> _logger;

        public RistorazioneService(TemplateDbContext dbContext, ILogger<RistorazioneService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<dynamic> GetTavoliAsync(DateTime? data)
        {
            var dataRichiesta = data ?? DateTime.Today;

            var tavoli = await _dbContext.Postazioni
                .Where(p => p.Tipo == "Ristorante")
                .Select(p => new
                {
                    p.Id,
                    p.Nome,
                    PostiOccupati = p.Prenotazioni.Count(pren =>
                        pren.DataPrenotazione.Date == dataRichiesta.Date && !pren.IsCancellata),
                    PostiTotali = p.PostiTotali > 0 ? p.PostiTotali : 4
                })
                .ToListAsync();

            return tavoli;
        }

        public async Task<EsitoPrenotazioneTavolo> PrenotaTavoloAsync(PrenotaTavoloRequest request, string userId)
        {
            if (request == null || request.NumeroPosti <= 0)
            {
                return new EsitoPrenotazioneTavolo
                {
                    Successo = false,
                    Messaggio = "Numero persone non valido."
                };
            }

            if (request.Data.Date < DateTime.UtcNow.Date)
            {
                return new EsitoPrenotazioneTavolo
                {
                    Successo = false,
                    Messaggio = "Non puoi prenotare nel passato."
                };
            }

            try
            {
                var postazione = await _dbContext.Postazioni.FindAsync(request.PostazioneId);
                if (postazione == null)
                {
                    return new EsitoPrenotazioneTavolo
                    {
                        Successo = false,
                        Messaggio = "Tavolo non trovato."
                    };
                }

                if (postazione.Tipo != "Ristorante")
                {
                    return new EsitoPrenotazioneTavolo
                    {
                        Successo = false,
                        Messaggio = "La postazione selezionata non è un tavolo ristorante."
                    };
                }

                // Verificare se l'utente ha già una scrivania prenotata per lo stesso giorno
                var haScrivania = await _dbContext.Prenotazioni
                    .Include(p => p.Postazione)
                    .AnyAsync(p =>
                        p.UserId == userId &&
                        p.DataPrenotazione.Date == request.Data.Date &&
                        !p.IsCancellata &&
                        p.Postazione.Tipo != "Ristorante"
                    );

                if (haScrivania)
                {
                    return new EsitoPrenotazioneTavolo
                    {
                        Successo = false,
                        Messaggio = "Hai già una scrivania prenotata per questo giorno. Non puoi prenotare anche un tavolo."
                    };
                }

                // Contare i posti già occupati
                var postiGiaOccupati = await _dbContext.Prenotazioni
                    .CountAsync(p =>
                        p.PostazioneId == request.PostazioneId &&
                        p.DataPrenotazione.Date == request.Data.Date &&
                        !p.IsCancellata);

                int capienzaMax = postazione.PostiTotali > 0 ? postazione.PostiTotali : 4;

                if (postiGiaOccupati + request.NumeroPosti > capienzaMax)
                {
                    int postiRimasti = capienzaMax - postiGiaOccupati;
                    return new EsitoPrenotazioneTavolo
                    {
                        Successo = false,
                        Messaggio = $"Posti insufficienti! Rimasti: {postiRimasti}"
                    };
                }

                // Creare le prenotazioni
                using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    for (int i = 0; i < request.NumeroPosti; i++)
                    {
                        _dbContext.Prenotazioni.Add(new Prenotazione
                        {
                            PostazioneId = request.PostazioneId,
                            DataPrenotazione = request.Data,
                            UserId = userId,
                            NumeroPersone = 1,
                            DataCreazione = DateTime.UtcNow,
                            IsCancellata = false
                        });
                    }

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new EsitoPrenotazioneTavolo
                    {
                        Successo = true,
                        Messaggio = $"Prenotazione ristorante confermata per {request.NumeroPosti} persone!"
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Errore durante la prenotazione del tavolo per User {UserId}", userId);
                    return new EsitoPrenotazioneTavolo
                    {
                        Successo = false,
                        Messaggio = "Errore tecnico durante il salvataggio: " + ex.Message
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore inaspettato nella prenotazione tavolo per User {UserId}", userId);
                return new EsitoPrenotazioneTavolo
                {
                    Successo = false,
                    Messaggio = "Errore inaspettato: " + ex.Message
                };
            }
        }
    }

    public class PrenotaTavoloRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "PostazioneId non valido")]
        public int PostazioneId { get; set; }

        [Required(ErrorMessage = "La data è obbligatoria")]
        public DateTime Data { get; set; }

        [Range(1, 10, ErrorMessage = "Numero posti deve essere tra 1 e 10")]
        public int NumeroPosti { get; set; }
    }
}
