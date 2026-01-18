using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Template.Services.Ristorazione;

namespace Template.Web.Features.Ristorazione
{
    [Authorize]
    public partial class RistorazioneController : Controller
    {
        private readonly RistorazioneService _ristorazioneService;

        public RistorazioneController(RistorazioneService ristorazioneService)
        {
            _ristorazioneService = ristorazioneService;
        }

        public virtual IActionResult Index() => View();

        [HttpGet]
        public virtual async Task<IActionResult> GetTavoli(DateTime? data)
        {
            var tavoli = await _ristorazioneService.GetTavoliAsync(data);
            return Json(tavoli);
        }

        [HttpPost]
        public virtual async Task<IActionResult> PrenotaTavolo([FromBody] PrenotaTavoloRequest request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name ?? "Utente_Sconosciuto";
            
            var esito = await _ristorazioneService.PrenotaTavoloAsync(request, currentUserId);

            if (esito.Successo)
                return Ok(new { success = true, message = esito.Messaggio });
            else
                return BadRequest(new { success = false, message = esito.Messaggio });
        }
    }
}