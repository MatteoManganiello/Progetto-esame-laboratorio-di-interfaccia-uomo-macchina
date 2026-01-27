using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Template.Data;
using Template.Web.Features.AreaRiservata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace Template.Web.Features.AreaRiservata
{
    [Authorize]
    public partial class AreaRiservataController : Controller
    {
        private readonly TemplateDbContext _dbContext;
        private readonly ILogger<AreaRiservataController> _logger;
        private readonly IHostEnvironment _env;
        public AreaRiservataController(TemplateDbContext dbContext, ILogger<AreaRiservataController> logger, IHostEnvironment env)
        {
            _dbContext = dbContext;
            _logger = logger;
            _env = env;
        }

        public virtual async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name;
            Guid userGuid;
            Guid.TryParse(userId, out userGuid);
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userGuid);
            var ordini = await _dbContext.Prenotazioni
                .Where(p => p.UserId == userId && !p.IsCancellata)
                .OrderByDescending(p => p.DataCreazione)
                .Take(10)
                .Select(p => new OrdineViewModel
                {
                    Id = p.Id,
                    Data = p.DataCreazione,
                    Descrizione = p.Postazione.Nome + (p.NumeroPersone > 1 ? $" (x{p.NumeroPersone})" : ""),
                    Prezzo = p.Prezzo
                })
                .ToListAsync();

            var acquistiFrequenti = await _dbContext.Prenotazioni
                .Where(p => p.UserId == userId && !p.IsCancellata)
                .GroupBy(p => p.Postazione.Nome)
                .Select(g => new AcquistoFrequenteViewModel
                {
                    Descrizione = g.Key,
                    Quantita = g.Count()
                })
                .OrderByDescending(a => a.Quantita)
                .Take(5)
                .ToListAsync();

            var model = new AreaRiservataViewModel
            {
                Nome = user != null ? $"{user.FirstName} {user.LastName}" : User.Identity.Name,
                Email = user?.Email ?? "",
                Ruolo = User.FindFirst("Ruolo")?.Value ?? "Utente",
                UltimiOrdini = ordini,
                AcquistiFrequenti = acquistiFrequenti
            };
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> CancellaOrdine(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name;
            _logger?.LogInformation("CancellaOrdine called. id={Id} userId={UserId}", id, userId);

            var prenotazione = await _dbContext.Prenotazioni.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId && !p.IsCancellata);
            if (prenotazione == null)
            {
                _logger?.LogWarning("Prenotazione non trovata per id={Id} userId={UserId}", id, userId);
                TempData["OrdineCancellato"] = "Ordine non trovato.";
                return RedirectToAction("Index");
            }

            var ageHours = (DateTime.Now - prenotazione.DataCreazione).TotalHours;
            _logger?.LogInformation("Prenotazione trovata id={Id} creata={DataCreazione} ageHours={Age}", id, prenotazione.DataCreazione, ageHours);

            // Controlla che sia cancellabile (entro 1 ora dalla creazione)
            if (ageHours < 1 || (_env != null && _env.IsDevelopment()))
            {
                _dbContext.Prenotazioni.Remove(prenotazione);
                await _dbContext.SaveChangesAsync();
                _logger?.LogInformation("Prenotazione rimossa id={Id}", id);
                TempData["OrdineCancellato"] = "Ordine cancellato con successo.";
            }
            else
            {
                _logger?.LogInformation("Prenotazione non più cancellabile id={Id} ageHours={Age}", id, ageHours);
                TempData["OrdineCancellato"] = "Non puoi più cancellare questo ordine.";
            }

            return RedirectToAction("Index");
        }

        // DEBUG: Permette la cancellazione via GET solo in Development per test rapido.
        [HttpGet]
        public virtual async Task<IActionResult> DebugDeleteOrder(int id)
        {
            if (_env == null || !_env.IsDevelopment())
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name;
            _logger?.LogInformation("DebugDeleteOrder called. id={Id} userId={UserId}", id, userId);

            var prenotazione = await _dbContext.Prenotazioni.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (prenotazione == null)
            {
                return Content($"Prenotazione non trovata: {id}");
            }

            _dbContext.Prenotazioni.Remove(prenotazione);
            await _dbContext.SaveChangesAsync();
            _logger?.LogInformation("DebugDeleteOrder removed id={Id}", id);
            return Content($"Prenotazione {id} rimossa (Debug)");
        }
    }
}
