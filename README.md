# Progetto, Laboratorio di Interfaccia Uomo-Macchina - Ce #

**Autore:** Matteo Manganiello 

**Corso:** Tecnologie dei Sistemi Informatici  

**Anno Accademico:** 2025/2026  

---

## 1. Introduzione
Il presente progetto illustra lo sviluppo di una web application dedicata alla prenotazione di postazioni di lavoro e servizi aziendali. 
L’obiettivo principale è stato superare la rigidità dei tradizionali sistemi tabellari, offrendo un’interfaccia visiva e intuitiva, 
nonché fornire una postazione o un’area di lavoro adatta a diverse tipologie di utenti, quali privati, studenti e altri profili.
Il sistema consente agli utenti di visualizzare una mappa interattiva dell’ufficio, verificare la disponibilità in tempo reale e gestire prenotazioni multiple tramite un carrello.

---

## 2. Obiettivi del progetto
Gli obiettivi principali del progetto sono:
- Rispettare l’ambiente di sviluppo, adottando il framework ASP.NET e seguendo lo standard architetturale MVC.
- Realizzare una soluzione innovativa, ancora poco diffusa sul mercato, in particolare nell’ambito della gestione delle postazioni di lavoro.
- Migliorare al massimo la user experience, offrendo un’interfaccia intuitiva, efficiente e orientata alle esigenze dell’utente.

---

## 3. Requisiti

### 3.1 Requisiti funzionali
- RF1: Visualizzazione Interattiva su Mappa Il sistema deve permettere la selezione delle risorse tramite una planimetria grafica interattiva, con feedback visivo immediato sullo stato (libero/occupato/parziale).
- RF2: Gestione Ibrida e Carrello Multiplo Il sistema deve supportare prenotazioni multiple in un’unica sessione, gestendo contemporaneamente logiche a capienza (es. Ristorante) e a uso esclusivo (es. Sale Meeting).

### 3.2 Requisiti non funzionali
- RNF1: User Experience Reattiva (SPA-like) L'interfaccia deve aggiornare dinamicamente dati e prezzi (tramite Vue.js) senza richiedere il ricaricamento della pagina web.

---

## 4. Tecnologie utilizzate
Per lo sviluppo del progetto sono state utilizzate le seguenti tecnologie:

4.1 Design e Prototipazione
- Figma: utilizzato per la progettazione dell’interfaccia utente (UI) e per lo studio della user experience (UX), consentendo la definizione dei flussi di navigazione e dello stile grafico prima della fase di sviluppo.
- Floorplanner: impiegato per la progettazione degli spazi dell’ufficio e per la creazione della planimetria 2D, utilizzata come base grafica della mappa interattiva.

4.2 Backend (Lato Server)
- ASP.NET Core (MVC): framework utilizzato come struttura portante dell’applicazione, garantendo il rispetto del pattern MVC, sicurezza e scalabilità.
- C#: linguaggio utilizzato per l’implementazione della logica di business e la gestione delle funzionalità di prenotazione.

4.3 Frontend (Lato Client)
- Vue.js: framework JavaScript integrato nelle viste MVC per garantire reattività e aggiornamenti dinamici dei contenuti senza ricaricare la pagina.
- Bootstrap 5: libreria utilizzata per la realizzazione di un layout responsive e per la stilizzazione coerente dei componenti dell’interfaccia.

---

## 6. Descrizione del funzionamento

Il sistema è stato pensato per essere facile da usare per chiunque. Ecco i passaggi principali:

- Mappa e Colori: Appena l'utente entra, vede la mappa dell'ufficio. Non servono liste complicate: i colori dicono subito tutto. Se una stanza è Verde è libera, se è Rossa è occupata.
- Grafici e Guide: Intorno alla mappa ci sono delle schede (card) molto utili. Alcune mostrano dei grafici per capire a colpo d'occhio quanto è affollato l'ufficio oggi. Altre schede funzionano come una guida: spiegano le regole e mostrano tutti i servizi disponibili.
- Carrello Unico: L'utente non deve fare tante prenotazioni separate. Può mettere tutto nel carrello (ad esempio: la scrivania per lavorare e il tavolo per il pranzo) e confermare l'intera giornata con un solo click finale.

---

## 7. Link utili
- https://floorplanner.com/projects/180160063/editor
- https://www.figma.com/design/vnJ8D0PBEFPyRsMZrkFyba/Untitled?node-id=0-1&p=f&t=w6btnuBEH7e88zIJ-0

