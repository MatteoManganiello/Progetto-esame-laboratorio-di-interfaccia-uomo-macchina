using System;
using System.Collections.Generic;

namespace Template.Web.Features.AreaRiservata
{
    public class AreaRiservataViewModel
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Ruolo { get; set; }
        public List<OrdineViewModel> UltimiOrdini { get; set; }
        public List<AcquistoFrequenteViewModel> AcquistiFrequenti { get; set; }
    }

    public class OrdineViewModel
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public string Descrizione { get; set; }
        public decimal Prezzo { get; set; }

        // Data = DataCreazione, quindi la logica è corretta
        public bool Cancellabile => (DateTime.Now - Data).TotalHours < 1;
    }

    public class AcquistoFrequenteViewModel
    {
        public string Descrizione { get; set; }
        public int Quantita { get; set; }
    }
}
