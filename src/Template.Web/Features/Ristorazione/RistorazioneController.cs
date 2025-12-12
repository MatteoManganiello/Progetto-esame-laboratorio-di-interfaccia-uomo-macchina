using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

// 1. NAMESPACE AGGIORNATI
using Template.Data;      // Per TemplateDbContext
using Template.Entities;  // Per Prenotazione e Postazione

namespace Template.Web.Features.Ristorazione
{
    [Authorize]
    public partial class RistorazioneController : Controller
    {
        // 2. RIMOSSO SharedService (Non esiste più)
        private readonly TemplateDbContext _dbContext;

        public RistorazioneController(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public virtual IActionResult Index() { return View(); }

        [HttpGet]
        public virtual async Task<IActionResult> GetTavoli(DateTime? data)
        {
            var dataRichiesta = data ?? DateTime.Today;

            // 3. FIX: Sostituito _sharedService.GetRistorante con query diretta
            // Recuperiamo i tavoli (Postazioni di tipo Ristorante)
            var tavoli = await _dbContext.Postazioni
                .Where(p => p.Tipo == "Ristorante")
                .Select(p => new 
                {
                    p.Id,
                    p.Nome,
                    // Calcoliamo i posti occupati per quella data
                    PostiOccupati = p.Prenotazioni.Count(pren => pren.DataPrenotazione.Date == dataRichiesta.Date),
                    PostiTotali = 4 // Assumiamo 4 come da tua logica sotto
                })
                .ToListAsync();

            return Json(tavoli);
        }

        [HttpPost]
        public virtual async Task<IActionResult> PrenotaTavolo([FromBody] PrenotaTavoloRequest request)
        {
            try
            {
                // Recuperiamo l'ID reale dell'utente
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name ?? "Utente_Sconosciuto";

                // 1. REGOLA: HAI UNA SCRIVANIA? (Logica invariata)
                var haScrivania = await _dbContext.Prenotazioni
                    .Include(p => p.Postazione)
                    .AnyAsync(p =>
                        p.UserId == currentUserId &&
                        p.DataPrenotazione.Date == request.Data.Date &&
                        p.Postazione.Tipo != "Ristorante"
                    );

                // IMPORTANTE: Decommenta le righe sotto per ATTIVARE il blocco "Niente scrivania, niente cibo"
                // if (!haScrivania) 
                // {
                //      return BadRequest(new { success = false, message = "Devi prenotare una scrivania prima di poter prenotare il pranzo!" });
                // }

                // 2. REGOLA: CONTROLLO POSTI (Logica invariata)
                var postiGiaOccupati = await _dbContext.Prenotazioni
                    .CountAsync(p => p.PostazioneId == request.PostazioneId && p.DataPrenotazione.Date == request.Data.Date);

                if (postiGiaOccupati + request.NumeroPosti > 4)
                {
                    return BadRequest(new { success = false, message = $"Posti insufficienti! Rimasti: {4 - postiGiaOccupati}" });
                }

                // 3. SALVATAGGIO (Logica invariata)
                for (int i = 0; i < request.NumeroPosti; i++)
                {
                    // 4. FIX: Usiamo la nuova Entità Template.Entities.Prenotazione
                    _dbContext.Prenotazioni.Add(new Template.Entities.Prenotazione
                    {
                        PostazioneId = request.PostazioneId,
                        DataPrenotazione = request.Data,
                        UserId = currentUserId, // Salvataggio ID Reale
                        DataCreazione = DateTime.Now
                    });
                }

                // Salvataggio
                await _dbContext.SaveChangesAsync();

                return Ok(new { success = true, message = $"Prenotazione ristorante confermata per {request.NumeroPosti} persone!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Errore: " + ex.Message });
            }
        }

        public class PrenotaTavoloRequest
        {
            public int PostazioneId { get; set; }
            public DateTime Data { get; set; }
            public int NumeroPosti { get; set; }
        }
    }
}