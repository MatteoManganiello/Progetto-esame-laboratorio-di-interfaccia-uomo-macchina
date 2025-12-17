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
                    HaArmadietto = p.Tipo == "Riunioni" || p.Tipo == "Team",
                    Haledwall = p.Tipo == "Eventi",
                    HaProiettore = p.Tipo == "Riunioni" || p.Tipo == "Team",
                    HaFinestre = new[] { "Singola", "Ristorante", "Team", "Riunioni", "Eventi" }.Contains(p.Tipo),
                    Descrizione = GeneraDescrizione(p.Tipo, p.Id)
                };
            });

            return Json(postazioniArricchite);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Prenota([FromBody] PrenotaRequest request)
        {
            if (!ModelState.IsValid || request.Elementi == null || !request.Elementi.Any())
                return BadRequest(new { success = false, message = "Il carrello è vuoto." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name ?? "Utente_Sconosciuto";

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in request.Elementi)
                {
                    var postazione = await _dbContext.Postazioni.FindAsync(item.PostazioneId);

                    if (postazione == null)
                        throw new Exception($"La postazione ID {item.PostazioneId} non esiste.");

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
                            throw new Exception($"'{postazione.Nome}': spazio insufficiente (Richiesti: {item.NumeroPersone}, Liberi: {capienzaMax - postiGiaOccupati}).");
                        }
                    }
                    else
                    {
                        if (postiGiaOccupati > 0)
                        {
                            throw new Exception($"'{postazione.Nome}' è già occupata.");
                        }
                    }

                    _dbContext.Prenotazioni.Add(new Template.Entities.Prenotazione
                    {
                        PostazioneId = postazione.Id,
                        DataPrenotazione = request.Data,
                        UserId = userId,
                        NumeroPersone = item.NumeroPersone,
                        DataCreazione = DateTime.UtcNow,
                        IsCancellata = false
                    });
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Prenotazione multipla confermata con successo!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

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