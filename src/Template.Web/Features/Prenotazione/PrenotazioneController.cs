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
                // 1. Calcoliamo quante persone ci sono
                int personeTotaliSedute = p.Prenotazioni
                    .Where(pren => pren.DataPrenotazione.Date == dataRichiesta.Date && !pren.IsCancellata)
                    .Sum(pren => pren.NumeroPersone);

                // 2. LOGICA COLORE (IsOccupata)
                bool isOccupata;

                if (p.Tipo == "Ristorante")
                {
                    // Ristorante: Rosso solo se PIENO
                    isOccupata = personeTotaliSedute >= p.PostiTotali;
                }
                else
                {
                    // Uffici/Meeting/Team: Rosso appena c'è QUALCUNO (Uso Esclusivo)
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
                    
                    // Qui passiamo il valore calcolato con la logica differenziata
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

        [HttpPost]
        public virtual async Task<IActionResult> Prenota([FromBody] PrenotaRequest request)
        {
            if (!ModelState.IsValid || request.PostazioniIds == null || !request.PostazioniIds.Any())
                return BadRequest(new { success = false, message = "Selezionare almeno una postazione." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name ?? "Utente_Sconosciuto";

            try
            {
                // 1. Recupero Entità Postazioni dal DB
                var postazioniDb = await _dbContext.Postazioni
                    .Where(p => request.PostazioniIds.Contains(p.Id))
                    .ToListAsync();

                if (postazioniDb.Count != request.PostazioniIds.Count)
                    return BadRequest(new { success = false, message = "Alcune postazioni non esistono." });

                // 2. Controllo Disponibilità e Creazione
                foreach (var postazione in postazioniDb)
                {
                    // Calcoliamo chi c'è già
                    int postiGiaOccupati = _dbContext.Prenotazioni
                        .Where(p => p.PostazioneId == postazione.Id
                                    && p.DataPrenotazione.Date == request.Data.Date
                                    && !p.IsCancellata)
                        .Sum(p => p.NumeroPersone);

                    // --- LOGICA DI BLOCCO ---
                    
                    if (postazione.Tipo == "Ristorante")
                    {
                        // A. LOGICA RISTORANTE (Condiviso)
                        int capienzaMax = postazione.PostiTotali > 0 ? postazione.PostiTotali : 1;
                        if (postiGiaOccupati + request.NumeroPersone > capienzaMax)
                        {
                            return BadRequest(new { 
                                success = false, 
                                message = $"'{postazione.Nome}': spazio insufficiente. Liberi: {capienzaMax - postiGiaOccupati}." 
                            });
                        }
                    }
                    else
                    {
                        // B. LOGICA UFFICI/MEETING (Esclusiva)
                        // Se c'è anche solo 1 persona seduta, la stanza è BLOCCATA per gli altri
                        if (postiGiaOccupati > 0)
                        {
                            return BadRequest(new { 
                                success = false, 
                                message = $"'{postazione.Nome}' è già occupata per questa data." 
                            });
                        }
                    }

                    // Se tutto ok, procedo
                    _dbContext.Prenotazioni.Add(new Template.Entities.Prenotazione
                    {
                        PostazioneId = postazione.Id,
                        DataPrenotazione = request.Data,
                        UserId = userId,
                        NumeroPersone = request.NumeroPersone,
                        DataCreazione = DateTime.UtcNow,
                        IsCancellata = false
                    });
                }

                await _dbContext.SaveChangesAsync();

                return Ok(new { success = true, message = $"{request.PostazioniIds.Count} spazi prenotati!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Errore server: " + ex.Message });
            }
        }

        // --- FUNZIONI DI SUPPORTO ---
        private static int CalcolaMq(string tipo, int id) => tipo switch
        {
            "Singola" => 12, "Team" => 25, "Riunioni" => 23, "Eventi" => 112, "Ristorante" => 77, _ => 0
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