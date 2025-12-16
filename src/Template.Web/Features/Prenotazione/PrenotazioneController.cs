using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Template.Data;
using Template.Entities;
using Template.Services.Prenotazioni;

namespace Template.Web.Features.Prenotazione
{
    [Authorize]
    public partial class PrenotazioneController : Controller
    {
        private readonly TemplateDbContext _dbContext;

        public PrenotazioneController(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public virtual IActionResult Mappa() => View();

        [HttpGet]
        public virtual async Task<IActionResult> GetDatiMappa(DateTime? data)
        {
            // --- QUESTA PARTE È RIMASTA INVARIATA (Logica colori corretta) ---
            var dataRichiesta = data ?? DateTime.Today;

            var postazioniDb = await _dbContext.Postazioni
                .Include(p => p.Prenotazioni)
                .ToListAsync();

            var postazioniArricchite = postazioniDb.Select(p =>
            {
                int personeTotaliSedute = p.Prenotazioni
                    .Where(pren => pren.DataPrenotazione.Date == dataRichiesta.Date && !pren.IsCancellata)
                    .Sum(pren => pren.NumeroPersone);

                bool isOccupata;

                if (p.Tipo == "Ristorante")
                {
                    isOccupata = personeTotaliSedute >= p.PostiTotali;
                }
                else
                {
                    isOccupata = personeTotaliSedute > 0;
                }

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
                    MetriQuadri = CalcolaMq(p.Tipo, p.Id),
                    Haledwall = p.Tipo == "Eventi",
                    HaProiettore = p.Tipo == "Riunioni" || p.Tipo == "Team",
                    HaFinestre = new[] { "Singola", "Ristorante", "Team", "Riunioni", "Eventi" }.Contains(p.Tipo),
                    Descrizione = GeneraDescrizione(p.Tipo, p.Id)
                };
            });

            return Json(postazioniArricchite);
        }

        // --- METODO PRENOTA AGGIORNATO PER IL CARRELLO ---
        [HttpPost]
        public virtual async Task<IActionResult> Prenota([FromBody] PrenotaRequest request)
        {
            // 1. Validazione: Verifichiamo se ci sono Elementi nel carrello
            if (!ModelState.IsValid || request.Elementi == null || !request.Elementi.Any())
                return BadRequest(new { success = false, message = "Il carrello è vuoto." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name ?? "Utente_Sconosciuto";

            // 2. Transazione: Tutto o niente
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Cicliamo ogni elemento del carrello
                foreach (var item in request.Elementi)
                {
                    // Recuperiamo la singola postazione
                    var postazione = await _dbContext.Postazioni.FindAsync(item.PostazioneId);

                    if (postazione == null)
                        throw new Exception($"La postazione ID {item.PostazioneId} non esiste.");

                    // Calcolo occupazione attuale
                    int postiGiaOccupati = await _dbContext.Prenotazioni
                        .Where(p => p.PostazioneId == postazione.Id
                                    && p.DataPrenotazione.Date == request.Data.Date
                                    && !p.IsCancellata)
                        .SumAsync(p => p.NumeroPersone);

                    // --- LOGICA DI BLOCCO ---
                    if (postazione.Tipo == "Ristorante")
                    {
                        // Ristorante: Controllo capienza numerica
                        int capienzaMax = postazione.PostiTotali > 0 ? postazione.PostiTotali : 1;
                        
                        // Qui usiamo item.NumeroPersone (specifico per questa voce del carrello)
                        if (postiGiaOccupati + item.NumeroPersone > capienzaMax)
                        {
                            throw new Exception($"'{postazione.Nome}': spazio insufficiente (Richiesti: {item.NumeroPersone}, Liberi: {capienzaMax - postiGiaOccupati}).");
                        }
                    }
                    else
                    {
                        // Uffici/Meeting: Uso esclusivo
                        if (postiGiaOccupati > 0)
                        {
                            throw new Exception($"'{postazione.Nome}' è già occupata.");
                        }
                    }

                    // Creazione Prenotazione
                    _dbContext.Prenotazioni.Add(new Template.Entities.Prenotazione
                    {
                        PostazioneId = postazione.Id,
                        DataPrenotazione = request.Data,
                        UserId = userId,
                        NumeroPersone = item.NumeroPersone, // Salva il numero specifico dell'item
                        DataCreazione = DateTime.UtcNow,
                        IsCancellata = false
                    });
                }

                // 3. Salvataggio e Commit
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Prenotazione multipla confermata con successo!" });
            }
            catch (Exception ex)
            {
                // Se c'è un errore, annulla tutto
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // --- FUNZIONI DI SUPPORTO (Intoccate) ---
        private static int CalcolaMq(string tipo, int id) => tipo switch
        {
            "Singola" => 52, "Team" => 25, "Riunioni" => 23, "Eventi" => 112, "Ristorante" => 77, _ => 0
        };

        private static string GeneraDescrizione(string tipo, int id)
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
    }
}