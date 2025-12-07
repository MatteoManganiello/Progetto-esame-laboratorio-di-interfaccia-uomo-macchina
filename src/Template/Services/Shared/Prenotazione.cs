using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template.Services.Shared
{
    [Table("Prenotazioni")]
    public class Prenotazione
    {
        [Key]
        public int Id { get; set; }

        public DateTime DataPrenotazione { get; set; } // Giorno prenotato
        public string UserId { get; set; }             // Chi ha prenotato

        // Foreign Key verso Postazione
        public int PostazioneId { get; set; }
        
        [ForeignKey(nameof(PostazioneId))]
        public virtual Postazione Postazione { get; set; }
    }
}