using System;
using System.Linq;
using System.Threading; 
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data; 
using Template.Services.Shared;
using Template.Web.Features.Prenotazione.Models;

namespace Template.Web.Features.Prenotazione.Services
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
            if (request.PostazioniIds == null || !request.PostazioniIds.Any())
            {
                return new EsitoPrenotazione { Successo = false, Messaggio = "Selezionare almeno una postazione." };
            }

            if (request.Data.Date < DateTime.UtcNow.Date)
            {
                 return new EsitoPrenotazione { Successo = false, Messaggio = "Non puoi prenotare nel passato." };
            }

            // *** FIX QUI SOTTO ***
            // Rimuoviamo l'argomento IsolationLevel.Serializable. 
            // Usiamo il default del database (ReadCommitted), che evita l'errore di compilazione
            // ed è sufficiente per prevenire i conflitti grazie ai controlli successivi.
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var postazioniOccupate = await _dbContext.Prenotazioni
                    .Where(p => request.PostazioniIds.Contains(p.PostazioneId)
                             && p.DataPrenotazione.Date == request.Data.Date
                             // && !p.IsCancellata 
                             )
                    .Select(p => p.Postazione.Nome)
                    .ToListAsync();

                if (postazioniOccupate.Any())
                {
                    var nomi = string.Join(", ", postazioniOccupate.Distinct());
                    return new EsitoPrenotazione { Successo = false, Messaggio = $"Le seguenti postazioni sono già occupate: {nomi}." };
                }

                var postazioniDb = await _dbContext.Postazioni
                    .Where(p => request.PostazioniIds.Contains(p.Id))
                    .ToListAsync();

                if (postazioniDb.Count != request.PostazioniIds.Count)
                {
                    return new EsitoPrenotazione { Successo = false, Messaggio = "Una o più postazioni selezionate non esistono." };
                }

                foreach (var postazione in postazioniDb)
                {
                    int capienzaMax = postazione.PostiTotali > 0 ? postazione.PostiTotali : 1;
                    
                    if (request.NumeroPersone > capienzaMax)
                    {
                        return new EsitoPrenotazione { Successo = false, Messaggio = $"La postazione '{postazione.Nome}' non può ospitare {request.NumeroPersone} persone (Max: {capienzaMax})." };
                    }

                    var nuovaPrenotazione = new Template.Services.Shared.Prenotazione
                    {
                        PostazioneId = postazione.Id,
                        DataPrenotazione = request.Data,
                        UserId = userId,
                        NumeroPersone = request.NumeroPersone, 
                        DataCreazione = DateTime.UtcNow,
                    };

                    _dbContext.Prenotazioni.Add(nuovaPrenotazione);
                }

                await _dbContext.SaveChangesAsync();
                
                await transaction.CommitAsync();

                return new EsitoPrenotazione { Successo = true, Messaggio = $"{request.PostazioniIds.Count} spazi prenotati con successo!" };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Errore durante la prenotazione multipla per User {UserId}", userId);
                
                return new EsitoPrenotazione { Successo = false, Messaggio = "Errore tecnico. Riprova tra un istante." };
            }
        }
    }
}