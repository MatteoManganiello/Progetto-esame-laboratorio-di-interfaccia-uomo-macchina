using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using Template.Infrastructure;

namespace Template.Web.Infrastructure
{
    /// <summary>
    /// Attributo per controllare l'accesso basato su ruolo
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public AuthorizeRoleAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Se non autenticato
            if (user == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Controlla il ruolo
            var userRole = user.FindFirst("Ruolo")?.Value ?? RuoliCostanti.USER;

            bool hasRole = false;
            foreach (var role in _allowedRoles)
            {
                if (userRole == role)
                {
                    hasRole = true;
                    break;
                }
            }

            if (!hasRole)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
