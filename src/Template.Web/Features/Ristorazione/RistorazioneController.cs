
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Template.Services.Shared;
using Template.Services;

namespace Template.Web.Features.Ristorazione
{
    [Authorize]
    public partial class RistorazioneController : Controller
    {
        private readonly SharedService _sharedService;
        private readonly TemplateDbContext _dbContext;

        public RistorazioneController(SharedService sharedService, TemplateDbContext dbContext)
        {
            _sharedService = sharedService;
            _dbContext = dbContext;
        }

        public virtual IActionResult Index() { return View(); }

        [HttpGet]
        public virtual async Task<IActionResult> GetTavoli(DateTime? data)
        {
            var dataRichiesta = data ?? DateTime.Today;
            var result = await _sharedService.GetRistorante(dataRichiesta);
            return Json(result.Tavoli);
        }

        [HttpPost]
        public virtual async Task<IActionResult> PrenotaTavolo([FromBody] PrenotaTavoloRequest request)
        {
            try
            {
                // Recuperiamo l'ID reale dell'utente
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name ?? "Utente_Sconosciuto";

                // 1. REGOLA: HAI UNA SCRIVANIA?
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

                // 2. REGOLA: CONTROLLO POSTI
                var postiGiaOccupati = await _dbContext.Prenotazioni
                    .CountAsync(p => p.PostazioneId == request.PostazioneId && p.DataPrenotazione.Date == request.Data.Date);

                if (postiGiaOccupati + request.NumeroPosti > 4)
                {
                    return BadRequest(new { success = false, message = $"Posti insufficienti! Rimasti: {4 - postiGiaOccupati}" });
                }

                // 3. SALVATAGGIO
                for (int i = 0; i < request.NumeroPosti; i++)
                {
                    _dbContext.Prenotazioni.Add(new Template.Services.Shared.Prenotazione
                    {
                        PostazioneId = request.PostazioneId,
                        DataPrenotazione = request.Data,
                        UserId = currentUserId, // Salvataggio ID Reale
                        DataCreazione = DateTime.Now
                    });
                }

                // Salvataggio senza transazione (compatibile con InMemory DB)
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