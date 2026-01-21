using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private readonly PrenotazioneService _prenotazioneService;
        private readonly TemplateDbContext _dbContext;

        public PrenotazioneController(PrenotazioneService prenotazioneService, TemplateDbContext dbContext)
        {
            _prenotazioneService = prenotazioneService;
            _dbContext = dbContext;
        }

        public virtual async Task<IActionResult> Mappa()
        {
            var weekStart = GetWeekStart(DateTime.Today);
            var menuSettimanale = await GetMenuSettimanaleForDate(DateTime.Today);
            var notifiche = GetNotificheAzienda();
            ViewBag.DashboardData = new
            {
                menuSettimanale,
                menuWarning = menuSettimanale == null,
                weekStart = weekStart.ToString("yyyy-MM-dd"),
                novita = notifiche
            };
            return View();
        }

        [HttpGet]
        public virtual async Task<IActionResult> GetDatiMappa(DateTime? data)
        {
            var datiMappa = await _prenotazioneService.GetDatiMappaAsync(data);
            var targetDate = data ?? DateTime.Today;
            var weekStart = GetWeekStart(targetDate);
            var menuSettimanale = await GetMenuSettimanaleForDate(targetDate);
            var notifiche = GetNotificheAzienda();

            return Json(new
            {
                postazioni = datiMappa,
                menuSettimanale,
                menuWarning = menuSettimanale == null,
                weekStart = weekStart.ToString("yyyy-MM-dd"),
                novita = notifiche
            });
        }

        [HttpPost]
        public virtual async Task<IActionResult> Prenota([FromBody] PrenotaRequest request)
        {
            if (!ModelState.IsValid || request.Elementi == null || !request.Elementi.Any())
                return BadRequest(new { success = false, message = "Il carrello è vuoto." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name ?? "Utente_Sconosciuto";

            var esito = await _prenotazioneService.EseguiPrenotazioneMultiplaAsync(request, userId);

            if (esito.Successo)
                return Ok(new { success = true, message = esito.Messaggio });
            else
                return BadRequest(new { success = false, message = esito.Messaggio });
        }

        private static DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-1 * diff);
        }

        private static Dictionary<string, string> MapMenu(MenuSettimanale menu)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Lun"] = menu.Lunedi,
                ["Mar"] = menu.Martedi,
                ["Mer"] = menu.Mercoledi,
                ["Gio"] = menu.Giovedi,
                ["Ven"] = menu.Venerdi,
                ["Sab"] = menu.Sabato,
                ["Dom"] = menu.Domenica
            };

            var hasValue = result.Values.Any(v => !string.IsNullOrWhiteSpace(v));
            return hasValue ? result : null;
        }

        private async Task<Dictionary<string, string>> GetMenuSettimanaleForDate(DateTime date)
        {
            var weekStart = GetWeekStart(date);
            var menu = await _dbContext.MenuSettimanali.FirstOrDefaultAsync(m => m.WeekStart == weekStart);
            return menu != null ? MapMenu(menu) : null;
        }

        private object[] GetNotificheAzienda()
        {
            var raw = HttpContext.Session.GetString("Admin.NotificheAzienda");
            var list = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<Template.Web.Features.Admin.NotificaItem>>(raw ?? "[]")
                ?? new System.Collections.Generic.List<Template.Web.Features.Admin.NotificaItem>();

            return list
                .Where(n => !string.IsNullOrWhiteSpace(n?.Titolo) || !string.IsNullOrWhiteSpace(n?.Contenuto) || !string.IsNullOrWhiteSpace(n?.Data))
                .Select(n => new
                {
                    data = n.Data,
                    titolo = n.Titolo,
                    contenuto = n.Contenuto
                })
                .ToArray();
        }
    }
}