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
        
        // Per l'esame: Contiene la password in CHIARO (es. "123")
        public string Password { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NickName { get; set; }

        /// <summary>
        /// Verifica password semplificata (confronto diretto stringa su stringa)
        /// </summary>
        public bool IsMatchWithPassword(string passwordDaControllare)
        {
            if (string.IsNullOrWhiteSpace(passwordDaControllare)) return false;

            // CONFRONTO DIRETTO:
            // Se nel DB c'è "123" e l'utente scrive "123", restituisce TRUE.
            return this.Password == passwordDaControllare;
        }
        
        // Ho rimosso SetPassword o metodi di hash complessi per evitare confusione.
    }
}