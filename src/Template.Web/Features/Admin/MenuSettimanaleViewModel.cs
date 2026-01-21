using System;

namespace Template.Web.Features.Admin
{
    public class MenuSettimanaleViewModel
    {
        public DateTime WeekStart { get; set; }
        public string WeekIso { get; set; }
        public string Lunedi { get; set; }
        public string Martedi { get; set; }
        public string Mercoledi { get; set; }
        public string Giovedi { get; set; }
        public string Venerdi { get; set; }
        public string Sabato { get; set; }
        public string Domenica { get; set; }
    }
}
