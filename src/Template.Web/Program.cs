using System;
using System.Collections.Generic; // Serve per List<>
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Template.Data;
using Template.Entities;

namespace Template.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // ==========================================
            // ZONA DI CREAZIONE DATI (SEEDING)
            // ==========================================
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<TemplateDbContext>();
                    
                    // 1. Assicurati che il DB esista
                    context.Database.EnsureCreated();

                    // 2. CREAZIONE UTENTE ADMIN
                    if (!System.Linq.Enumerable.Any(context.Users))
                    {
                        Console.WriteLine("--> CREAZIONE UTENTE ADMIN...");
                        context.Users.Add(new User
                        {
                            Email = "admin@test.com",
                            FirstName = "Admin",
                            LastName = "Test",
                            NickName = "Boss",
                            Password = "123"
                        });
                    }

                    // 3. CREAZIONE POSTAZIONI (CON LE TUE COORDINATE PRECISE)
                    if (!System.Linq.Enumerable.Any(context.Postazioni))
                    {
                        Console.WriteLine("--> CREAZIONE MAPPA CON LAYOUT PERSONALIZZATO...");
                        
                        var lista = new List<Postazione>();
                        int idCounter = 1;

                        // A. Sala Eventi
                        lista.Add(new Postazione { CodiceUnivoco = "event-main", Nome = "Main Hall", Tipo = "Eventi", X = 35, Y = 30, Width = 350, Height = 250, PostiTotali = 50 });

                        // B. Open Space (Griglia 5x3)
                        // Richiamiamo il metodo helper (definito sotto)
                        AggiungiGruppo(lista, ref idCounter, "Desk", "Singola", startX: 570, startY: 30, rows: 5, cols: 3, width: 30, height: 30, gap: 20);

                        // C. Team Rooms
                        lista.Add(new Postazione { CodiceUnivoco = "dev-1", Nome = "Team Alpha", Tipo = "Team", X = 250, Y = 470, Width = 120, Height = 80, PostiTotali = 6 });
                        lista.Add(new Postazione { CodiceUnivoco = "dev-2", Nome = "Team Beta", Tipo = "Team", X = 450, Y = 470, Width = 120, Height = 80, PostiTotali = 6 });

                        // D. Sale Riunioni
                        lista.Add(new Postazione { CodiceUnivoco = "meet-1", Nome = "Sala Red", Tipo = "Riunioni", X = 40, Y = 485, Width = 125, Height = 50, PostiTotali = 8 });
                        lista.Add(new Postazione { CodiceUnivoco = "meet-2", Nome = "Sala Blue", Tipo = "Riunioni", X = 640, Y = 485, Width = 125, Height = 50, PostiTotali = 8 });

                        // E. Ristorante (Ciclo for)
                        for (int i = 0; i < 3; i++)
                        {
                            lista.Add(new Postazione 
                            { 
                                CodiceUnivoco = $"rist-{i+1}", 
                                Nome = $"Tavolo {i+1}", 
                                Tipo = "Ristorante", 
                                X = 820,              
                                Y = 45 + (i * 115), 
                                Width = 100,
                                Height = 50,
                                PostiTotali = 4
                            });
                        }

                        // Salvataggio nel DB
                        context.Postazioni.AddRange(lista);
                    }

                    // 4. COMMIT FINALE
                    context.SaveChanges();
                    Console.WriteLine("--> DATI INSERITI CORRETTAMENTE! <--");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERRORE NEL SEEDER: " + ex.Message);
                }
            }

            host.Run();
        }

        // --- METODO HELPER (Copiato dal tuo file e reso statico) ---
        private static void AggiungiGruppo(List<Postazione> lista, ref int counter, string prefix, string tipo, int startX, int startY, int rows, int cols, int width, int height, int gap)
        {
            for (int r = 0; r < rows; r++) 
            { 
                for (int c = 0; c < cols; c++) 
                { 
                    lista.Add(new Postazione 
                    { 
                        CodiceUnivoco = $"{prefix}-{counter}", 
                        Nome = $"{prefix} {counter}", 
                        Tipo = tipo, 
                        X = startX + (c * (width + gap)), 
                        Y = startY + (r * (height + gap)), 
                        Width = width,   // (Ho corretto il typo "wid. h" che c'era nel tuo codice)
                        Height = height, 
                        PostiTotali = 1 
                    }); 
                    counter++; 
                } 
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}