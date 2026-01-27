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
            if (request.Elementi == null || !request.Elementi.Any())
            {
                return new EsitoPrenotazione { Successo = false, Messaggio = "Il carrello è vuoto, selezionare almeno una postazione." };
            }

            if (request.Data.Date < DateTime.UtcNow.Date)
            {
                 return new EsitoPrenotazione { Successo = false, Messaggio = "Non puoi prenotare nel passato." };
            }

            try
            {
                var strategy = _dbContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                    foreach (var item in request.Elementi)
                    {
                        var postazione = await _dbContext.Postazioni.FindAsync(item.PostazioneId);

                        if (postazione == null)
                        {
                            await transaction.RollbackAsync();
                            return new EsitoPrenotazione { Successo = false, Messaggio = $"La postazione ID {item.PostazioneId} non esiste." };
                        }

                        int postiGiaOccupati = await _dbContext.Prenotazioni
                            .Where(p => p.PostazioneId == postazione.Id
                                        && p.DataPrenotazione.Date == request.Data.Date
                                        && !p.IsCancellata)
                            .SumAsync(p => p.NumeroPersone);

                        if (postazione.Tipo == "Ristorante")
                        {

                            int capienzaMax = postazione.PostiTotali > 0 ? postazione.PostiTotali : 1;
                            
                            if (postiGiaOccupati + item.NumeroPersone > capienzaMax)
                            {
                                int postiRimasti = capienzaMax - postiGiaOccupati;
                                await transaction.RollbackAsync();
                                return new EsitoPrenotazione { Successo = false, Messaggio = $"'{postazione.Nome}': posti insufficienti (Richiesti: {item.NumeroPersone}, Rimasti: {postiRimasti})." };
                            }
                        }
                        else
                        {

                            if (postiGiaOccupati > 0)
                            {
                                await transaction.RollbackAsync();
                                return new EsitoPrenotazione { Successo = false, Messaggio = $"La postazione '{postazione.Nome}' è già occupata per questa data." };
                            }
                        }


                        var nuovaPrenotazione = new Prenotazione 
                        {
                            PostazioneId = postazione.Id,
                            DataPrenotazione = request.Data,
                            UserId = userId,
                            NumeroPersone = item.NumeroPersone,
                            Note = request.Note,
                            DataCreazione = DateTime.Now,
                            IsCancellata = false,
                            Prezzo = CalcolaPrezzo(postazione.Tipo, item.NumeroPersone)
                        };

                        _dbContext.Prenotazioni.Add(nuovaPrenotazione);
                    }

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new EsitoPrenotazione { Successo = true, Messaggio = $"{request.Elementi.Count} spazi prenotati con successo!" };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la prenotazione multipla per User {UserId}", userId);
                
                return new EsitoPrenotazione { Successo = false, Messaggio = "Errore tecnico durante il salvataggio: " + ex.Message };
            }
        }

        public async Task<dynamic> GetDatiMappaAsync(DateTime? data)
        {
            var dataRichiesta = data ?? DateTime.Today;

            var postazioniDb = await _dbContext.Postazioni
                .Include(p => p.Prenotazioni)
                .ToListAsync();

            var postazioniArricchite = postazioniDb.Select(p =>
            {
                int personeTotaliSedute = p.Prenotazioni
                    .Where(pren => pren.DataPrenotazione.Date == dataRichiesta.Date && !pren.IsCancellata)
                    .Sum(pren => pren.NumeroPersone);

                bool isOccupata = p.Tipo == "Ristorante"
                    ? personeTotaliSedute >= p.PostiTotali
                    : personeTotaliSedute > 0;

                return new
                {
                    p.Id,
                    p.Nome,
                    p.Tipo,
                    p.X,
                    p.Y,
                    p.Width,
                    p.Height,
                    p.PostiTotali,
                    PostiOccupati = personeTotaliSedute,
                    IsOccupata = isOccupata,
                    MetriQuadri = CalcolaMq(p.Tipo),
                    HaArmadietto = p.Tipo == "Riunioni" || p.Tipo == "Team",
                    Haledwall = p.Tipo == "Eventi",
                    HaProiettore = p.Tipo == "Riunioni" || p.Tipo == "Team",
                    HaFinestre = new[] { "Singola", "Ristorante", "Team", "Riunioni", "Eventi" }.Contains(p.Tipo),
                    Descrizione = GeneraDescrizione(p.Tipo)
                };
            });

            return postazioniArricchite;
        }

        private static int CalcolaMq(string tipo) => tipo switch
        {
            "Singola" => 52,
            "Team" => 25,
            "Riunioni" => 23,
            "Eventi" => 112,
            "Ristorante" => 77,
            _ => 0
        };

        private static string GeneraDescrizione(string tipo)
        {
            const string baseComfort = "WiFi Ultra, A/C.";
            return tipo switch
            {
                "Singola" => $"Open space. {baseComfort}",
                "Team" => $"Area Team. {baseComfort}",
                "Riunioni" => $"Sala Meeting. {baseComfort}",
                "Ristorante" => $"Zona pranzo. {baseComfort}",
                _ => baseComfort
            };
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