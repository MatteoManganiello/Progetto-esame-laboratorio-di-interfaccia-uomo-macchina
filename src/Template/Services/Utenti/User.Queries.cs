using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Template.Infrastructure; 
using Template.Entities;      
using Template.Data;          

namespace Template.Services.Utenti
{
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
        public string Ruolo { get; set; }
    }

    public class CheckLoginCredentialsQuery
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class UserQueries
    {
        private readonly TemplateDbContext _dbContext;

        public UserQueries(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<UsersSelectDTO> Query(UsersSelectQuery qry)
        {
            var queryable = _dbContext.Users.AsQueryable();


            if (qry.IdCurrentUser != Guid.Empty)
            {

            }

            if (!string.IsNullOrWhiteSpace(qry.Filter))
            {
                queryable = queryable.Where(x => x.Email.Contains(qry.Filter)); 
            }

            return new UsersSelectDTO
            {
                Users = await queryable
                    .Select(x => new UsersSelectDTO.UserDTO
                    {
                        Id = x.Id, 
                        Email = x.Email
                    })
                    .ToArrayAsync(),
                Count = await queryable.CountAsync(),
            };
        }

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

            
            var list = await queryable

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
                    NickName = x.NickName,
                    Ruolo = x.Ruolo
                })
                .FirstOrDefaultAsync();
        }

        public async Task<UserDetailDTO> Query(CheckLoginCredentialsQuery qry)
        {
            var user = await _dbContext.Users
                .Where(x => x.Email == qry.Email)
                .FirstOrDefaultAsync();

            if (user == null || user.IsMatchWithPassword(qry.Password) == false)
            {
                throw new Exception("Email o password errate"); 
            }

            return new UserDetailDTO
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                NickName = user.NickName,
                Ruolo = user.Ruolo
            };
        }
    }
}