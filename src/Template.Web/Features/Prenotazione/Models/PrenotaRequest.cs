using System;
using System.Collections.Generic;

namespace Template.Web.Features.Prenotazione.Models 
{
    public class PrenotaRequest
    {
        public List<int> PostazioniIds { get; set; } = new List<int>(); 
        public DateTime Data { get; set; }
        public int NumeroPersone { get; set; } = 1;
        
        // Aggiungi questo se vuoi gestire le note anche qui
        public string Note { get; set; }
    }
}