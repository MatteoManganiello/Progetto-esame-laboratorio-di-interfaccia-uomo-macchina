using System;
using System.Collections.Generic;

namespace Template.Services.Prenotazioni // <--- Unico cambiamento: allineato alla cartella
{
    public class PrenotaRequest
    {
        // Lista principale usata dal Controller
        public List<int> PostazioniIds { get; set; } = new List<int>(); 

        // --- TRUCCO FONDAMENTALE ---
        // Se il frontend manda "postazioneId" (singolare), lo mettiamo nella lista.
        public int PostazioneId 
        { 
            set 
            { 
                if (value > 0) PostazioniIds = new List<int> { value }; 
            } 
        }

        public DateTime Data { get; set; }
        public int NumeroPersone { get; set; } = 1;
        public string Note { get; set; }
    }
}