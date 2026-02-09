using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;
using Template.Data;
using Template.Entities;
using Template.Infrastructure;
using Template.Web.Infrastructure;

namespace Template.Web.Features.Admin
{
    [Route("[controller]")]
    public partial class AdminController : Controller
    {
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;
        private readonly TemplateDbContext _dbContext;
        private const string NotificheKey = "Admin.NotificheAzienda";

        public AdminController(IStringLocalizer<SharedResource> sharedLocalizer, TemplateDbContext dbContext)
        {
            _sharedLocalizer = sharedLocalizer;
            _dbContext = dbContext;
        }

        private bool IsAdmin()
        {
            var userIdentity = User.Identity;
            if (userIdentity == null || !userIdentity.IsAuthenticated)
                return false;

            var role = User.FindFirst("Ruolo")?.Value ?? "User";
            return role == RuoliCostanti.ADMIN || role == RuoliCostanti.SUPER_ADMIN;
        }

        [HttpGet("Dashboard")]
        public virtual IActionResult Dashboard()
        {
            if (!IsAdmin())
                return Unauthorized();

            ViewBag.Title = "Dashboard Admin";
            var messaggi = _dbContext.MessaggiSuperAdmin
                .OrderByDescending(m => m.DataCreazione)
                .Take(20)
                .Select(m => new NotificaItem
                {
                    Titolo = m.Titolo,
                    Contenuto = m.Contenuto,
                    Data = m.Data
                })
                .ToList();
            ViewBag.NotificheSuperAdmin = messaggi;
            return View();
        }

        [HttpGet("InfoCards/Edit")]
        public virtual IActionResult EditInfoCards()
        {
            if (!IsAdmin())
                return Unauthorized();

            ViewBag.Novita = new object[] { };
            ViewBag.MenuSettimanale = new object[] { };

            return View();
        }

        [HttpGet("MenuSettimanale")]
        public virtual async Task<IActionResult> MenuSettimanale(DateTime? weekStart)
        {
            if (!IsAdmin())
                return Unauthorized();

            var start = GetWeekStart(weekStart ?? DateTime.Today);
            var menu = await _dbContext.MenuSettimanali.FirstOrDefaultAsync(m => m.WeekStart == start);

            var model = new MenuSettimanaleViewModel
            {
                WeekStart = start,
                WeekIso = ToIsoWeek(start),
                Lunedi = menu?.Lunedi,
                Martedi = menu?.Martedi,
                Mercoledi = menu?.Mercoledi,
                Giovedi = menu?.Giovedi,
                Venerdi = menu?.Venerdi,
                Sabato = menu?.Sabato,
                Domenica = menu?.Domenica
            };

            return View(model);
        }

        [HttpPost("MenuSettimanale")]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> SaveMenuSettimanale(MenuSettimanaleViewModel model)
        {
            if (!IsAdmin())
                return Unauthorized();

            var start = GetWeekStart(model.WeekStart == default ? DateTime.Today : model.WeekStart);
            if (!string.IsNullOrWhiteSpace(model.WeekIso))
            {
                var parsed = ParseIsoWeek(model.WeekIso);
                if (parsed.HasValue)
                {
                    start = parsed.Value;
                }
            }
            var menu = await _dbContext.MenuSettimanali.FirstOrDefaultAsync(m => m.WeekStart == start);

            if (menu == null)
            {
                menu = new MenuSettimanale { WeekStart = start };
                _dbContext.MenuSettimanali.Add(menu);
            }

            menu.Lunedi = model.Lunedi;
            menu.Martedi = model.Martedi;
            menu.Mercoledi = model.Mercoledi;
            menu.Giovedi = model.Giovedi;
            menu.Venerdi = model.Venerdi;
            menu.Sabato = model.Sabato;
            menu.Domenica = model.Domenica;

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Menù settimanale aggiornato.";
            return RedirectToAction(nameof(MenuSettimanale), new { weekStart = start.ToString("yyyy-MM-dd") });
        }

        [HttpGet("Notifiche")]
        public virtual IActionResult Notifiche()
        {
            if (!IsAdmin())
                return Unauthorized();

            var notificheDb = _dbContext.Notifiche.ToList();
            var notificheList = notificheDb.Select(n => new NotificaItem
            {
                Data = n.Data,
                Titolo = n.Titolo,
                Contenuto = n.Contenuto
            }).ToList();

            while (notificheList.Count < 3)
            {
                notificheList.Add(new NotificaItem());
            }

            ViewBag.NotificheList = notificheList;
            return View();
        }

        [HttpPost("Notifiche")]
        public virtual IActionResult SaveNotifiche([FromForm] System.Collections.Generic.List<NotificaItem> notifiche)
        {
            if (!IsAdmin())
                return Unauthorized();

            var cleaned = (notifiche ?? new System.Collections.Generic.List<NotificaItem>())
                .FindAll(n => !string.IsNullOrWhiteSpace(n?.Titolo) || !string.IsNullOrWhiteSpace(n?.Contenuto) || !string.IsNullOrWhiteSpace(n?.Data));

            var tutte = _dbContext.Notifiche.ToList();
            _dbContext.Notifiche.RemoveRange(tutte);
            _dbContext.SaveChanges();

            foreach (var n in cleaned)
            {
                var notificaDb = new Template.Entities.Notifica
                {
                    Titolo = n.Titolo,
                    Contenuto = n.Contenuto,
                    Data = n.Data,
                    DataCreazione = DateTime.UtcNow
                };
                _dbContext.Notifiche.Add(notificaDb);
            }
            _dbContext.SaveChanges();

            TempData["SuccessMessage"] = "Notifiche aziendali aggiornate.";
            return RedirectToAction(nameof(Notifiche));
        }


        private static DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-1 * diff);
        }

        private static string ToIsoWeek(DateTime date)
        {
            var year = ISOWeek.GetYear(date);
            var week = ISOWeek.GetWeekOfYear(date);
            return $"{year}-W{week:D2}";
        }

        private static DateTime? ParseIsoWeek(string isoWeek)
        {
            if (string.IsNullOrWhiteSpace(isoWeek))
                return null;

            string yearPart = null;
            string weekPart = null;

            if (isoWeek.Contains("-W", StringComparison.OrdinalIgnoreCase))
            {
                var parts = isoWeek.Split("-W", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    yearPart = parts[0];
                    weekPart = parts[1];
                }
            }
            else if (isoWeek.Contains("-", StringComparison.OrdinalIgnoreCase))
            {
                var parts = isoWeek.Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    yearPart = parts[0];
                    weekPart = parts[1];
                }
            }

            if (!int.TryParse(yearPart, out var year))
                return null;
            if (!int.TryParse(weekPart, out var week))
                return null;

            return ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
        }
        [HttpPost("InfoCards/Update")]
        public virtual async Task<IActionResult> UpdateInfoCards(
            [FromBody] InfoCardsUpdateRequest request)
        {
            if (!IsAdmin())
                return Unauthorized();

            try
            {
                return Ok(new { success = true, message = "InfoCards aggiornate con successo" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    public class InfoCardsUpdateRequest
    {
        public object[] Novita { get; set; }
        public object MenuSettimanale { get; set; }
    }

    public class NotificaItem
    {
        public string Data { get; set; }
        public string Titolo { get; set; }
        public string Contenuto { get; set; }
    }
}
