using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template.Entities
{
    [Table("User")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string Email { get; set; }
        
        public string Password { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NickName { get; set; }
        public string Ruolo { get; set; } = "User";

        public bool IsMatchWithPassword(string passwordDaControllare)
        {
            if (string.IsNullOrWhiteSpace(passwordDaControllare)) return false;

            return this.Password == passwordDaControllare;
        }
        
    }
}