using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Template.Data;
using Template.Entities;

namespace Template.Services.Utenti
{
    public class EsitoRegistrazione
    {
        public bool Successo { get; set; }
        public string Messaggio { get; set; }
    }

    public class RegisterService
    {
        private readonly TemplateDbContext _dbContext;
        private readonly ILogger<RegisterService> _logger;

        public RegisterService(TemplateDbContext dbContext, ILogger<RegisterService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<EsitoRegistrazione> RegistraUtenteAsync(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new EsitoRegistrazione
                {
                    Successo = false,
                    Messaggio = "Email e Password sono obbligatorie."
                };
            }

            try
            {
                bool emailEsiste = await _dbContext.Users.AnyAsync(u => u.Email == request.Email);
                if (emailEsiste)
                {
                    return new EsitoRegistrazione
                    {
                        Successo = false,
                        Messaggio = "Questa email è già registrata."
                    };
                }

                var nuovoUtente = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    NickName = request.NickName,
                    Email = request.Email,
                    Password = request.Password
                };

                _dbContext.Users.Add(nuovoUtente);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Nuovo utente registrato: {Email}", request.Email);

                return new EsitoRegistrazione
                {
                    Successo = true,
                    Messaggio = "Registrazione completata! Effettua il login."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la registrazione dell'utente {Email}", request.Email);
                return new EsitoRegistrazione
                {
                    Successo = false,
                    Messaggio = "Errore durante la registrazione: " + ex.Message
                };
            }
        }
    }

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email è obbligatoria")]
        [EmailAddress(ErrorMessage = "Email non valida")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password è obbligatoria")]
        [MinLength(3, ErrorMessage = "Password deve avere almeno 3 caratteri")]
        public string Password { get; set; }

        [StringLength(100)]
        public string FirstName { get; set; }

        [StringLength(100)]
        public string LastName { get; set; }

        [StringLength(100)]
        public string NickName { get; set; }
    }
}
