using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Template.Data;
using Template.Entities;

namespace Template.Infrastructure
{
    public class DataGenerator
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new TemplateDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<TemplateDbContext>>()))
            {
                // Se ci sono già postazioni, non fare nulla (il DB è già pieno)
                if (context.Postazioni.Any())
                {
                    return; 
                }

                Console.WriteLine("--> GENERAZIONE DATI DI PROVA...");

                // 1. CREIAMO GLI UTENTI CON RUOLI DIVERSI
                var superAdmin = new User
                {
                    Email = "superadmin@test.com",
                    FirstName = "Admin",
                    LastName = "Super",
                    NickName = "SuperAdmin",
                    Password = "123",
                    Ruolo = RuoliCostanti.SUPER_ADMIN
                };

                var admin = new User
                {
                    Email = "admin@test.com",
                    FirstName = "Mario",
                    LastName = "Rossi",
                    NickName = "Admin",
                    Password = "123",
                    Ruolo = RuoliCostanti.ADMIN
                };

                var user = new User
                {
                    Email = "user@test.com",
                    FirstName = "Giovanni",
                    LastName = "Bianchi",
                    NickName = "User",
                    Password = "123",
                    Ruolo = RuoliCostanti.USER
                };

                context.Users.AddRange(superAdmin, admin, user);

                // 2. CREIAMO LE POSTAZIONI (Layout originale della mappa)
                var postazioni = new List<Postazione>();
                int idCounter = 1;

                // Main Hall (Sala eventi grande)
                postazioni.Add(new Postazione 
                { 
                    CodiceUnivoco = "event-main", 
                    Nome = "Main Hall", 
                    Tipo = "Eventi", 
                    X = 35, 
                    Y = 30, 
                    Width = 350, 
                    Height = 250, 
                    PostiTotali = 50 
                });

                // Desk individuali (griglia 5x3)
                for (int r = 0; r < 5; r++) 
                { 
                    for (int c = 0; c < 3; c++) 
                    { 
                        postazioni.Add(new Postazione 
                        { 
                            CodiceUnivoco = $"Desk-{idCounter}", 
                            Nome = $"Desk {idCounter}", 
                            Tipo = "Singola", 
                            X = 570 + (c * 50), 
                            Y = 30 + (r * 50), 
                            Width = 30,   
                            Height = 30, 
                            PostiTotali = 1 
                        }); 
                        idCounter++; 
                    } 
                }

                // Team rooms
                postazioni.Add(new Postazione 
                { 
                    CodiceUnivoco = "dev-1", 
                    Nome = "Team Alpha", 
                    Tipo = "Team", 
                    X = 250, 
                    Y = 470, 
                    Width = 120, 
                    Height = 80, 
                    PostiTotali = 6 
                });
                
                postazioni.Add(new Postazione 
                { 
                    CodiceUnivoco = "dev-2", 
                    Nome = "Team Beta", 
                    Tipo = "Team", 
                    X = 450, 
                    Y = 470, 
                    Width = 120, 
                    Height = 80, 
                    PostiTotali = 6 
                });

                // Meeting rooms
                postazioni.Add(new Postazione 
                { 
                    CodiceUnivoco = "meet-1", 
                    Nome = "Sala Red", 
                    Tipo = "Riunioni", 
                    X = 40, 
                    Y = 485, 
                    Width = 125, 
                    Height = 50, 
                    PostiTotali = 6 
                });
                
                postazioni.Add(new Postazione 
                { 
                    CodiceUnivoco = "meet-2", 
                    Nome = "Sala Blue", 
                    Tipo = "Riunioni", 
                    X = 640, 
                    Y = 485, 
                    Width = 125, 
                    Height = 50, 
                    PostiTotali = 6 
                });

                // Tavoli ristorante
                for (int i = 0; i < 3; i++)
                {
                    postazioni.Add(new Postazione 
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

                context.Postazioni.AddRange(postazioni);
                context.SaveChanges();

                // 3. CREIAMO PRENOTAZIONI DI TEST CON PREZZI
                var random = new Random();
                var prenotazioni = new List<Prenotazione>();

                // Creiamo 20 prenotazioni distribuite tra gli utenti
                for (int i = 0; i < 20; i++)
                {
                    var utenteId = i % 3 == 0 ? superAdmin.Id : (i % 3 == 1 ? admin.Id : user.Id);
                    var prezzo = (decimal)(25 + random.Next(0, 75)); // Prezzi da 25 a 100 euro
                    var dataPrenotazione = DateTime.Now.AddDays(random.Next(-30, 30)); // Ultimi/prossimi 30 giorni

                    prenotazioni.Add(new Prenotazione
                    {
                        UserId = utenteId.ToString(),
                        PostazioneId = postazioni[random.Next(postazioni.Count)].Id,
                        DataPrenotazione = dataPrenotazione,
                        DataCreazione = DateTime.Now.AddDays(random.Next(-30, 0)),
                        NumeroPersone = random.Next(1, 5),
                        Prezzo = prezzo,
                        IsCancellata = random.NextDouble() < 0.1, // 10% cancellate
                        Note = i % 5 == 0 ? "Prenotazione da testare" : null
                    });
                }

                context.Prenotazioni.AddRange(prenotazioni);
                context.SaveChanges();
                Console.WriteLine("--> DATI GENERATI CON SUCCESSO!");
            }
        }
    }
}