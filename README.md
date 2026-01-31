# Progetto: Laboratorio di Interfaccia Uomo-Macchina - Ce

**Autore:** Matteo Manganiello  
**Corso:** Tecnologie dei Sistemi Informatici  
**Anno Accademico:** 2025/2026

---

## Introduzione

Questo progetto è una web application che serve per prenotare postazioni di lavoro e servizi aziendali. L’idea è evitare i classici sistemi a tabelle, che spesso sono scomodi e poco chiari, e sostituirli con una mappa visiva e facile da usare.

L’utente può vedere una mappa interattiva dell’ufficio, controllare subito la disponibilità in tempo reale e fare più prenotazioni insieme usando un carrello.
In più, l’applicazione è divisa in ruoli, così ognuno vede e fa solo quello che gli serve:

* **User (Utente):** prenota postazioni e servizi, e può anche cancellare le prenotazioni se cambia idea.
* **Admin:** aggiorna i contenuti che l’utente vede nella homepage, come le notifiche aziendali e il menù settimanale.
* **SuperAdmin:** è l’orchestratore del sito: può fare tutte le azioni degli altri ruoli, ha una dashboard con tutti i dati del sito e può inviare notifiche agli admin.

L’applicazione è deployata online e gira su **Google Cloud Run**, quindi è accessibile tramite browser senza installare nulla.

## Obiettivi del progetto

Gli obiettivi principali del progetto sono:

* Usare l’ambiente di sviluppo richiesto, quindi **ASP.NET** e architettura **MVC**.
* Creare una soluzione moderna, che non è ancora molto diffusa sul mercato, soprattutto per la gestione delle postazioni di lavoro.
* Rendere l’esperienza utente il più semplice e intuitiva possibile.

## Requisiti

### 3.1 Requisiti funzionali
* **RF1: Visualizzazione Interattiva su Mappa** Il sistema deve permettere di scegliere le risorse tramite una planimetria interattiva, con colori chiari per capire subito lo stato (libero/occupato/parziale).
* **RF2: Gestione Ibrida e Carrello Multiplo** Il sistema deve permettere prenotazioni multiple in una sola sessione, gestendo sia risorse a capienza (es. ristorante) sia risorse a uso esclusivo (es. sale meeting).

### 3.2 Requisiti non funzionali
* **RNF1: User Experience Reattiva (SPA-like)** L’interfaccia deve aggiornare dati e prezzi in modo dinamico (con Vue.js) senza ricaricare la pagina.

## Tecnologie utilizzate

Per lo sviluppo del progetto sono state utilizzate le seguenti tecnologie:

### 4.1 Design e Prototipazione
* **Figma:** usato per progettare UI/UX e flussi prima di sviluppare.
* **Floorplanner:** usato per creare la planimetria 2D dell’ufficio, poi utilizzata come mappa interattiva.

### 4.2 Backend (Lato Server)
* **ASP.NET Core (MVC):** struttura principale dell’applicazione con pattern MVC.
* **C#:** logica di business e gestione prenotazioni.
* **MySQL:** database usato per salvare utenti, prenotazioni e contenuti del sito.

### 4.3 Frontend (Lato Client)
* **Vue.js:** usato nelle viste MVC per rendere la pagina dinamica senza refresh.
* **Bootstrap 5:** layout responsive e stile coerente dei componenti.

## Descrizione del funzionamento

Il sistema è stato pensato per essere facile da usare per chiunque. Ecco i passaggi principali:

1.  **Mappa e Colori:** appena l’utente entra vede la mappa dell’ufficio. I colori fanno capire subito tutto: se una stanza è verde è libera, se è rossa è occupata.
2.  **Grafici e Guide:** intorno alla mappa ci sono delle card utili. Alcune mostrano grafici per capire quanto è affollato l’ufficio. Altre spiegano regole e servizi disponibili.
3.  **Carrello Unico:** l’utente può mettere tutto nel carrello (ad esempio postazione + ristorante) e confermare tutto insieme con un solo click.


## Come avviare il progetto in locale

Per eseguire l'applicazione sulla propria macchina di sviluppo, è necessario seguire questi passaggi per attivare il database e lanciare il server.

### 1. Avvio del Database (XAMPP)
Il progetto si appoggia a un database MySQL locale gestito tramite XAMPP.

1.  Aprire il pannello di controllo di **XAMPP**.
2.  Cliccare sul pulsante **Start** in corrispondenza del modulo **MySQL**.
3.  Attendere che l'indicatore diventi verde e che appaia il numero di porta (solitamente 3306).

### 2. Esecuzione dell'Applicazione (.NET)
Una volta che il database è attivo:

1.  Aprire il terminale (o PowerShell) all'interno della cartella radice del progetto.
2.  Digitare il seguente comando per avviare l'applicazione con la funzionalità di *hot-reload* (ricarica automatica alle modifiche):

    ```bash
    dotnet watch run
    ```

3.  Attendere la compilazione: il browser si aprirà automaticamente mostrando la pagina iniziale dell'applicazione (solitamente su `https://localhost:7196` o porta simile).s


## Link utili

* [Editor Floorplanner](https://floorplanner.com/projects/180160063/editor)
* [Prototipo Figma](https://www.figma.com/design/vnJ8D0PBEFPyRsMZrkFyba/Untitled?node-id=0-1&p=f&t=w6btnuBEH7e88zIJ-0)
* [Live Demo (Cloud Run)](https://template-web-819095504988.europe-west1.run.app)
