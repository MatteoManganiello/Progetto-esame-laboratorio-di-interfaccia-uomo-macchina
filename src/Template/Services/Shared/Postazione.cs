using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template.Services.Shared
{
    [Table("Postazioni")]
    public class Postazione
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CodiceUnivoco { get; set; }
        
        public string Nome { get; set; }
        public string Tipo { get; set; }
        public bool IsAbilitata { get; set; } = true;

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        // --- NUOVA PROPRIETÀ ---
        public int PostiTotali { get; set; } = 1; // Default 1 posto
        // ------------------------

        public virtual ICollection<Prenotazione> Prenotazioni { get; set; }
    }
}