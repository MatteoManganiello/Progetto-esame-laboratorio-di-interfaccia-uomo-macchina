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
using Template.Data; 
using Template.Entities;

namespace Template.Web.Features.Login
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [Alerts]
    [ModelStateToTempData]
    public partial class LoginController : Controller
    {
        public static string LoginErrorModelStateKey = "LoginError";

        private readonly UserQueries _userQueries;
        private readonly TemplateDbContext _dbContext;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

        public LoginController(UserQueries userQueries, TemplateDbContext dbContext, IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _userQueries = userQueries;
            _dbContext = dbContext;
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
            // --- DEBUG 1: Siamo entrati nel metodo? ---
            Console.WriteLine($"[DEBUG LOGIN] Tentativo di accesso: Email='{model.Email}' Password='{model.Password}'");

            if (!ModelState.IsValid)
            {
                // --- DEBUG 2: Il form è invalido? ---
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
                // --- DEBUG 3: Chiamiamo la query ---
                Console.WriteLine("[DEBUG LOGIN] Chiamata a UserQueries...");
                
                var utente = await _userQueries.Query(new CheckLoginCredentialsQuery 
                { 
                    Email = model.Email?.Trim(), // Aggiunto Trim per sicurezza
                    Password = model.Password 
                });

                // --- DEBUG 4: Utente trovato ---
                Console.WriteLine($"[DEBUG LOGIN] Successo! Utente trovato: {utente.Email} (ID: {utente.Id})");

                return await LoginAndRedirect(utente.Email, utente.Id.ToString(), model.ReturnUrl, model.RememberMe);
            }
            catch (Exception ex)
            {
                // --- DEBUG 5: Qualcosa è andato storto nella logica ---
                Console.WriteLine($"[DEBUG LOGIN] ECCEZIONE: {ex.Message}");
                ModelState.AddModelError(LoginErrorModelStateKey, "Errore: " + ex.Message);
            }

            // Se siamo qui, qualcosa è fallito
            Console.WriteLine("[DEBUG LOGIN] Ritorno alla vista Login (Fallito)");
            return View(model);
        }

        private async Task<ActionResult> LoginAndRedirect(string username, string userId, string returnUrl, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, username)
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