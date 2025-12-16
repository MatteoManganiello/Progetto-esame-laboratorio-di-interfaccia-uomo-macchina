using System;
using System.Collections.Generic;

namespace Template.Services.Prenotazioni
{
    public class PrenotaRequest
    {
        public DateTime Data { get; set; }

        public string Note { get; set; }
        public List<CarrelloItemRequest> Elementi { get; set; } = new List<CarrelloItemRequest>();
    }
    public class CarrelloItemRequest
    {
        public int PostazioneId { get; set; }
        public int NumeroPersone { get; set; }
    }
}