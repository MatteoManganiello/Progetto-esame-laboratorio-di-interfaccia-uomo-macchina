using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // Necessario per [Authorize]
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // Necessario per leggere l'ID utente
using System.Threading.Tasks;
using Template.Services.Shared;
using Template.Services;
using Template.Web.Features.Prenotazione.Models; 

namespace Template.Web.Features.Prenotazione
{
    [Authorize]
    public partial class PrenotazioneController : Controller
    {
        private readonly SharedService _sharedService;
        private readonly TemplateDbContext _dbContext;

        public PrenotazioneController(SharedService sharedService, TemplateDbContext dbContext)
        {
            _sharedService = sharedService;
            _dbContext = dbContext;
        }

        public virtual IActionResult Mappa() => View();

        [HttpGet]
        public virtual async Task<IActionResult> GetDatiMappa(DateTime? data)
        {
            var dataRichiesta = data ?? DateTime.Today;
            
            var risultato = await _sharedService.Query(new MappaQuery { Data = dataRichiesta });

            var postazioniArricchite = risultato.Postazioni.Select(p => new
            {
                p.Id, p.Nome, p.Tipo, p.X, p.Y, p.Width, p.Height,
                p.PostiTotali, p.PostiOccupati, p.IsOccupata,
                MetriQuadri = CalcolaMq(p.Tipo, p.Id),
                Haledwall = p.Tipo == "Eventi",
                HaProiettore = p.Tipo == "Riunioni" || p.Tipo == "Team",
                HaFinestre = new[] { "Singola", "Ristorante", "Team", "Riunioni", "Eventi" }.Contains(p.Tipo),
                Descrizione = GeneraDescrizione(p.Tipo, p.Id)
            });

            return Json(postazioniArricchite);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Prenota([FromBody] PrenotaRequest request)
        {
            if (!ModelState.IsValid || request.PostazioniIds == null || !request.PostazioniIds.Any())
                return BadRequest(new { success = false, message = "Selezionare almeno una postazione." });

            // Recuperiamo l'ID reale dell'utente loggato
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name ?? "Utente_Sconosciuto";

            try
            {
                // 1. Controllo Conflitti
                var conflitti = await _dbContext.Prenotazioni
                    .Where(p => request.PostazioniIds.Contains(p.PostazioneId) && p.DataPrenotazione.Date == request.Data.Date)
                    .Select(p => p.Postazione.Nome)
                    .ToListAsync();

                if (conflitti.Any())
                {
                    var nomi = string.Join(", ", conflitti.Distinct());
                    return BadRequest(new { success = false, message = $"Già occupati: {nomi}." });
                }

                // 2. Recupero Entità dal DB
                var postazioniDb = await _dbContext.Postazioni
                    .Where(p => request.PostazioniIds.Contains(p.Id))
                    .ToListAsync();

                if (postazioniDb.Count != request.PostazioniIds.Count)
                    return BadRequest(new { success = false, message = "Alcune postazioni non esistono." });

                // 3. Controllo Capienza e Creazione Prenotazione
                foreach (var postazione in postazioniDb)
                {
                    int capienzaMax = postazione.PostiTotali > 0 ? postazione.PostiTotali : 1;
                    if (request.NumeroPersone > capienzaMax)
                        return BadRequest(new { success = false, message = $"'{postazione.Nome}': max {capienzaMax} persone." });

                    _dbContext.Prenotazioni.Add(new Template.Services.Shared.Prenotazione
                    {
                        PostazioneId = postazione.Id,
                        DataPrenotazione = request.Data,
                        UserId = userId, // Salvataggio ID Reale
                        NumeroPersone = request.NumeroPersone,
                        DataCreazione = DateTime.Now
                    });
                }

                // 4. Salvataggio (SENZA TRANSAZIONE per compatibilità InMemory)
                await _dbContext.SaveChangesAsync();

                return Ok(new { success = true, message = $"{request.PostazioniIds.Count} spazi prenotati!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Errore server: " + ex.Message });
            }
        }

        private static int CalcolaMq(string tipo, int id) => tipo switch
        {
            "Singola" => 12,
            "Team" => id % 2 == 0 ? 25 : 27,
            "Riunioni" => id % 2 == 0 ? 23 : 25,
            "Eventi" => 112,
            "Ristorante" => 77,
            _ => 0
        };

        private static string GeneraDescrizione(string tipo, int id)
        {
            const string baseComfort = "WiFi Ultra, A/C, Cam.";
            const string parquet = "Parquet, Locker. " + baseComfort;

            return tipo switch
            {
                "Singola" => $"Open space luminoso. {baseComfort}",
                "Team" => id % 2 == 0 ? $"Frontend Beta: iMac Pro. {parquet}" : $"Backend Alpha: Doppio Schermo. {parquet}",
                "Riunioni" => id % 2 == 0 ? $"Blue Room: Insonorizzata. {parquet}" : $"Creative: Pareti scrivibili. {parquet}",
                "Eventi" => $"Led Wall 4K, Audio Dolby. {baseComfort}",
                "Ristorante" => $"Menu del giorno e relax. {baseComfort}",
                _ => baseComfort
            };
        }
    }
}