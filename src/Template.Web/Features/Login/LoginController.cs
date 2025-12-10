using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Template.Infrastructure;
using Template.Services.Shared;
using Template.Web.Infrastructure;

namespace Template.Web.Features.Login
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [Alerts]
    [ModelStateToTempData]
    public partial class LoginController : Controller
    {
        public static string LoginErrorModelStateKey = "LoginError";
        private readonly SharedService _sharedService;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

        public LoginController(SharedService sharedService, IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _sharedService = sharedService;
            _sharedLocalizer = sharedLocalizer;
        }

        // Metodo privato per gestire il cookie e il redirect
        private async Task<ActionResult> LoginAndRedirect(string username, string returnUrl, bool rememberMe)
        {
            // 1. CREAZIONE CLAIMS (Dati dell'utente nel biscotto)
            var claims = new List<Claim>
            {
                // Usiamo lo username sia come ID che come Nome per semplicità
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, username) // Se usi l'email come username
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. SCRITTURA COOKIE
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), 
                new AuthenticationProperties
                {
                    ExpiresUtc = (rememberMe) ? DateTimeOffset.UtcNow.AddMonths(3) : null,
                    IsPersistent = rememberMe,
                });

            // 3. GESTIONE REDIRECT
            // Se c'era un URL specifico (es. stavi andando al Ristorante), ci torniamo
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // ALTRIMENTI: Vai dritto alla Mappa (Modifica richiesta)
            return RedirectToAction("Mappa", "Prenotazione");
        }

        [HttpGet]
        public virtual IActionResult Login(string returnUrl)
        {
            // Se l'utente è già loggato, non mostrare il form, mandalo dentro
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Mappa", "Prenotazione");
            }

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl,
            };

            return View(model);
        }

        [HttpPost]
        public async virtual Task<ActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // --- LOGICA DI AUTENTICAZIONE ---
                    // Qui dovresti chiamare il DB per verificare la password.
                    // Per ora, per far funzionare il tuo prototipo, accettiamo l'utente 
                    // se ha inserito qualcosa nel campo Email/Username.
                    
                    /* * CODICE ORIGINALE (Scommenta quando avrai la tabella utenti vera):
                     * var utente = await _sharedService.Query(new CheckLoginCredentialsQuery { Email = model.Email, Password = model.Password });
                     * await LoginAndRedirect(utente.Email, model.ReturnUrl, model.RememberMe);
                     */

                    // LOGICA "PROTOTIPO": Accetta tutto per farti testare la mappa
                    if (!string.IsNullOrWhiteSpace(model.Email)) 
                    {
                        return await LoginAndRedirect(model.Email, model.ReturnUrl, model.RememberMe);
                    }
                    else 
                    {
                        ModelState.AddModelError(LoginErrorModelStateKey, "Inserisci un nome utente.");
                    }
                }
                catch (Exception e) // Cattura generica per sicurezza
                {
                    ModelState.AddModelError(LoginErrorModelStateKey, "Credenziali non valide: " + e.Message);
                }
            }

            // Se qualcosa è andato storto, rimaniamo qui e mostriamo gli errori
            return View(model);
        }

        [HttpPost]
        public async virtual Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Messaggio di conferma (usa il sistema di Alert del template)
            Alerts.AddSuccess(this, "Utente scollegato correttamente");
            
            // Torna alla pagina di Login pulita
            return RedirectToAction("Login");
        }
    }
}