using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Template.Entities; // <--- 1. Importiamo l'entità User
using Template.Data;     // <--- 2. Importiamo il DbContext

namespace Template.Services.Utenti // <--- 3. Namespace allineato alla cartella
{
    public class AddOrUpdateUserCommand
    {
        public Guid? Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NickName { get; set; }
    }

    public class UserCommands // <--- 4. Nome classe dedicato (non più SharedService)
    {
        private readonly TemplateDbContext _dbContext;

        // 5. Costruttore per iniettare il Database
        public UserCommands(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Guid> Handle(AddOrUpdateUserCommand cmd)
        {
            // Cerchiamo l'utente (se cmd.Id è null, user sarà null)
            var user = await _dbContext.Users
                .Where(x => x.Id == cmd.Id)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                // Creazione nuovo utente
                user = new User
                {
                    Email = cmd.Email,
                    // È buona norma inizializzare l'ID se non lo fa il DB, 
                    // ma qui lascio come nel tuo originale.
                };
                _dbContext.Users.Add(user);
            }

            // Aggiornamento dati
            user.FirstName = cmd.FirstName;
            user.LastName = cmd.LastName;
            user.NickName = cmd.NickName;

            await _dbContext.SaveChangesAsync();

            return user.Id;
        }
    }
}