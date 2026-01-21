using System;

namespace Template.Entities
{
    public class MenuSettimanale
    {
        public int Id { get; set; }
        public DateTime WeekStart { get; set; }

        public string Lunedi { get; set; }
        public string Martedi { get; set; }
        public string Mercoledi { get; set; }
        public string Giovedi { get; set; }
        public string Venerdi { get; set; }
        public string Sabato { get; set; }
        public string Domenica { get; set; }
    }
}
