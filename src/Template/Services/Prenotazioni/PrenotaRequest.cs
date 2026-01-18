using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Template.Services.Prenotazioni
{
    public class PrenotaRequest
    {
        [Required(ErrorMessage = "La data è obbligatoria")]
        public DateTime Data { get; set; }

        [StringLength(500, ErrorMessage = "Le note non possono superare 500 caratteri")]
        public string Note { get; set; }

        [Required(ErrorMessage = "Seleziona almeno una postazione")]
        [MinLength(1, ErrorMessage = "Il carrello deve contenere almeno un elemento")]
        public List<CarrelloItemRequest> Elementi { get; set; } = new List<CarrelloItemRequest>();
    }

    public class CarrelloItemRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "PostazioneId non valido")]
        public int PostazioneId { get; set; }

        [Range(1, 100, ErrorMessage = "Numero persone deve essere tra 1 e 100")]
        public int NumeroPersone { get; set; }
    }
}