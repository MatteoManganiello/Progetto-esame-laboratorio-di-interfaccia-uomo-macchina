using System;

namespace Template.Entities
{
    public class MessaggioSuperAdmin
    {
        public int Id { get; set; }
        public string Titolo { get; set; }
        public string Contenuto { get; set; }
        public string Data { get; set; } // Puoi cambiarlo in DateTime se preferisci
        public DateTime DataCreazione { get; set; } = DateTime.UtcNow;
        // Eventuali altri campi specifici (es. destinatario, mittente, ecc.)
    }
}