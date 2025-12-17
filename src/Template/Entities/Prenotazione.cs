using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template.Entities
{
    [Table("Prenotazioni")]
    public class Prenotazione
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime DataPrenotazione { get; set; } 

        public DateTime DataCreazione { get; set; } = DateTime.Now; 

        [Required]
        [MaxLength(450)] 
        public string UserId { get; set; } 

        public int NumeroPersone { get; set; } = 1;

        public bool IsCancellata { get; set; } = false;


        [MaxLength(500)]
        public string Note { get; set; }


        [Required]
        public int PostazioneId { get; set; }
        
        [ForeignKey(nameof(PostazioneId))]
        public virtual Postazione Postazione { get; set; }
    }
}