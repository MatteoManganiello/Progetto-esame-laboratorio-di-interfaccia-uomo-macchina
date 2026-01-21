using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                    if (env.IsDevelopment())
                    {
                        // Usa DataGenerator per inizializzare i dati solo in dev
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