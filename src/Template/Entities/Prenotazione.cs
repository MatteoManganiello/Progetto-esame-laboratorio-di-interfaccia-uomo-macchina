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
        public DateTime DataPrenotazione { get; set; } // Giorno prenotato

        // --- CAMPI MANCANTI AGGIUNTI ---
        
        // Data in cui l'utente ha cliccato "Prenota"
        public DateTime DataCreazione { get; set; } = DateTime.Now; 

        [Required]
        [MaxLength(450)] // Lunghezza standard per ID Utente
        public string UserId { get; set; } 

        // Numero effettivo di persone (necessario per il controllo capienza)
        public int NumeroPersone { get; set; } = 1;

        // Per cancellare senza perdere i dati (Soft Delete)
        public bool IsCancellata { get; set; } = false;

        // Note opzionali inserite dall'utente
        [MaxLength(500)]
        public string Note { get; set; }

        // --- RELAZIONI ---

        [Required]
        public int PostazioneId { get; set; }
        
        [ForeignKey(nameof(PostazioneId))]
        public virtual Postazione Postazione { get; set; }
    }
}