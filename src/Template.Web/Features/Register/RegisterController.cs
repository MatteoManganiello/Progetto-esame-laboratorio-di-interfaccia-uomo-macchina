using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Template.Services.Utenti;
using Template.Web.Infrastructure;

namespace Template.Web.Features.Register
{
    public partial class RegisterController : Controller
    {
        private readonly RegisterService _registerService;

        public RegisterController(RegisterService registerService)
        {
            _registerService = registerService;
        }

        [HttpGet]
        public virtual IActionResult Register() => View();

        [HttpPost]
        public virtual async Task<IActionResult> Register(RegisterRequest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var esito = await _registerService.RegistraUtenteAsync(model);

            if (esito.Successo)
            {
                Alerts.AddSuccess(this, esito.Messaggio);
                return RedirectToAction("Login", "Login");
            }
            else
            {
                ModelState.AddModelError("Register", esito.Messaggio);
                return View(model);
            }
        }
    }
}