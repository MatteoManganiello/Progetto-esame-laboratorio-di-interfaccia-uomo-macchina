using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Template.Infrastructure;
using Template.Web.Infrastructure;
using Template.Services.Utenti;

namespace Template.Web.Features.Login
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [Alerts]
    [ModelStateToTempData]
    public partial class LoginController : Controller
    {
        public static string LoginErrorModelStateKey = "LoginError";

        private readonly UserQueries _userQueries;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

        public LoginController(UserQueries userQueries, IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _userQueries = userQueries;
            _sharedLocalizer = sharedLocalizer;
        }

        [HttpGet]
        public virtual IActionResult Login(string returnUrl)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Mappa", "Prenotazione");

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async virtual Task<ActionResult> Login(LoginViewModel model)
        {
            Console.WriteLine($"[DEBUG LOGIN] Tentativo di accesso: Email='{model.Email}' Password='{model.Password}'");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("[DEBUG LOGIN] ModelState NON VALIDO!");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"[DEBUG ERROR] Campo: {state.Key}, Errore: {error.ErrorMessage}");
                    }
                }
                return View(model);
            }

            try
            {
                Console.WriteLine("[DEBUG LOGIN] Chiamata a UserQueries...");
                
                var utente = await _userQueries.Query(new CheckLoginCredentialsQuery 
                { 
                    Email = model.Email?.Trim(), 
                    Password = model.Password 
                });

                Console.WriteLine($"[DEBUG LOGIN] Successo! Utente trovato: {utente.Email} (ID: {utente.Id})");

                return await LoginAndRedirect(utente.Email, utente.Id.ToString(), model.ReturnUrl, model.RememberMe);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG LOGIN] ECCEZIONE: {ex.Message}");
                ModelState.AddModelError(LoginErrorModelStateKey, "Errore: " + ex.Message);
            }

            Console.WriteLine("[DEBUG LOGIN] Ritorno alla vista Login (Fallito)");
            return View(model);
        }

        private async Task<ActionResult> LoginAndRedirect(string username, string userId, string returnUrl, bool rememberMe)
        {
            // Recupera l'utente per ottenere il ruolo
            var utente = await _userQueries.Query(new UserDetailQuery { Id = Guid.Parse(userId) });
            var ruolo = utente?.Ruolo ?? RuoliCostanti.USER;
            
            Console.WriteLine($"[DEBUG LoginAndRedirect] Utente trovato: {utente?.Email}");
            Console.WriteLine($"[DEBUG LoginAndRedirect] Utente.Ruolo dal database: '{utente?.Ruolo}' (è null? {utente?.Ruolo == null})");
            Console.WriteLine($"[DEBUG LoginAndRedirect] ruolo utilizzato per il confronto: '{ruolo}'");
            Console.WriteLine($"[DEBUG LoginAndRedirect] RuoliCostanti.SUPER_ADMIN = '{RuoliCostanti.SUPER_ADMIN}'");
            Console.WriteLine($"[DEBUG LoginAndRedirect] RuoliCostanti.ADMIN = '{RuoliCostanti.ADMIN}'");
            Console.WriteLine($"[DEBUG LoginAndRedirect] Confronto SUPER_ADMIN: ruolo == RuoliCostanti.SUPER_ADMIN = {ruolo == RuoliCostanti.SUPER_ADMIN}");
            Console.WriteLine($"[DEBUG LoginAndRedirect] Confronto ADMIN: ruolo == RuoliCostanti.ADMIN = {ruolo == RuoliCostanti.ADMIN}");


            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, username),
                new Claim("Ruolo", ruolo)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), 
                new AuthenticationProperties
                {
                    ExpiresUtc = (rememberMe) ? DateTimeOffset.UtcNow.AddMonths(3) : null,
                    IsPersistent = rememberMe,
                });

            Console.WriteLine("[DEBUG LOGIN] Cookie creato. Reindirizzamento...");
            Console.WriteLine($"[DEBUG] Ruolo recuperato: '{ruolo}'");
            Console.WriteLine($"[DEBUG] RuoliCostanti.SUPER_ADMIN: '{RuoliCostanti.SUPER_ADMIN}'");
            Console.WriteLine($"[DEBUG] Confronto SUPER_ADMIN: {ruolo == RuoliCostanti.SUPER_ADMIN}");

            // Reindirizza in base al ruolo
            if (ruolo == RuoliCostanti.SUPER_ADMIN)
            {
                Console.WriteLine("[DEBUG] Reindirizzamento a SuperAdmin/Dashboard");
                return RedirectToAction("Dashboard", "SuperAdmin");
            }

            if (ruolo == RuoliCostanti.ADMIN)
            {
                Console.WriteLine("[DEBUG] Reindirizzamento a Admin/Dashboard");
                return RedirectToAction("Dashboard", "Admin");
            }
            
            Console.WriteLine("[DEBUG] Reindirizzamento a Prenotazione/Mappa");

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Mappa", "Prenotazione");
        }

        [HttpPost]
        public async virtual Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}