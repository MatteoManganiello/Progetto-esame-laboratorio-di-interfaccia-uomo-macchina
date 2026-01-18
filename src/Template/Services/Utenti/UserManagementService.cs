using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Template.Data;
using Template.Entities;

namespace Template.Services.Utenti
{
    public class EsitoUtenteOperazione
    {
        public bool Successo { get; set; }
        public string Messaggio { get; set; }
        public Guid? UtenteId { get; set; }
    }

    public class UserManagementService
    {
        private readonly TemplateDbContext _dbContext;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(TemplateDbContext dbContext, ILogger<UserManagementService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<EsitoUtenteOperazione> AggiornaUtenteAsync(Guid userId, UserEditRequest request)
        {
            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    return new EsitoUtenteOperazione
                    {
                        Successo = false,
                        Messaggio = "Utente non trovato"
                    };
                }

                user.Email = request.Email;
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.NickName = request.NickName;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Utente {UserId} aggiornato", userId);

                return new EsitoUtenteOperazione
                {
                    Successo = true,
                    Messaggio = "Informazioni aggiornate",
                    UtenteId = userId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento utente {UserId}", userId);
                return new EsitoUtenteOperazione
                {
                    Successo = false,
                    Messaggio = "Errore: " + ex.Message
                };
            }
        }

        public async Task<EsitoUtenteOperazione> CreaUtenteAsync(UserEditRequest request)
        {
            try
            {
                var newUser = new User
                {
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    NickName = request.NickName,
                    Password = "ChangeMe123!"
                };

                _dbContext.Users.Add(newUser);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Nuovo utente creato: {Email}", request.Email);

                return new EsitoUtenteOperazione
                {
                    Successo = true,
                    Messaggio = "Utente creato con successo",
                    UtenteId = newUser.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella creazione utente {Email}", request.Email);
                return new EsitoUtenteOperazione
                {
                    Successo = false,
                    Messaggio = "Errore: " + ex.Message
                };
            }
        }

        public async Task<EsitoUtenteOperazione> EliminaUtenteAsync(Guid userId)
        {
            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    return new EsitoUtenteOperazione
                    {
                        Successo = false,
                        Messaggio = "Utente non trovato"
                    };
                }

                _dbContext.Users.Remove(user);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Utente {UserId} eliminato", userId);

                return new EsitoUtenteOperazione
                {
                    Successo = true,
                    Messaggio = "Utente cancellato"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'eliminazione utente {UserId}", userId);
                return new EsitoUtenteOperazione
                {
                    Successo = false,
                    Messaggio = "Errore: " + ex.Message
                };
            }
        }
    }

    public class UserEditRequest
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NickName { get; set; }
    }
}
