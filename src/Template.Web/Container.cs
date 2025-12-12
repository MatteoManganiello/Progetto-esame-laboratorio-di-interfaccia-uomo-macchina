using Microsoft.Extensions.DependencyInjection;
using Template.Web.SignalR;

// 1. IMPORTIAMO I NUOVI NAMESPACE
using Template.Services.Utenti;
using Template.Services.Prenotazioni;

namespace Template.Web
{
    public class Container
    {
        public static void RegisterTypes(IServiceCollection container)
        {
            // 2. REGISTRIAMO I NUOVI SERVIZI
            // Questi comandi dicono all'app: "Quando un controller chiede UserQueries, dagliene uno nuovo."
            container.AddScoped<UserQueries>();
            container.AddScoped<PrenotazioneService>();

            // (Abbiamo rimosso container.AddScoped<SharedService>(); perché non esiste più)

            // Registration of SignalR events (Lasciamo invariato)
            container.AddScoped<IPublishDomainEvents, SignalrPublishDomainEvents>();
        }
    }
}