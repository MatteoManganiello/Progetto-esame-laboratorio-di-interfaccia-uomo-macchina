using System.ComponentModel.DataAnnotations;

namespace Template.Web.Features.Register
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Il nome è obbligatorio")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Il cognome è obbligatorio")]
        public string LastName { get; set; }

        public string NickName { get; set; } 

        [Required(ErrorMessage = "L'email è obbligatoria")]
        [EmailAddress(ErrorMessage = "Inserisci un'email valida")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La password è obbligatoria")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Le password non coincidono")]
        public string ConfirmPassword { get; set; }
    }
}