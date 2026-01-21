using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Globalization;
using System.Threading.Tasks;
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

        /// <summary>
        /// Verifica se l'utente è un Admin (Admin o SuperAdmin)
        /// </summary>
        private bool IsAdmin()
        {
            var userIdentity = User.Identity;
            if (userIdentity == null || !userIdentity.IsAuthenticated)
                return false;

            // Idealmente leggeresti il ruolo da claims, ma per ora verifichi dal cookie
            var role = User.FindFirst("Ruolo")?.Value ?? "User";
            return role == RuoliCostanti.ADMIN || role == RuoliCostanti.SUPER_ADMIN;
        }

        [HttpGet("Dashboard")]
        public virtual IActionResult Dashboard()
        {
            if (!IsAdmin())
                return Unauthorized();

            ViewBag.Title = "Dashboard Admin";
            return View();
        }

        [HttpGet("InfoCards/Edit")]
        public virtual IActionResult EditInfoCards()
        {
            if (!IsAdmin())
                return Unauthorized();

            // Carica i dati attuali da ViewBag o da un servizio
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

            ViewBag.Notifiche = HttpContext.Session.GetString(NotificheKey) ?? string.Empty;
            return View();
        }

        [HttpPost("Notifiche")]
        public virtual IActionResult SaveNotifiche([FromForm] string notifiche)
        {
            if (!IsAdmin())
                return Unauthorized();

            HttpContext.Session.SetString(NotificheKey, notifiche ?? string.Empty);
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
            // Expected format: YYYY-Www
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
                // TODO: Salvare i dati nel database
                // Per ora restituisci il successo
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
}
