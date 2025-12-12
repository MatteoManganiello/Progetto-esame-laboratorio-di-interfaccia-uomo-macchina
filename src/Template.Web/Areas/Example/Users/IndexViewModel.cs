using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Template.Web.Infrastructure;

// 1. IMPORTANTE: Puntiamo al nuovo namespace dove ci sono i DTO
using Template.Services.Utenti; 

namespace Template.Web.Areas.Example.Users
{
    public class IndexViewModel : PagingViewModel
    {
        public IndexViewModel()
        {
            OrderBy = nameof(UserIndexViewModel.Email);
            OrderByDescending = false;
            Users = Array.Empty<UserIndexViewModel>();
        }

        [Display(Name = "Cerca")]
        public string Filter { get; set; }

        public IEnumerable<UserIndexViewModel> Users { get; set; }

        internal void SetUsers(UsersIndexDTO usersIndexDTO)
        {
            // Qui mappiamo i DTO del servizio sui ViewModel della pagina
            Users = usersIndexDTO.Users.Select(x => new UserIndexViewModel(x)).ToArray();
            TotalItems = usersIndexDTO.Count;
        }

        public UsersIndexQuery ToUsersIndexQuery()
        {
            return new UsersIndexQuery
            {
                Filter = Filter,
                // Assicurati che Template.Infrastructure.Paging esista e abbia queste proprietà
                Paging = new Template.Infrastructure.Paging
                {
                    OrderBy = OrderBy,
                    OrderByDescending = OrderByDescending,
                    Page = Page,
                    PageSize = PageSize
                }
            };
        }

        // Se "MVC.Example..." ti da errore, sostituiscilo con:
        // public override IActionResult GetRoute() => new RedirectToActionResult("Index", "Users", new { area = "Example" });
        // Altrimenti lascialo così:
        public override IActionResult GetRoute() => MVC.Example.Users.Index(this).GetAwaiter().GetResult();

        public string OrderbyUrl<TProperty>(IUrlHelper url, System.Linq.Expressions.Expression<Func<UserIndexViewModel, TProperty>> expression) => base.OrderbyUrl(url, expression);

        public string OrderbyCss<TProperty>(HttpContext context, System.Linq.Expressions.Expression<Func<UserIndexViewModel, TProperty>> expression) => base.OrderbyCss(context, expression);

        public string ToJson()
        {
            return JsonSerializer.ToJsonCamelCase(this);
        }
    }

    public class UserIndexViewModel
    {
        // 2. CORREZIONE QUI:
        // Nel file User.Queries.cs abbiamo chiamato la classe interna "UserDTO", non "User".
        public UserIndexViewModel(UsersIndexDTO.UserDTO userIndexDTO)
        {
            this.Id = userIndexDTO.Id;
            this.Email = userIndexDTO.Email;
            this.FirstName = userIndexDTO.FirstName;
            this.LastName = userIndexDTO.LastName;
        }

        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}