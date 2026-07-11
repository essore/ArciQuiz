# Architettura

## Stato architetturale attuale

ArciQuiz è una soluzione .NET 10 composta da tre progetti:

```text
Web -> Core
Web -> Infrasctructure -> Core
```

### Core

Contiene le entità EF/dominio:

- `Partita`;
- `Manche`;
- `MancheDomanda`;
- `Domanda`;
- `Player`;
- `PlayerPartita`;
- `MancheRispostaRicevuta`.

Contiene inoltre gli enum di stato e il record runtime `GameState`.

### Infrasctructure

Il nome contiene un refuso storico. La correzione non è necessaria per il prodotto e non deve essere mescolata a task funzionali.

Contiene:

- `ArciQuizDbContext` con EF Core e SQLite;
- configurazione delle relazioni;
- `ArciQuizDbInitializer` con dati demo limitati allo sviluppo;
- migrazione iniziale EF Core e snapshot del modello.

Il database viene aggiornato con `Database.Migrate()` all'avvio. Una factory
design-time permette agli strumenti EF di generare migrazioni senza dipendere
dal percorso dati runtime.

### Web

È una Blazor Web App con render mode Interactive Server.

Pagine principali presenti:

- amministrazione e riepilogo partite;
- catalogo domande;
- creazione e configurazione partita/manche;
- regia;
- proiettore.

Servizi presenti:

- `GameStateService`: stato live in memoria e notifiche tra circuiti;
- `LANAddressService`: scoperta dell'IPv4 privata;
- `QRCodeService`: generazione QR in PNG.

## Flusso runtime attuale

La regia aggiorna un singleton `GameStateService`. Il proiettore sottoscrive gli eventi del singleton e ricarica dal database la domanda da visualizzare.

Questo meccanismo dimostra la comunicazione regia-proiettore, ma non soddisfa ancora i requisiti finali:

- lo stato live è perso al riavvio;
- il singleton rappresenta una sola partita globale;
- timer e fasi non sono completi;
- non esiste il client squadra;
- non esiste un endpoint/servizio autorevole per registrare le risposte;
- non viene calcolato alcun punteggio.

## Persistenza attuale

SQLite viene configurato in `Program.cs` tramite `AddDbContextFactory`, con
database in `%LocalAppData%\ArciQuiz\arciquiz.db`.

Stato e limiti attuali:

- componenti Razor amministrativi iniettano direttamente `ArciQuizDbContext`, mentre il proiettore usa `IDbContextFactory`;
- nei circuiti Blazor di lunga durata è preferibile creare context brevi tramite factory;
- il seed demo parte in base alla presenza di una partita, anche se ora è limitato all'ambiente Development.

## Direzione architetturale approvata

Mantenere per ora .NET 10, Blazor Interactive Server e SQLite. Non è autorizzata una riscrittura o l'introduzione di un frontend separato.

Evolvere il prototipo con modifiche incrementali:

1. stabilire una build e una suite di test verdi;
2. adottare migrazioni EF Core prima di estendere stabilmente lo schema;
3. usare context brevi creati tramite `IDbContextFactory` nei flussi Blazor;
4. spostare le nuove regole di partita, risposta e punteggio in servizi testabili;
5. persistere ogni transizione necessaria al recupero dopo riavvio;
6. usare notifiche in memoria solo per aggiornare rapidamente le UI, non come fonte di verità;
7. mantenere server-authoritative timer, risposte e punteggi;
8. proteggere dashboard e operazioni admin con autenticazione locale di base.

Non introdurre repository generici, CQRS, message broker, microservizi o cloud: non sono proporzionati al progetto.

## Confini logici obiettivo

### Preparazione

Gestisce catalogo domande, import/export CSV, partite, manche, ordine e validazione dello stato `Pronta`.

### Partecipazione

Gestisce registrazione, login, sessione esclusiva del dispositivo e iscrizione tardiva dell'admin.

### Motore partita

Gestisce transizioni di fase, timer, domanda corrente, chiusura delle risposte e recupero dopo riavvio.

### Risposte e punteggio

Registra in modo idempotente una risposta per squadra/domanda, calcola il risultato e permette il ricalcolo dopo annullamento.

### Presentazione

Espone view model separati per dashboard, proiettore e smartphone. La soluzione corretta viene inclusa soltanto dopo la chiusura.

## Modello dati: lacune note

Il modello corrente non rappresenta ancora esplicitamente:

- credenziale/sessione attiva della squadra nella singola partita;
- stato persistente della partita e della domanda sufficiente al recupero;
- durata personalizzata della singola domanda;
- malus base e moltiplicatore della manche come regole complete;
- validità/annullamento della specifica `MancheDomanda`;
- punteggio assegnato e motivazione del calcolo;
- consumo e ripristino delle astensioni;
- audit minimo delle operazioni di annullamento.

Ogni estensione dello schema deve essere introdotta con una migrazione e coperta da test sulle regole interessate.

## Concorrenza e capacità

Il target massimo è 50 squadre, ognuna con un solo dispositivo attivo. Il design deve prevenire:

- doppia risposta dovuta a retry o doppio click;
- risposta dal dispositivo sostituito;
- accettazione dopo la scadenza;
- doppia chiusura della domanda;
- doppio annullamento e doppio ricalcolo.

Non è richiesta scalabilità multi-server. Un singolo processo e un singolo database SQLite sono sufficienti, purché le operazioni critiche siano brevi, atomiche e verificate.

## Distribuzione obiettivo

- PC Windows dell'associazione;
- database e configurazione locali;
- ascolto sulla LAN con URL/QR raggiungibile dagli smartphone;
- avvio semplificato per un utente non tecnico;
- nessuna dipendenza runtime da Internet;
- istruzioni per firewall, scelta dell'interfaccia di rete e recupero del file dati.
