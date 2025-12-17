using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Template.Data;      
using Template.Entities;  

namespace Template.Web.Features.Ristorazione
{
    [Authorize]
    public partial class RistorazioneController : Controller
    {
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

            var tavoli = await _dbContext.Postazioni
                .Where(p => p.Tipo == "Ristorante")
                .Select(p => new 
                {
                    p.Id,
                    p.Nome,
                    PostiOccupati = p.Prenotazioni.Count(pren => pren.DataPrenotazione.Date == dataRichiesta.Date),
                    PostiTotali = 4 
                })
                .ToListAsync();

            return Json(tavoli);
        }

        [HttpPost]
        public virtual async Task<IActionResult> PrenotaTavolo([FromBody] PrenotaTavoloRequest request)
        {
            try
            {

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name ?? "Utente_Sconosciuto";

                var haScrivania = await _dbContext.Prenotazioni
                    .Include(p => p.Postazione)
                    .AnyAsync(p =>
                        p.UserId == currentUserId &&
                        p.DataPrenotazione.Date == request.Data.Date &&
                        p.Postazione.Tipo != "Ristorante"
                    );

                var postiGiaOccupati = await _dbContext.Prenotazioni
                    .CountAsync(p => p.PostazioneId == request.PostazioneId && p.DataPrenotazione.Date == request.Data.Date);

                if (postiGiaOccupati + request.NumeroPosti > 4)
                {
                    return BadRequest(new { success = false, message = $"Posti insufficienti! Rimasti: {4 - postiGiaOccupati}" });
                }


                for (int i = 0; i < request.NumeroPosti; i++)
                {

                    _dbContext.Prenotazioni.Add(new Template.Entities.Prenotazione
                    {
                        PostazioneId = request.PostazioneId,
                        DataPrenotazione = request.Data,
                        UserId = currentUserId, 
                        DataCreazione = DateTime.Now
                    });
                }

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