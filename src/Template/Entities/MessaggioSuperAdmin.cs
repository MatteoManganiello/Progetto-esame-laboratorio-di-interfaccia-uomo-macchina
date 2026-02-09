using System;

namespace Template.Entities
{
    public class MessaggioSuperAdmin
    {
        public int Id { get; set; }
        public string Titolo { get; set; }
        public string Contenuto { get; set; }
        public string Data { get; set; }
        public DateTime DataCreazione { get; set; } = DateTime.UtcNow;
    }
}