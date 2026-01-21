using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Template.Web.Infrastructure;
using Template.Web.SignalR.Hubs;

// --- NAMESPACE CORRETTI ---
using Template.Data;                   // Per TemplateDbContext
using Template.Services.Utenti;        // Per UserQueries, RegisterService
using Template.Services.Prenotazioni;  // Per PrenotazioneService
using Template.Services.Ristorazione;  // Per RistorazioneService

namespace Template.Web
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; set; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Env = env;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            // 1. DATABASE - MySQL
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var envConnectionString = System.Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(envConnectionString))
            {
                connectionString = envConnectionString;
            }
            services.AddDbContext<TemplateDbContext>(options =>
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlOptions =>
                {
                    mySqlOptions.CommandTimeout(30);
                    mySqlOptions.EnableRetryOnFailure(3);
                });
            });

            // 2. REGISTRAZIONE SERVIZI (Dependency Injection)
            // ==============================================================
            
            // A. UserQueries (Per Login e Gestione Utenti)
            services.AddScoped<UserQueries>();

            // B. PrenotazioneService (Per Logica Prenotazioni)
            services.AddScoped<PrenotazioneService>();

            // C. RistorazioneService (Per Logica Ristorazione)
            services.AddScoped<Template.Services.Ristorazione.RistorazioneService>();

            // D. RegisterService (Per Registrazione Utenti)
            services.AddScoped<Template.Services.Utenti.RegisterService>();

            // E. UserManagementService (Per Gestione Utenti CRUD)
            services.AddScoped<Template.Services.Utenti.UserManagementService>();
            
            // ==============================================================

            // SERVIZI DI AUTENTICAZIONE
            services.AddSession();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Login/Login";
                    options.LogoutPath = "/Login/Logout";
                    options.AccessDeniedPath = "/Home/Error";
                });

            var builder = services.AddMvc()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization(options =>
                {
                    options.DataAnnotationLocalizerProvider = (type, factory) =>
                        factory.Create(typeof(SharedResource));
                });

#if DEBUG
            builder.AddRazorRuntimeCompilation();
#endif

            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.AreaViewLocationFormats.Clear();
                options.AreaViewLocationFormats.Add("/Areas/{2}/{1}/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Views/Shared/{0}.cshtml");

                options.ViewLocationFormats.Clear();
                options.ViewLocationFormats.Add("/Features/{1}/{0}.cshtml");
                options.ViewLocationFormats.Add("/Features/Views/{1}/{0}.cshtml");
                options.ViewLocationFormats.Add("/Features/Views/Shared/{0}.cshtml");
                options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
            });

            services.AddSignalR();

            services.AddHealthChecks()
                .AddDbContextCheck<TemplateDbContext>();
            
            // Registra eventuali altri tipi definiti in Container.cs
            Container.RegisterTypes(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!env.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseRequestLocalization(SupportedCultures.CultureNames);
            
            // --- ORDINE FONDAMENTALE DEI MIDDLEWARE ---
            app.UseRouting();
            app.UseSession();
            
            app.UseAuthentication(); // 1. Chi sei?
            app.UseAuthorization();  // 2. Puoi entrare?
            // ------------------------------------------

            var fileProviders = new List<IFileProvider> { env.WebRootFileProvider };

            var nodeModulesPath = Path.Combine(Directory.GetCurrentDirectory(), "node_modules");
            if (Directory.Exists(nodeModulesPath))
            {
                fileProviders.Add(new CompositePhysicalFileProvider(Directory.GetCurrentDirectory(), "node_modules"));
            }

            var areasPath = Path.Combine(Directory.GetCurrentDirectory(), "Areas");
            if (Directory.Exists(areasPath))
            {
                fileProviders.Add(new CompositePhysicalFileProvider(Directory.GetCurrentDirectory(), "Areas"));
            }

            env.WebRootFileProvider = new CustomCompositeFileProvider(fileProviders);
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthz");
                endpoints.MapHealthChecks("/healthz/db", new HealthCheckOptions
                {
                    Predicate = registration => registration.Name.Contains("DbContext")
                });
                endpoints.MapHub<TemplateHub>("/templateHub");
                
                endpoints.MapAreaControllerRoute("Example", "Example", "Example/{controller=Users}/{action=Index}/{id?}");

                // Default route che punta alla Mappa
                endpoints.MapControllerRoute(
                    name: "default", 
                    pattern: "{controller=Prenotazione}/{action=Mappa}/{id?}");
            });
        }
    }

    public static class SupportedCultures
    {
        public readonly static string[] CultureNames;
        public readonly static CultureInfo[] Cultures;

        static SupportedCultures()
        {
            CultureNames = new[] { "it-it" };
            Cultures = CultureNames.Select(c => new CultureInfo(c)).ToArray();
        }
    }
}