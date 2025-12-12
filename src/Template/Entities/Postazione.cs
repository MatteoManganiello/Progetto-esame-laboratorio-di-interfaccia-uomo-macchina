using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template.Entities // <--- Corretto: siamo nella cartella Entities
{
    [Table("Postazioni")]
    public class Postazione
    {
        // Costruttore: Inizializza la lista per evitare crash (NullReference)
        public Postazione()
        {
            Prenotazioni = new HashSet<Prenotazione>();
        }

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
        public int PostiTotali { get; set; } = 1; 
        // ------------------------

        // Relazione con le prenotazioni
        // Poiché anche la classe 'Prenotazione' è in Template.Entities, non servono 'using' extra.
        public virtual ICollection<Prenotazione> Prenotazioni { get; set; }
    }
}