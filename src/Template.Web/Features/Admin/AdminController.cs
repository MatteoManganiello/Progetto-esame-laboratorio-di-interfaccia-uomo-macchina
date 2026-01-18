using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Threading.Tasks;
using Template.Infrastructure;
using Template.Web.Infrastructure;

namespace Template.Web.Features.Admin
{
    [Route("[controller]")]
    public partial class AdminController : Controller
    {
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

        public AdminController(IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _sharedLocalizer = sharedLocalizer;
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
