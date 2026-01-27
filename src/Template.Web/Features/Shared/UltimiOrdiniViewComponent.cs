using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Template.Data;
using Template.Web.Features.AreaRiservata;

namespace Template.Web.Features.Shared
{
    public class UltimiOrdiniViewComponent : ViewComponent
    {
        private readonly TemplateDbContext _db;

        public UltimiOrdiniViewComponent(TemplateDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsUser = HttpContext?.User as ClaimsPrincipal;
            var userId = claimsUser?.FindFirstValue(ClaimTypes.NameIdentifier) ?? claimsUser?.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
            {
                return View(new System.Collections.Generic.List<UltimoOrdineDto>());
            }

            var ordini = await _db.Prenotazioni
                .Where(p => p.UserId == userId && !p.IsCancellata)
                .OrderByDescending(p => p.DataCreazione)
                .Take(4)
                .Select(p => new UltimoOrdineDto
                {
                    Id = p.Id,
                    Tipo = p.Postazione.Tipo,
                    Descrizione = p.Postazione.Nome + (p.NumeroPersone > 1 ? $" (x{p.NumeroPersone})" : ""),
                    Data = p.DataCreazione,
                    Prezzo = p.Prezzo,
                    Cancellabile = false
                })
                .ToListAsync();

            // Calcola la cancellabilità in memoria (EF non può tradurre DateTime.Now - p.DataCreazione)
            var now = System.DateTime.Now;
            foreach (var o in ordini)
            {
                o.Cancellabile = (now - o.Data).TotalHours < 1;
            }

            return View(ordini);
        }

        public class UltimoOrdineDto
        {
            public int Id { get; set; }
            public string Tipo { get; set; }
            public string Descrizione { get; set; }
            public System.DateTime Data { get; set; }
            public decimal Prezzo { get; set; }
            public bool Cancellabile { get; set; }
        }
    }
}
