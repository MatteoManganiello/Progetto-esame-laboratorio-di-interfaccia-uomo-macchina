using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic; // AGGIUNTO: Serve per List<>
using System.Linq;
using System.Security.Claims;     // AGGIUNTO: Serve per ClaimTypes
using System.Threading.Tasks;
using Template.Services.Shared;
using Template.Services;
using Template.Web.Features.Prenotazione.Models;

namespace Template.Web.Features.Prenotazione
{
    [AllowAnonymous]
    public partial class PrenotazioneController : Controller
    {
        private readonly SharedService _sharedService;
        private readonly TemplateDbContext _dbContext;

        public PrenotazioneController(SharedService sharedService, TemplateDbContext dbContext)
        {
            _sharedService = sharedService;
            _dbContext = dbContext;
        }

        public virtual IActionResult Mappa()
        {
            return View();
        }

        [HttpGet]
        public virtual async Task<IActionResult> GetDatiMappa(DateTime? data)
        {
            var dataRichiesta = data ?? DateTime.Today;
            var query = new MappaQuery { Data = dataRichiesta };
            var risultato = await _sharedService.Query(query);

            var postazioniArricchite = risultato.Postazioni.Select(p => new
            {
                p.Id,
                p.Nome,
                p.Tipo,
                p.X,
                p.Y,
                p.Width,
                p.Height,
                p.PostiTotali,
                p.PostiOccupati,
                p.IsOccupata,
                MetriQuadri = CalcolaMq(p.Tipo, p.Id),
                haledwall = p.Tipo == "Eventi",
                HaProiettore = p.Tipo == "Riunioni" || p.Tipo == "Team",
                HaFinestre = p.Tipo == "Singola" || p.Tipo == "Ristorante" || p.Tipo == "Team" || p.Tipo == "Riunioni" || p.Tipo == "Eventi",
                Descrizione = GeneraDescrizione(p.Tipo, p.Id)
            });

            return Json(postazioniArricchite);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Prenota([FromBody] PrenotaRequest request)
        {
            if (!ModelState.IsValid || request.PostazioniIds == null || !request.PostazioniIds.Any())
            {
                return BadRequest(new { success = false, message = "Selezionare almeno una postazione." });
            }

            // 1. Recupero User ID
            var userId = User.Identity?.IsAuthenticated == true
                         ? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name
                         : "Utente_Guest";

            // 2. Transazione Atomica (Tutto o Niente)
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // A. CONTROLLO DISPONIBILITÀ (Bulk Check)
                var postazioniOccupate = await _dbContext.Prenotazioni
                    .Where(p => request.PostazioniIds.Contains(p.PostazioneId)
                             && p.DataPrenotazione.Date == request.Data.Date)
                    .Select(p => p.Postazione.Nome)
                    .ToListAsync();

                if (postazioniOccupate.Any())
                {
                    var nomi = string.Join(", ", postazioniOccupate);
                    return BadRequest(new { success = false, message = $"Impossibile completare: le seguenti postazioni sono già occupate: {nomi}." });
                }

                // B. RECUPERO DETTAGLI POSTAZIONI (Bulk Fetch)
                var postazioniDb = await _dbContext.Postazioni
                    .Where(p => request.PostazioniIds.Contains(p.Id))
                    .ToListAsync();

                if (postazioniDb.Count != request.PostazioniIds.Count)
                {
                    return BadRequest(new { success = false, message = "Una o più postazioni selezionate non esistono." });
                }

                // C. CREAZIONE PRENOTAZIONI
                foreach (var postazione in postazioniDb)
                {
                    int capienzaMax = postazione.PostiTotali > 0 ? postazione.PostiTotali : 1;
                    if (request.NumeroPersone > capienzaMax)
                    {
                        return BadRequest(new { success = false, message = $"La postazione '{postazione.Nome}' non può ospitare {request.NumeroPersone} persone (Max: {capienzaMax})." });
                    }

                    var nuovaPrenotazione = new Template.Services.Shared.Prenotazione
                    {
                        PostazioneId = postazione.Id,
                        DataPrenotazione = request.Data,
                        UserId = userId
                    };

                    _dbContext.Prenotazioni.Add(nuovaPrenotazione);
                }

                // D. SALVATAGGIO
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = $"{request.PostazioniIds.Count} spazi prenotati con successo!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = "Errore durante la prenotazione multipla: " + ex.Message });
            }
        }

        private int CalcolaMq(string tipo, int id)
        {
            switch (tipo)
            {
                case "Singola": return 52;
                case "Team": return id % 2 == 0 ? 25 : 27;
                case "Riunioni": return id % 2 == 0 ? 23 : 25;
                case "Eventi": return 112;
                case "Ristorante": return 77;
                default: return 0;
            }
        }

        private string GeneraDescrizione(string tipo, int id)
        {
            string baseComfort = "WiFi Ultra, A/C, Cam.";

            switch (tipo)
            {
                case "Singola":
                    return $"Open space silenzioso. {baseComfort}";
                case "Team":
                    string teamExtra = "Parquet, Locker. " + baseComfort;
                    return id % 2 == 0
                        ? $"Frontend Beta: iMac Pro e Tavolette. {teamExtra}"
                        : $"Backend Alpha: Lavagne e Schermi doppi. {teamExtra}";
                case "Riunioni":
                    string meetExtra = "Parquet, Locker. " + baseComfort;
                    return id % 2 == 0
                        ? $"Blue Room: Insonorizzata. {meetExtra}"
                        : $"Creative: Pareti scrivibili. {meetExtra}";
                case "Eventi":
                    return $"Led Wall 4K, Audio Dolby. {baseComfort}";
                case "Ristorante":
                    return $"Area ristoro, microonde/caffè. {baseComfort}";
                default: return baseComfort;
            }
        }

        // --- CORREZIONE QUI SOTTO ---
        
    }
}