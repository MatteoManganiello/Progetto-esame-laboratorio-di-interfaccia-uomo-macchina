using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Template.Data;
using Template.Entities;
using Template.Web.Infrastructure;

namespace Template.Web.Features.Register
{
    public partial class RegisterController : Controller
    {
        private readonly TemplateDbContext _dbContext;

        public RegisterController(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // *** AGGIUNTO "virtual" QUI SOTTO ***
        [HttpGet]
        public virtual IActionResult Register() 
        {
            return View();
        }

        // *** AGGIUNTO "virtual" ANCHE QUI SOTTO ***
        [HttpPost]
        public virtual async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            bool emailEsiste = await _dbContext.Users.AnyAsync(u => u.Email == model.Email);
            if (emailEsiste)
            {
                ModelState.AddModelError("Email", "Questa email è già registrata.");
                return View(model);
            }

            var nuovoUtente = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                NickName = model.NickName,
                Email = model.Email,
                Password = model.Password 
            };

            _dbContext.Users.Add(nuovoUtente);
            await _dbContext.SaveChangesAsync();

            Alerts.AddSuccess(this, "Registrazione completata! Effettua il login.");
            return RedirectToAction("Login", "Login");
        }
    }
}