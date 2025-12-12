using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Template.Web.Infrastructure;
using Template.Web.SignalR;
using Template.Web.SignalR.Hubs.Events;

// 1. IMPORTIAMO I NUOVI NAMESPACE
using Template.Services.Utenti; // Qui ci sono UserQueries e i DTO
using Template.Data;           // Per il DbContext
using Template.Entities;       // Per l'entità User

namespace Template.Web.Areas.Example.Users
{
    [Area("Example")]
    public partial class UsersController : AuthenticatedBaseController
    {
        // 2. RIMPIAZZIAMO SharedService CON I NUOVI SERVIZI
        private readonly UserQueries _userQueries;
        private readonly TemplateDbContext _dbContext;
        
        private readonly IPublishDomainEvents _publisher;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

        // Costruttore Aggiornato
        public UsersController(
            UserQueries userQueries, 
            TemplateDbContext dbContext,
            IPublishDomainEvents publisher, 
            IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _userQueries = userQueries;
            _dbContext = dbContext;
            _publisher = publisher;
            _sharedLocalizer = sharedLocalizer;

            ModelUnbinderHelpers.ModelUnbinders.Add(typeof(IndexViewModel), new SimplePropertyModelUnbinder());
        }

        [HttpGet]
        public virtual async Task<IActionResult> Index(IndexViewModel model)
        {
            // Lettura tramite UserQueries
            // Nota: Assicurati che model.ToUsersIndexQuery() esista nel ViewModel, 
            // altrimenti crea l'oggetto UsersIndexQuery a mano qui.
            var users = await _userQueries.Query(model.ToUsersIndexQuery());
            model.SetUsers(users);

            return View(model);
        }

        [HttpGet]
        public virtual IActionResult New()
        {
            return RedirectToAction(Actions.Edit());
        }

        [HttpGet]
        public virtual async Task<IActionResult> Edit(Guid? id)
        {
            var model = new EditViewModel();

            if (id.HasValue)
            {
                // Lettura dettaglio tramite UserQueries
                var userDetail = await _userQueries.Query(new UserDetailQuery
                {
                    Id = id.Value,
                });
                
                model.SetUser(userDetail);
            }

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Edit(EditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Guid userId;

                    // 3. LOGICA DI SALVATAGGIO DIRETTA (Senza SharedService)
                    if (model.Id.HasValue)
                    {
                        // MODIFICA
                        var user = await _dbContext.Users.FindAsync(model.Id.Value);
                        if (user != null)
                        {
                            user.Email = model.Email;
                            user.FirstName = model.FirstName;
                            user.LastName = model.LastName;
                            user.NickName = model.NickName;
                            // user.Password = ... (gestisci la password se serve)
                            
                            userId = user.Id;
                        }
                        else
                        {
                            throw new Exception("Utente non trovato");
                        }
                    }
                    else
                    {
                        // CREAZIONE NUOVO
                        var newUser = new User
                        {
                            Email = model.Email,
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            NickName = model.NickName,
                            // Password provvisoria o gestita dal modello
                            Password = "ChangeMe123!" 
                        };
                        
                        _dbContext.Users.Add(newUser);
                        await _dbContext.SaveChangesAsync(); // Salviamo subito per avere l'ID
                        userId = newUser.Id;
                    }

                    // Conferma salvataggio modifiche
                    await _dbContext.SaveChangesAsync();

                    Alerts.AddSuccess(this, "Informazioni aggiornate");

                    // Evento SignalR
                    await _publisher.Publish(new NewMessageEvent
                    {
                        IdGroup = userId,
                        IdUser = userId,
                        IdMessage = Guid.NewGuid()
                    });

                    // Aggiorniamo l'ID nel modello per il redirect corretto
                    model.Id = userId;
                }
                catch (Exception e)
                {
                    ModelState.AddModelError(string.Empty, e.Message);
                }
            }

            if (ModelState.IsValid == false)
            {
                Alerts.AddError(this, "Errore in aggiornamento");
            }

            return RedirectToAction(Actions.Edit(model.Id));
        }

        [HttpPost]
        public virtual async Task<IActionResult> Delete(Guid id)
        {
            // 4. LOGICA DI CANCELLAZIONE DIRETTA
            var user = await _dbContext.Users.FindAsync(id);
            if (user != null)
            {
                _dbContext.Users.Remove(user);
                await _dbContext.SaveChangesAsync();
                Alerts.AddSuccess(this, "Utente cancellato");
            }
            else
            {
                Alerts.AddError(this, "Utente non trovato");
            }

            return RedirectToAction(Actions.Index());
        }
    }
}