using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Template.Services.Shared;
using Template.Services; // <--- 1. AGGIUNTO QUESTO PER IL DBCONTEXT

namespace Template.Web.Features.Ristorazione
{
    [AllowAnonymous]
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
            var query = new MappaQuery { Data = data ?? DateTime.Today };
            var mappa = await _sharedService.Query(query);
            // Filtriamo solo i tavoli
            var tavoli = mappa.Postazioni.Where(p => p.Tipo == "Ristorante");
            return Json(tavoli);
        }

        [HttpPost]
        public virtual async Task<IActionResult> PrenotaTavolo([FromBody] PrenotaTavoloRequest request)
        {
            try
            {
                // Simulazione ID utente (in produzione usa User.Identity...)
                var currentUserId = Guid.NewGuid().ToString(); 

                // 1. CONTROLLO: HAI UNA SCRIVANIA OGGI?
                var haScrivania = await _dbContext.Prenotazioni // <--- 2. USA "Prenotazioni" (PLURALE)
                    .Include(p => p.Postazione)
                    .AnyAsync(p => 
                        p.UserId == currentUserId && 
                        p.DataPrenotazione.Date == request.Data.Date &&
                        p.Postazione.Tipo != "Ristorante"
                    );

                // Nota: Per testare senza aver prenotato scrivania, puoi commentare questo IF temporaneamente
                if (!haScrivania)
                {
                    // return BadRequest(new { success = false, message = "Devi prenotare una scrivania prima!" });
                }

                // 2. CONTROLLO POSTI LIBERI
                var postiOccupati = await _dbContext.Prenotazioni // <--- PLURALE
                    .CountAsync(p => p.PostazioneId == request.PostazioneId && p.DataPrenotazione.Date == request.Data.Date);

                if (postiOccupati + request.NumeroPosti > 4)
                {
                    return BadRequest(new { success = false, message = $"Posti insufficienti. Liberi: {4 - postiOccupati}" });
                }

                // 3. SALVATAGGIO
                for (int i = 0; i < request.NumeroPosti; i++)
                {
                    _dbContext.Prenotazioni.Add(new Template.Services.Shared.Prenotazione // <--- PLURALE + Namespace completo per sicurezza
                    {
                        PostazioneId = request.PostazioneId,
                        DataPrenotazione = request.Data,
                        UserId = currentUserId
                    });
                }
                
                await _dbContext.SaveChangesAsync();

                return Ok(new { success = true, message = "Tavolo prenotato!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
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