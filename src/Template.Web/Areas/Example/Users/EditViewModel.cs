using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
// 1. IMPORTIAMO I DTO E L'INFRASTRUTTURA
using Template.Services.Utenti; 
using Template.Web.Infrastructure; // <--- Serve per JsonSerializer

namespace Template.Web.Areas.Example.Users
{
    public class EditViewModel
    {
        [HiddenInput]
        public Guid? Id { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Nome")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Cognome")]
        public string LastName { get; set; }

        [Display(Name = "NickName")]
        public string NickName { get; set; }

        // Metodo per popolare il ViewModel dai dati del DB
        public void SetUser(UserDetailDTO user)
        {
            this.Id = user.Id;
            this.Email = user.Email;
            this.FirstName = user.FirstName;
            this.LastName = user.LastName;
            this.NickName = user.NickName;
        }

        // Metodo opzionale (puoi lasciarlo o toglierlo se non lo usi più)
        public object ToAddOrUpdateUserCommand()
        {
            return this; 
        }

        // 2. ECCO IL METODO MANCANTE CHE CERCAVA LA VIEW
        public string ToJson()
        {
            // Converte questo oggetto in una stringa JSON per il JavaScript
            return JsonSerializer.ToJsonCamelCase(this);
        }
    }
}