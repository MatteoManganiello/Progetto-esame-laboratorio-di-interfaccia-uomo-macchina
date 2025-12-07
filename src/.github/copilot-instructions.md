<!-- Copilot/agent instructions tailored to this repository -->
<!-- Istruzioni per Copilot/agent adattate a questo repository -->
# Guida rapida (italiano) per agenti AI che lavorano su questo repo

Scopo: fornire a un agente AI le informazioni essenziali per essere produttivo subito: architettura, convenzioni, comandi di build/run e file chiave.

- **Build principale**: la soluzione è `Template.sln` nella cartella `src/`. Esegui `dotnet build Template.sln` da `src/`.
- **Avvio dell'app web**: dalla radice `src/` esegui `dotnet run --project Template.Web` oppure `cd Template.Web && dotnet run`.

Architettura (panoramica)
- Applicazione ASP.NET Core MVC in `Template.Web` (Razor views + controller). Entry point: `Template.Web/Program.cs` e `Template.Web/Startup.cs`.
- Logica di business e dati in `Template/Services` e `Template/Infrastructure` (DbContext EF, seed dei dati, utility).
- Registrazione delle dipendenze centralizzata in `Template.Web/Container.cs` — aggiungi i servizi custom qui con `services.AddScoped`, `AddTransient` o `AddSingleton`.
- SignalR e pubblicazione eventi si trovano in `Template.Web/SignalR` (hub: `TemplateHub` esposto su `/templateHub`). L'interfaccia `IPublishDomainEvents` è collegata a `SignalrPublishDomainEvents`.

Convenzioni e pattern rilevanti
- Database:
  - `Template.Services.TemplateDbContext` usa un database in-memory configurato in `Startup.ConfigureServices` con `UseInMemoryDatabase("Template")`.
  - Il seeding dei dati avviene in `Template.Infrastructure.DataGenerator.InitializeUsers`, chiamato dal costruttore di `TemplateDbContext`. Alcuni utenti hanno GUID fissi usati nei test.
- Servizi:
  - I servizi possono essere suddivisi in partial class sotto `Template/Services` (esempio: `Template/Services/Shared/_SharedService.cs`).
  - Registra i servizi applicativi in `Container.RegisterTypes(IServiceCollection)` in modo che il web project li risolva via DI.
- View e routing:
  - Le posizioni delle view sono personalizzate in `Startup.ConfigureServices`: il layout preferito è `Features/{Feature}/{View}.cshtml` e sono supportate le `Areas`.
  - La route di default punta al login (`{controller=Login}/{action=Login}`) e l'autenticazione cookie è configurata in `Startup`.
- Paginazione e query: usa `Template.Infrastructure.Paging` con le extension `ApplyPaging` e `ApplyOrder`. L'ordinamento usa stringhe compatibili con `System.Linq.Dynamic.Core`.

Integrazioni e note di runtime
- I file statici includono anche `node_modules` e `Areas` tramite `CustomCompositeFileProvider` (`Template.Web/Infrastructure/CustomCompositeFileProvider.cs`).
- Localizzazione: vengono usati resource file `.resx` (es. `SharedResource.*.resx`) e la localizzazione delle view è abilitata. La cultura supportata è `it-it` (vedi `SupportedCultures` in `Startup.cs`).
- SignalR: l'hub `Template.Web.SignalR.Hubs.TemplateHub` richiede utenti autenticati (`[Authorize]`). Il client implementa `ITemplateClientEvent`.

Flussi di lavoro e comandi utili
- Build soluzione: `cd src && dotnet build Template.sln`.
- Eseguire il progetto web: `dotnet run --project src/Template.Web` oppure `cd src/Template.Web && dotnet run`.
- Per attivare la compilazione runtime delle view e caricare `appsettings.Development.json` impostare `ASPNETCORE_ENVIRONMENT=Development` (la compilazione runtime è abilitata nel blocco `#if DEBUG` in `Startup`).
- Per il debug, controlla `Template.Web/Properties/launchSettings.json` per i profili usati dagli IDE.

File da ispezionare quando si valuta l'impatto di una modifica
- `Template.Web/Startup.cs` — pipeline HTTP, autenticazione, mappatura SignalR, static files, localizzazione, view locations.
- `Template.Web/Container.cs` — punto unico per registrare i servizi DI.
- `Template/Services/_TemplateDbContext.cs` e `Template/Infrastructure/DataGenerator.cs` — aggiungere nuovi `DbSet` o aggiornare il seed dei dati.
- `Template/Infrastructure/Paging.cs` — comportamento standard per paginazione e ordinamento; riutilizzare quando si aggiungono endpoint di lista.
- `Template.Web/SignalR/Hubs/TemplateHub.cs` — metodi hub, gestione gruppi e publishing eventi tramite `IPublishDomainEvents`.

Consigli rapidi per PR e modifiche
- Non modificare file in `bin/` e `obj/`; sono generati.
- Quando aggiungi un `DbSet` EF, dichiara la proprietà in `TemplateDbContext` e aggiorna `DataGenerator` se serve seed di esempio.
- Registra i nuovi servizi applicativi in `Container.RegisterTypes` e preferisci `AddScoped` per servizi che dipendono dal DbContext.
- Quando aggiungi nuove feature/UI, rispetta la struttura `Features/{Feature}/...` per le view invece di sparpagliare file sotto `Views`.

Vuoi che estenda questa guida con esempi concreti? Posso aggiungere una piccola checklist per le PR, uno snippet che mostra come aggiungere un controller+view+service, o esempi di test unitari mirati.
