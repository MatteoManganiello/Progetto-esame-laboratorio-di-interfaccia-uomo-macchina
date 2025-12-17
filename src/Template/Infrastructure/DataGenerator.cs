using System;
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

                // 1. CREIAMO GLI UTENTI
                var user1 = new User
                {
                    Email = "admin@test.com",
                    FirstName = "Mario",
                    LastName = "Rossi",
                    NickName = "SuperMario",
                    Password = "123" // Se hai messo l'hash, qui dovresti mettere l'hash di "123"
                };
                // Se usi l'hash SHA256, l'hash di "123" (o simile) va calcolato, 
                // oppure usa il metodo SetPassword se l'hai aggiunto in User.cs.
                // Per ora assumiamo tu stia testando con password in chiaro o hash semplice.
                if(string.IsNullOrEmpty(user1.Password)) user1.Password = "123";

                context.Users.AddRange(user1);

                // 2. CREIAMO LE POSTAZIONI (Mappa dell'ufficio)
                // Esempio basato sul tuo screenshot della mappa
                context.Postazioni.AddRange(
                    // Sala Eventi (Grande a sinistra)
                    new Postazione { Nome = "SALA EVENTI", Tipo = "Eventi", X = 0, Y = 0, Width = 400, Height = 300, PostiTotali = 50 },
                    
                    // Desk Room (Al centro)
                    new Postazione { Nome = "DESK ROOM", Tipo = "Singola", X = 410, Y = 0, Width = 200, Height = 300, PostiTotali = 10 },
                    
                    // Ristorante (A destra)
                    new Postazione { Nome = "RISTORANTE", Tipo = "Ristorante", X = 620, Y = 0, Width = 200, Height = 600, PostiTotali = 20 },

                    // Stanze in basso (Meeting e Dev Team)
                    new Postazione { Nome = "MEETING A", Tipo = "Riunioni", X = 0, Y = 310, Width = 150, Height = 200, PostiTotali = 6 },
                    new Postazione { Nome = "DEV TEAM 1", Tipo = "Team", X = 160, Y = 310, Width = 150, Height = 200, PostiTotali = 4 },
                    new Postazione { Nome = "DEV TEAM 2", Tipo = "Team", X = 320, Y = 310, Width = 150, Height = 200, PostiTotali = 4 },
                    new Postazione { Nome = "MEETING B", Tipo = "Riunioni", X = 480, Y = 310, Width = 140, Height = 200, PostiTotali = 6 }
                );

                context.SaveChanges();
                Console.WriteLine("--> DATI GENERATI CON SUCCESSO!");
            }
        }
    }
}