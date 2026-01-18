using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Template.Services.Prenotazioni;

namespace Template.Web.Features.Prenotazione
{
    [Authorize]
    public partial class PrenotazioneController : Controller
    {
        private readonly PrenotazioneService _prenotazioneService;

        public PrenotazioneController(PrenotazioneService prenotazioneService)
        {
            _prenotazioneService = prenotazioneService;
        }

        public virtual IActionResult Mappa() => View();

        [HttpGet]
        public virtual async Task<IActionResult> GetDatiMappa(DateTime? data)
        {
            var datiMappa = await _prenotazioneService.GetDatiMappaAsync(data);
            return Json(datiMappa);
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
    }
}