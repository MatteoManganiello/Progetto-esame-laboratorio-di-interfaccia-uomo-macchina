using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Template.Data;
using Template.Web.Features.AreaRiservata;

namespace Template.Web.Features.AreaRiservata
{
    [Authorize]
    public partial class AreaRiservataController : Controller
    {
        private readonly TemplateDbContext _dbContext;
        public AreaRiservataController(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public virtual async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name;
            Guid userGuid;
            Guid.TryParse(userId, out userGuid);
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userGuid);
            var ordini = await _dbContext.Prenotazioni
                .Where(p => p.UserId == userId && !p.IsCancellata)
                .OrderByDescending(p => p.DataPrenotazione)
                .Take(10)
                .Select(p => new OrdineViewModel
                {
                    Data = p.DataPrenotazione,
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
    }
}
