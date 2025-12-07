using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Template.Services.Shared;
using Template.Services; // <--- FONDAMENTALE: Serve per vedere TemplateDbContext

namespace Template.Web.Features.Prenotazione
{
    [AllowAnonymous]
    public partial class PrenotazioneController : Controller
    {
        private readonly SharedService _sharedService;
        private readonly TemplateDbContext _dbContext;

        public PrenotazioneController(SharedService sharedService, TemplateDbContext dbContext)
        {
            _sharedService = sharedService;
            _dbContext = dbContext;
        }

        public virtual IActionResult Mappa() 
        { 
            return View(); 
        }

        [HttpGet]
        public virtual async Task<IActionResult> GetDatiMappa(DateTime? data)
        {
            var dataRichiesta = data ?? DateTime.Today;
            var query = new MappaQuery { Data = dataRichiesta };
            var risultato = await _sharedService.Query(query);
            return Json(risultato.Postazioni);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Prenota([FromBody] PrenotaRequest request)
        {
            try 
            {
                var currentUserId = Guid.NewGuid().ToString(); 

                // 1. CONTROLLO DISPONIBILITÀ ESCLUSIVA
                // Se esiste già UNA prenotazione per quella stanza in quella data, è occupata.
                var isOccupata = await _dbContext.Prenotazioni
                    .AnyAsync(p => p.PostazioneId == request.PostazioneId && p.DataPrenotazione.Date == request.Data.Date);

                if (isOccupata)
                {
                    return BadRequest(new { success = false, message = "Spiacente, questo spazio è già prenotato per la data selezionata." });
                }

                // 2. CONTROLLO CAPIENZA
                // Recuperiamo la postazione per vedere quanti posti ha
                var postazione = await _dbContext.Postazioni.FindAsync(request.PostazioneId);
                
                // Se PostiTotali nel DB è 0 (vecchi dati), usiamo 1 come fallback
                int capienzaMax = postazione.PostiTotali > 0 ? postazione.PostiTotali : 1;

                if (request.NumeroPersone > capienzaMax)
                {
                    return BadRequest(new { success = false, message = $"Il numero di persone ({request.NumeroPersone}) supera la capienza massima della stanza ({capienzaMax})." });
                }

                // 3. SALVATAGGIO
                var nuovaPrenotazione = new Template.Services.Shared.Prenotazione
                {
                    PostazioneId = request.PostazioneId,
                    DataPrenotazione = request.Data,
                    UserId = currentUserId
                };

                _dbContext.Prenotazioni.Add(nuovaPrenotazione);
                await _dbContext.SaveChangesAsync();

                return Ok(new { success = true, message = "Spazio prenotato con successo!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Errore: " + ex.Message });
            }
        }

        // Classe per ricevere i dati dal JSON
        public class PrenotaRequest
        {
            public int PostazioneId { get; set; }
            public DateTime Data { get; set; }
            public int NumeroPersone { get; set; } = 1;
        }
    }
}