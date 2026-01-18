using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Template.Web.Infrastructure;
using Template.Web.SignalR;
using Template.Web.SignalR.Hubs.Events;
using Template.Services.Utenti;

namespace Template.Web.Areas.Example.Users
{
    [Area("Example")]
    public partial class UsersController : AuthenticatedBaseController
    {
        // 2. RIMPIAZZIAMO SharedService CON I NUOVI SERVIZI
        private readonly UserQueries _userQueries;
        private readonly UserManagementService _userManagementService;
        private readonly IPublishDomainEvents _publisher;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

        // Costruttore Aggiornato
        public UsersController(
            UserQueries userQueries,
            UserManagementService userManagementService,
            IPublishDomainEvents publisher, 
            IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _userQueries = userQueries;
            _userManagementService = userManagementService;
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
                    EsitoUtenteOperazione esito;

                    if (model.Id.HasValue)
                    {
                        // MODIFICA
                        var request = new UserEditRequest
                        {
                            Email = model.Email,
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            NickName = model.NickName
                        };
                        esito = await _userManagementService.AggiornaUtenteAsync(model.Id.Value, request);
                    }
                    else
                    {
                        // CREAZIONE NUOVO
                        var request = new UserEditRequest
                        {
                            Email = model.Email,
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            NickName = model.NickName
                        };
                        esito = await _userManagementService.CreaUtenteAsync(request);
                    }

                    if (esito.Successo)
                    {
                        Alerts.AddSuccess(this, esito.Messaggio);

                        // Evento SignalR
                        await _publisher.Publish(new NewMessageEvent
                        {
                            IdGroup = esito.UtenteId.Value,
                            IdUser = esito.UtenteId.Value,
                            IdMessage = Guid.NewGuid()
                        });

                        model.Id = esito.UtenteId;
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, esito.Messaggio);
                    }
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
            var esito = await _userManagementService.EliminaUtenteAsync(id);
            
            if (esito.Successo)
            {
                Alerts.AddSuccess(this, esito.Messaggio);
            }
            else
            {
                Alerts.AddError(this, esito.Messaggio);
            }

            return RedirectToAction(Actions.Index());
        }
    }
}