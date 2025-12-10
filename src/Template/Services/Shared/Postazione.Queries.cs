/*
 * FILE: Postazione.Queries.cs
 * SCOPO: Gestore Dati Unico.
 * Include: Eventi, Open Space, Team, Meeting E RISTORANTE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Template.Services.Shared
{
    // ==========================================
    // DTO PER LA MAPPA GENERALE
    // ==========================================
    public class MappaQuery { public DateTime Data { get; set; } }

    public class MappaDTO
    {
        public IEnumerable<PostazioneDTO> Postazioni { get; set; }
        public class PostazioneDTO
        {
            public int Id { get; set; }
            public string CodiceSVG { get; set; }
            public string Nome { get; set; }
            public string Tipo { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            
            // STATO
            public bool IsOccupata { get; set; } // Rosso se True
            public int PostiTotali { get; set; } // Capienza Max
            public int PostiOccupati { get; set; } // Quanti sono già presi
        }
    }

    // ==========================================
    // DTO PER IL RISTORANTE (AGGIUNTO)
    // ==========================================
    public class RistoranteDTO
    {
        public IEnumerable<TavoloDTO> Tavoli { get; set; }
        public class TavoloDTO
        {
            public int Id { get; set; }
            public string Nome { get; set; }
            public int Posti { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public bool IsOccupato { get; set; }
        }
    }

    public partial class SharedService
    {
        // -----------------------------------------------------------------------
        // METODO 1: QUERY GENERALE (Usata da PrenotazioneController)
        // -----------------------------------------------------------------------
        public async Task<MappaDTO> Query(MappaQuery qry)
        {
            await EnsureSeeding(); // Assicura che i dati esistano (con le tue coordinate)

            var postazioni = await _dbContext.Postazioni.Where(x => x.IsAbilitata).ToListAsync();
            
            // Recuperiamo tutte le prenotazioni di oggi
            var prenotazioniOggi = await _dbContext.Prenotazioni
                .Where(x => x.DataPrenotazione.Date == qry.Data.Date)
                .Select(x => x.PostazioneId) 
                .ToListAsync();

            return new MappaDTO
            {
                Postazioni = postazioni.Select(p => {
                    // Contiamo quante volte appare questo ID nelle prenotazioni
                    int occupati = prenotazioniOggi.Count(id => id == p.Id);
                    int capienza = p.PostiTotali > 0 ? p.PostiTotali : 1;

                    // Logica colore Rosso:
                    // - Ristorante: Rosso solo se PIENO.
                    // - Uffici: Rosso se c'è ALMENO UNA prenotazione.
                    bool isRed = (p.Tipo == "Ristorante") 
                                 ? occupati >= capienza 
                                 : occupati > 0;

                    return new MappaDTO.PostazioneDTO
                    {
                        Id = p.Id,
                        CodiceSVG = p.CodiceUnivoco,
                        Nome = p.Nome,
                        Tipo = p.Tipo,
                        X = p.X, Y = p.Y, Width = p.Width, Height = p.Height,
                        PostiTotali = capienza,
                        PostiOccupati = occupati,
                        IsOccupata = isRed
                    };
                })
            };
        }

        // -----------------------------------------------------------------------
        // METODO 2: GET RISTORANTE (Usata da RistorazioneController) - AGGIUNTO
        // -----------------------------------------------------------------------
        public async Task<RistoranteDTO> GetRistorante(DateTime date)
        {
            await EnsureSeeding();

            var tavoli = await _dbContext.Postazioni
                .Where(x => x.Tipo == "Ristorante" && x.IsAbilitata)
                .ToListAsync();

            var occupazioni = await _dbContext.Prenotazioni
                .Where(x => x.DataPrenotazione.Date == date.Date)
                .ToListAsync();

            return new RistoranteDTO
            {
                Tavoli = tavoli.Select(t => {
                    int occupati = occupazioni.Count(p => p.PostazioneId == t.Id);
                    int capienza = t.PostiTotali > 0 ? t.PostiTotali : 4;

                    return new RistoranteDTO.TavoloDTO
                    {
                        Id = t.Id,
                        Nome = t.Nome,
                        Posti = capienza,
                        X = t.X, Y = t.Y, Width = t.Width, Height = t.Height,
                        IsOccupato = occupati >= capienza
                    };
                })
            };
        }

        // -----------------------------------------------------------------------
        // PRIVATE: SEEDING DATI (Coordinate ESATTE fornite da te)
        // -----------------------------------------------------------------------
        private async Task EnsureSeeding()
        {
            // Se ci sono già dati, non facciamo nulla
            if (await _dbContext.Postazioni.AnyAsync()) return;

            var lista = new List<Postazione>();
            int idCounter = 1;

            // --- UFFICI E SALE ---
            // Sala Eventi
            lista.Add(new Postazione { CodiceUnivoco = "event-main", Nome = "Main Hall", Tipo = "Eventi", X = 35, Y = 30, Width = 350, Height = 250 });

            // Open Space (15 posti: 5 righe x 3 colonne)
            AggiungiGruppo(lista, ref idCounter, "Desk", "Singola", startX: 570, startY: 30, rows: 5, cols: 3, width: 30, height: 30, gap: 20);

            // Team Rooms (Capienza 6)
            lista.Add(new Postazione { CodiceUnivoco = "dev-1", Nome = "Team Alpha", Tipo = "Team", X = 250, Y = 470, Width = 120, Height = 80, PostiTotali = 6 });
            lista.Add(new Postazione { CodiceUnivoco = "dev-2", Nome = "Team Beta", Tipo = "Team", X = 450, Y = 470, Width = 120, Height = 80, PostiTotali = 6 });

            // Sale Riunioni (Capienza 8)
            lista.Add(new Postazione { CodiceUnivoco = "meet-1", Nome = "Sala Red", Tipo = "Riunioni", X = 40, Y = 485, Width = 125, Height = 50, PostiTotali = 8 });
            lista.Add(new Postazione { CodiceUnivoco = "meet-2", Nome = "Sala Blue", Tipo = "Riunioni", X = 640, Y = 485, Width = 125, Height = 50, PostiTotali = 8 });

            // --- RISTORANTE ---
            // 3 Tavoli Rettangolari da 4 posti nel corridoio destro
            for (int i = 0; i < 3; i++)
            {
                lista.Add(new Postazione 
                { 
                    CodiceUnivoco = $"rist-{i+1}", 
                    Nome = $"Tavolo {i+1}", 
                    Tipo = "Ristorante", 
                    X = 820,              
                    Y = 45 + (i * 115), // Spaziatura verticale modificata come da richiesta
                    Width = 100,
                    Height = 50,
                    PostiTotali = 4
                });
            }

            _dbContext.Postazioni.AddRange(lista);
            await _dbContext.SaveChangesAsync();
        }

        private void AggiungiGruppo(List<Postazione> lista, ref int counter, string prefix, string tipo, int startX, int startY, int rows, int cols, int width, int height, int gap)
        {
            for (int r = 0; r < rows; r++) { for (int c = 0; c < cols; c++) { lista.Add(new Postazione { CodiceUnivoco = $"{prefix}-{counter}", Nome = $"{prefix} {counter}", Tipo = tipo, X = startX + (c * (width + gap)), Y = startY + (r * (height + gap)), Width = width, Height = height, PostiTotali = 1 }); counter++; } }
        }
    }
}