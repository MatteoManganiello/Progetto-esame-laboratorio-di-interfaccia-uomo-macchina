using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Template.Data;
using Template.Infrastructure;
using Template.Web.Features.Admin;
using Template.Web.Infrastructure;

namespace Template.Web.Features.SuperAdmin
{
    [Route("[controller]")]
    public partial class SuperAdminController : Controller
    {
        private readonly TemplateDbContext _context;

        public SuperAdminController(TemplateDbContext context)
        {
            _context = context;
        }

        // GET: /SuperAdmin/Dashboard
        [HttpGet("Dashboard")]
        public virtual async Task<IActionResult> Dashboard()
        {
            // Verifica che l'utente sia SuperAdmin
            var userRole = User.FindFirst("Ruolo")?.Value;
            Console.WriteLine($"[DEBUG SuperAdmin] userRole dal claim: '{userRole}'");
            Console.WriteLine($"[DEBUG SuperAdmin] RuoliCostanti.SUPER_ADMIN: '{RuoliCostanti.SUPER_ADMIN}'");
            Console.WriteLine($"[DEBUG SuperAdmin] Utente autenticato: {User.Identity?.IsAuthenticated}");
            Console.WriteLine($"[DEBUG SuperAdmin] Claims totali: {User.Claims.Count()}");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"[DEBUG SuperAdmin] Claim: {claim.Type} = {claim.Value}");
            }
            
            if (userRole != RuoliCostanti.SUPER_ADMIN)
            {
                Console.WriteLine($"[DEBUG SuperAdmin] ACCESSO NEGATO - Ruolo non è SuperAdmin");
                return Forbid();
            }

            Console.WriteLine($"[DEBUG SuperAdmin] Accesso consentito");

            var stats = await BuildStatsAsync();
            return View(stats);
        }

        [HttpPost("NotificheAdmin")]
        [ValidateAntiForgeryToken]
        public virtual IActionResult SaveNotificheAdmin([FromForm] NotificaItem notifica)
        {
            var userRole = User.FindFirst("Ruolo")?.Value;
            if (userRole != RuoliCostanti.SUPER_ADMIN)
            {
                return Forbid();
            }

            // Salva il messaggio nel database
            var messaggio = new Template.Entities.MessaggioSuperAdmin
            {
                Titolo = notifica.Titolo,
                Contenuto = notifica.Contenuto,
                Data = DateTime.Now.ToString("yyyy-MM-dd"),
                DataCreazione = DateTime.UtcNow
            };
            _context.MessaggiSuperAdmin.Add(messaggio);
            _context.SaveChanges();

            TempData["SuccessMessageSuperAdmin"] = "Notifica inviata agli admin.";
            return RedirectToAction(nameof(Dashboard));
        }

        private async Task<SuperAdminStatsViewModel> BuildStatsAsync()
        {
            var stats = new SuperAdminStatsViewModel();

            // 1. Totale utenti registrati
            stats.TotalUtenti = await _context.Users.CountAsync();

            // 2. Utenti per ruolo
            stats.UtentiPerRuolo = new Dictionary<string, int>
            {
                { "SuperAdmin", await _context.Users.CountAsync(u => u.Ruolo == RuoliCostanti.SUPER_ADMIN) },
                { "Admin", await _context.Users.CountAsync(u => u.Ruolo == RuoliCostanti.ADMIN) },
                { "User", await _context.Users.CountAsync(u => u.Ruolo == RuoliCostanti.USER) }
            };

            // 3. Prenotazioni totali
            stats.TotalPrenotazioni = await _context.Prenotazioni.CountAsync();

            // 4. Prenotazioni attive (non cancellate e future)
            stats.PrenotazioniAttive = await _context.Prenotazioni
                .CountAsync(p => !p.IsCancellata && p.DataPrenotazione >= DateTime.Now);

            // 5. Prenotazioni cancellate
            stats.PrenotazioniCancellate = await _context.Prenotazioni
                .CountAsync(p => p.IsCancellata);

            // 6. Spesa totale
            stats.SpesaTotale = await _context.Prenotazioni
                .Where(p => !p.IsCancellata)
                .SumAsync(p => p.Prezzo);

            // 7. Spesa media per prenotazione
            stats.SpesaMedia = stats.TotalPrenotazioni > 0
                ? Math.Round(stats.SpesaTotale / stats.TotalPrenotazioni, 2)
                : 0;

            // 8. Utilizzo postazioni (più prenotate)
            stats.PostazioniPiuPrenotate = await _context.Prenotazioni
                .Where(p => !p.IsCancellata)
                .Include(p => p.Postazione)
                .GroupBy(p => p.Postazione.Nome)
                .Select(g => new PostazioneStatsDto
                {
                    Nome = g.Key,
                    NumPrenotazioni = g.Count(),
                    SpesaTotale = g.Sum(p => p.Prezzo)
                })
                .OrderByDescending(x => x.NumPrenotazioni)
                .Take(10)
                .ToListAsync();

            // 9. Utenti con più spesa (senza navigation property, userò join)
            var utentiSpesaQuery = from p in _context.Prenotazioni.Where(p => !p.IsCancellata)
                                   join u in _context.Users on p.UserId equals u.Id.ToString()
                                   group new { p, u } by new { u.Email, u.FirstName } into g
                                   select new UtenteSpesaDto
                                   {
                                       Email = g.Key.Email,
                                       Nome = g.Key.FirstName,
                                       NumPrenotazioni = g.Count(),
                                       SpesaTotale = g.Sum(x => x.p.Prezzo)
                                   };

            stats.UtentiMaggiorSpesa = await utentiSpesaQuery
                .OrderByDescending(x => x.SpesaTotale)
                .Take(10)
                .ToListAsync();

            // 10. Statistiche giornaliere ultimi 7 giorni
            stats.StatsGiornaliere = await GetStatsGiornaliere();

            // 11. Spesa per sezione (per tipo postazione)
            stats.SpesaPerSezione = await GetSpesaPerSezione();

            // 12. Prenotazioni settimanali (settimana corrente)
            var weekStart = GetWeekStart(DateTime.Today);
            var weekEnd = weekStart.AddDays(7);
            stats.PrenotazioniSettimanali = await _context.Prenotazioni
                .CountAsync(p => p.DataCreazione >= weekStart && p.DataCreazione < weekEnd);

            // 13. Ultimi ordini (per SuperAdmin view) - últimos 20
            stats.RecentOrders = await (from p in _context.Prenotazioni.Where(p => !p.IsCancellata)
                                        join u in _context.Users on p.UserId equals u.Id.ToString()
                                        join pos in _context.Postazioni on p.PostazioneId equals pos.Id
                                        orderby p.DataCreazione descending
                                        select new RecentOrderDto
                                        {
                                            UserEmail = u.Email,
                                            DataCreazione = p.DataCreazione,
                                            Descrizione = pos.Nome + (p.NumeroPersone > 1 ? $" (x{p.NumeroPersone})" : ""),
                                            Prezzo = p.Prezzo
                                        }).Take(20).ToListAsync();

            return stats;
        }

        private async Task<List<GiornataStatsDto>> GetStatsGiornaliere()
        {
            var ultimi7Giorni = Enumerable.Range(0, 7)
                .Select(i => DateTime.Now.AddDays(-i).Date)
                .OrderBy(d => d)
                .ToList();

            var stats = new List<GiornataStatsDto>();

            foreach (var giorno in ultimi7Giorni)
            {
                var prenotazioniGiorno = await _context.Prenotazioni
                    .Where(p => p.DataCreazione.Date == giorno)
                    .ToListAsync();

                stats.Add(new GiornataStatsDto
                {
                    Data = giorno,
                    NumPrenotazioni = prenotazioniGiorno.Count,
                    SpesaTotale = prenotazioniGiorno.Where(p => !p.IsCancellata).Sum(p => p.Prezzo)
                });
            }

            return stats;
        }

        private static DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-1 * diff);
        }

        private async Task<List<SpesaSezioneDto>> GetSpesaPerSezione()
        {
            var prenotazioni = await _context.Prenotazioni
                .Where(p => !p.IsCancellata)
                .Include(p => p.Postazione)
                .ToListAsync();

            var grouped = prenotazioni
                .GroupBy(p => NormalizeSezione(p.Postazione?.Nome, p.Postazione?.Tipo))
                .ToDictionary(g => g.Key, g => new SpesaSezioneDto
                {
                    Sezione = g.Key,
                    SpesaTotale = g.Sum(x => x.Prezzo),
                    NumPrenotazioni = g.Count()
                });

            var sezioniRichieste = new List<string>
            {
                "Sala eventi",
                "Sala meeting",
                "Dev team",
                "Desk room",
                "Ristorante"
            };

            var lista = sezioniRichieste
                .Select(sezione => grouped.TryGetValue(sezione, out var value)
                    ? value
                    : new SpesaSezioneDto { Sezione = sezione, SpesaTotale = 0, NumPrenotazioni = 0 })
                .OrderByDescending(x => x.SpesaTotale)
                .ThenByDescending(x => x.NumPrenotazioni)
                .ToList();

            return lista;
        }

        private static string NormalizeSezione(string nome, string tipo)
        {
            if (!string.IsNullOrWhiteSpace(nome))
            {
                var n = nome.Trim();
                if (n.Contains("SALA EVENTI", StringComparison.OrdinalIgnoreCase))
                    return "Sala eventi";
                if (n.Contains("MEETING", StringComparison.OrdinalIgnoreCase))
                    return "Sala meeting";
                if (n.Contains("DEV TEAM", StringComparison.OrdinalIgnoreCase))
                    return "Dev team";
                if (n.Contains("DESK ROOM", StringComparison.OrdinalIgnoreCase))
                    return "Desk room";
                if (n.Contains("RISTORANTE", StringComparison.OrdinalIgnoreCase))
                    return "Ristorante";
            }

            if (!string.IsNullOrWhiteSpace(tipo))
            {
                var t = tipo.Trim();
                if (t.Equals("Riunioni", StringComparison.OrdinalIgnoreCase))
                    return "Sala meeting";
                if (t.Equals("Ristorante", StringComparison.OrdinalIgnoreCase))
                    return "Ristorante";
                if (t.Equals("Eventi", StringComparison.OrdinalIgnoreCase))
                    return "Sala eventi";
            }

            return "Desk room";
        }

        // GET: /SuperAdmin/UtentiDettagli
        [HttpGet("UtentiDettagli")]
        public virtual async Task<IActionResult> UtentiDettagli()
        {
            var userRole = User.FindFirst("Ruolo")?.Value;
            if (userRole != RuoliCostanti.SUPER_ADMIN)
            {
                return Forbid();
            }

            var utenti = await _context.Users
                .Select(u => new UtenteDettaglioDto
                {
                    Id = u.Id.ToString(),
                    Email = u.Email,
                    Nome = u.FirstName + " " + u.LastName,
                    Ruolo = u.Ruolo
                })
                .ToListAsync();

            foreach (var utente in utenti)
            {
                var spese = await _context.Prenotazioni
                    .Where(p => p.UserId == utente.Id && !p.IsCancellata)
                    .ToListAsync();

                utente.NumPrenotazioni = spese.Count;
                utente.SpesaTotale = spese.Sum(p => p.Prezzo);
            }

            return View(utenti);
        }

        // GET: /SuperAdmin/StatsJson
        [HttpGet("StatsJson")]
        public virtual async Task<IActionResult> StatsJson()
        {
            var userRole = User.FindFirst("Ruolo")?.Value;
            if (userRole != RuoliCostanti.SUPER_ADMIN)
            {
                return Forbid();
            }

            var stats = await BuildStatsAsync();
            // Usa serializer camelCase per coerenza lato client
            return Content(Template.Web.Infrastructure.JsonSerializer.ToJsonCamelCase(stats), "application/json");
        }
    }

    // DTOs per le statistiche
    public class SuperAdminStatsViewModel
    {
        public int TotalUtenti { get; set; }
        public Dictionary<string, int> UtentiPerRuolo { get; set; }
        public int TotalPrenotazioni { get; set; }
        public int PrenotazioniAttive { get; set; }
        public int PrenotazioniCancellate { get; set; }
        public decimal SpesaTotale { get; set; }
        public decimal SpesaMedia { get; set; }
        public List<PostazioneStatsDto> PostazioniPiuPrenotate { get; set; }
        public List<UtenteSpesaDto> UtentiMaggiorSpesa { get; set; }
        public List<GiornataStatsDto> StatsGiornaliere { get; set; }
        public List<SpesaSezioneDto> SpesaPerSezione { get; set; }
        public int PrenotazioniSettimanali { get; set; }
        public List<RecentOrderDto> RecentOrders { get; set; }
    }

    public class PostazioneStatsDto
    {
        public string Nome { get; set; }
        public int NumPrenotazioni { get; set; }
        public decimal SpesaTotale { get; set; }
    }

    public class UtenteSpesaDto
    {
        public string Email { get; set; }
        public string Nome { get; set; }
        public int NumPrenotazioni { get; set; }
        public decimal SpesaTotale { get; set; }
    }

    public class GiornataStatsDto
    {
        public DateTime Data { get; set; }
        public int NumPrenotazioni { get; set; }
        public decimal SpesaTotale { get; set; }
    }

    public class SpesaSezioneDto
    {
        public string Sezione { get; set; }
        public int NumPrenotazioni { get; set; }
        public decimal SpesaTotale { get; set; }
    }


    public class UtenteDettaglioDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Nome { get; set; }
        public string Ruolo { get; set; }
        public int NumPrenotazioni { get; set; }
        public decimal SpesaTotale { get; set; }
    }

    public class RecentOrderDto
    {
        public string UserEmail { get; set; }
        public DateTime DataCreazione { get; set; }
        public string Descrizione { get; set; }
        public decimal Prezzo { get; set; }
    }
}
