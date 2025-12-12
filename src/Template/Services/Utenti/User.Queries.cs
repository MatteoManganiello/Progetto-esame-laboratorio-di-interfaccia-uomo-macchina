using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Template.Infrastructure; // Qui deve esserci Paging e LoginException
using Template.Entities;       // Qui c'è la classe User
using Template.Data;           // Qui c'è il DbContext

namespace Template.Services.Utenti
{
    // ==========================================
    // DTOs (Data Transfer Objects)
    // ==========================================
    public class UsersSelectQuery
    {
        public Guid IdCurrentUser { get; set; }
        public string Filter { get; set; }
    }

    public class UsersSelectDTO
    {
        public IEnumerable<UserDTO> Users { get; set; }
        public int Count { get; set; }

        public class UserDTO
        {
            public Guid Id { get; set; }
            public string Email { get; set; }
        }
    }

    public class UsersIndexQuery
    {
        public Guid IdCurrentUser { get; set; }
        public string Filter { get; set; }
        public Paging Paging { get; set; }
    }

    public class UsersIndexDTO
    {
        public IEnumerable<UserDTO> Users { get; set; }
        public int Count { get; set; }

        public class UserDTO
        {
            public Guid Id { get; set; }
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
    }

    public class UserDetailQuery
    {
        public Guid Id { get; set; }
    }

    public class UserDetailDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NickName { get; set; }
    }

    public class CheckLoginCredentialsQuery
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    // ==========================================
    // CLASSE SERVICE (UserQueries)
    // ==========================================
    public class UserQueries
    {
        private readonly TemplateDbContext _dbContext;

        public UserQueries(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // --- 1. Metodo Semplice (Utile per il LoginController rapido) ---
        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        // --- 2. Query Select (Per menu a tendina) ---
        public async Task<UsersSelectDTO> Query(UsersSelectQuery qry)
        {
            var queryable = _dbContext.Users.AsQueryable();

            // Filtro ID corrente (se diverso da Empty)
            if (qry.IdCurrentUser != Guid.Empty)
            {
                // Nota: Assumiamo che User.Id sia Guid. Se è stringa, togli il Guid.Empty
                // queryable = queryable.Where(x => x.Id != qry.IdCurrentUser);
            }

            if (!string.IsNullOrWhiteSpace(qry.Filter))
            {
                queryable = queryable.Where(x => x.Email.Contains(qry.Filter)); // Rimossa StringComparison per compatibilità EF Core standard
            }

            return new UsersSelectDTO
            {
                Users = await queryable
                    .Select(x => new UsersSelectDTO.UserDTO
                    {
                        Id = x.Id, // Se x.Id è stringa e il DTO vuole Guid, usa Guid.Parse(x.Id)
                        Email = x.Email
                    })
                    .ToArrayAsync(),
                Count = await queryable.CountAsync(),
            };
        }

        // --- 3. Query Index (Per liste paginate) ---
        public async Task<UsersIndexDTO> Query(UsersIndexQuery qry)
        {
            var queryable = _dbContext.Users.AsQueryable();

            if (qry.IdCurrentUser != Guid.Empty)
            {
                 // queryable = queryable.Where(x => x.Id != qry.IdCurrentUser);
            }

            if (!string.IsNullOrWhiteSpace(qry.Filter))
            {
                queryable = queryable.Where(x => x.Email.Contains(qry.Filter));
            }

            // Nota: .ApplyPaging è un Extension Method. 
            // Assicurati che "using Template.Infrastructure" sia presente e corretto.
            
            var list = await queryable
                    // .ApplyPaging(qry.Paging) // Scommenta se hai l'estensione ApplyPaging funzionante
                    .Select(x => new UsersIndexDTO.UserDTO
                    {
                        Id = x.Id, 
                        Email = x.Email,
                        FirstName = x.FirstName,
                        LastName = x.LastName
                    })
                    .ToArrayAsync();

            return new UsersIndexDTO
            {
                Users = list,
                Count = await queryable.CountAsync()
            };
        }

        // --- 4. Query Dettaglio ---
        public async Task<UserDetailDTO> Query(UserDetailQuery qry)
        {
            return await _dbContext.Users
                .Where(x => x.Id == qry.Id)
                .Select(x => new UserDetailDTO
                {
                    Id = x.Id,
                    Email = x.Email,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    NickName = x.NickName
                })
                .FirstOrDefaultAsync();
        }

        // --- 5. Query Login (Con controllo Password) ---
        public async Task<UserDetailDTO> Query(CheckLoginCredentialsQuery qry)
        {
            var user = await _dbContext.Users
                .Where(x => x.Email == qry.Email)
                .FirstOrDefaultAsync();

            // ATTENZIONE: Questo metodo IsMatchWithPassword deve esistere dentro User.cs!
            if (user == null || user.IsMatchWithPassword(qry.Password) == false)
            {
                // Assicurati di avere la classe LoginException in Template.Infrastructure
                throw new Exception("Email o password errate"); 
            }

            return new UserDetailDTO
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                NickName = user.NickName
            };
        }
    }
}