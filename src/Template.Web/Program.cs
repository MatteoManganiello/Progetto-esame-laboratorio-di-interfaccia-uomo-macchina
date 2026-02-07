// Avvia l'app web, applica le migrazioni al database, 
// inizializza i dati in ambiente di sviluppo e 
// configura l'host con porta opzionale da variabile PORT.
using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Template.Data;
using Template.Infrastructure;

namespace Template.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
                    dbContext.Database.Migrate();

                    if (env.IsDevelopment())
                    {
                        DataGenerator.Initialize(services);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERRORE NELL'INIZIALIZZAZIONE DEI DATI: " + ex.Message);
                }
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var port = Environment.GetEnvironmentVariable("PORT");
                    if (!string.IsNullOrWhiteSpace(port))
                    {
                        webBuilder.UseUrls($"http://0.0.0.0:{port}");
                    }
                    webBuilder.UseStartup<Startup>();
                });
    }
}