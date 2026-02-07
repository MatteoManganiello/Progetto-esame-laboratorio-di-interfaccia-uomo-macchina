using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Template.Web.Infrastructure;

namespace Template.Web.Areas
{
    [Authorize]
    [Alerts]
    [ModelStateToTempData]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public partial class AuthenticatedBaseController : Controller
    {
        public AuthenticatedBaseController() { }

        protected IdentitaViewModel Identita
        {
            get
            {
                return (IdentitaViewModel)ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY];
            }
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext == null || context.HttpContext.User == null || !context.HttpContext.User.Identity.IsAuthenticated)
            {
                HttpContext.SignOutAsync();
                this.SignOut();
                Alerts.AddError(this, "L'utente non possiede i diritti per visualizzare la risorsa richiesta");
                context.Result = new RedirectToActionResult("Login", "Login", null);
                return;
            }

            var email = context.HttpContext.User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                HttpContext.SignOutAsync();
                this.SignOut();
                Alerts.AddError(this, "Sessione non valida. Effettua di nuovo il login.");
                context.Result = new RedirectToActionResult("Login", "Login", null);
                return;
            }

            ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY] = new IdentitaViewModel
            {
                EmailUtenteCorrente = email
            };

            base.OnActionExecuting(context);
        }
    }
}
