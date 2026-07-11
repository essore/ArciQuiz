# Piano di lavoro

## Protocollo della coda

La coda è ordinata. Un agente autonomo prende esclusivamente il primo task `READY`, completa un solo task e segue `AGENTS.md`.

Deve esserci al massimo un task `READY`. Quando viene concluso, l'agente promuove a `READY` il primo task `PLANNED` con dipendenze soddisfatte.

Stati ammessi: `PLANNED`, `READY`, `IN_PROGRESS`, `BLOCKED`, `DONE`.

## Milestone M0 — Baseline affidabile

### AQ-001 — Ripristinare la compilazione

- Stato: `DONE`
- Priorità: P0
- Dipendenze: nessuna
- Obiettivo: riallineare i due costruttori di `GameState` al contratto corrente senza implementare nuove funzioni.
- Ambito: `GameStateService` e pubblicazione dello stato dalla regia; rinominare il file errato solo se necessario alla compilazione, altrimenti creare un task separato.
- Criteri di accettazione:
  - i nuovi campi ricevono valori espliciti coerenti con lo stato prototipale;
  - nessun cambiamento funzionale estraneo;
  - `dotnet build Web.slnx --no-restore` termina con zero errori.
- Validazione: build completa e controllo del diff.

### AQ-002 — Creare la baseline di test

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-001
- Obiettivo: aggiungere un progetto di test coerente con .NET 10 e almeno un test di dominio significativo, senza testare dettagli Razor.
- Criteri di accettazione:
  - il progetto test è incluso nella soluzione;
  - esiste un test sul versionamento monotono o su una regola pura estratta senza rifattorizzazioni estese;
  - `dotnet test Web.slnx` e `dotnet build Web.slnx` sono verdi;
  - README e AGENTS riportano il comando canonico.
- Validazione: test e build completi.

### AQ-003 — Correggere la gestione degli artefatti SQLite

- Stato: `BLOCKED`
- Priorità: P0
- Dipendenze: AQ-002
- Obiettivo: impedire il versionamento di database runtime, file WAL/SHM e altri artefatti locali.
- Criteri di accettazione:
  - `.gitignore` copre gli artefatti SQLite runtime;
  - i file runtime già tracciati vengono rimossi dall'indice senza cancellare dati utente non verificati;
  - avvio/build non ricreano file non ignorati nel worktree;
  - il percorso dati corrente è documentato.
- Validazione: `git status --short`, build e avvio controllato se possibile.
- Blocco: l'ambiente corrente fa terminare `dotnet restore` e `dotnet build`
  con esito negativo durante la risoluzione degli asset, senza errori riportati
  da MSBuild. Serve ripristinare una baseline di restore/build verificabile
  prima di poter soddisfare la validazione essenziale del task.

### AQ-004 — Stabilire migrazioni e percorso dati

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-003
- Obiettivo: sostituire `EnsureCreated` con una strategia EF Core evolutiva e definire un percorso SQLite locale stabile e recuperabile.
- Criteri di accettazione:
  - prima migrazione riproduce lo schema necessario;
  - database nuovo creato correttamente;
  - database esistente di prova viene preservato o la procedura di transizione è documentata;
  - seed demo limitato allo sviluppo e separato dai dati reali;
  - connection string/configurazione hanno una sola fonte effettiva.
- Validazione: test su database temporaneo, build e avvio.

### AQ-005 — Rendere sicuro il ciclo di vita del DbContext

- Stato: `READY`
- Priorità: P0
- Dipendenze: AQ-004
- Obiettivo: evitare `DbContext` di lunga durata nei componenti Blazor, seguendo il pattern factory già presente nel proiettore.
- Criteri di accettazione:
  - i componenti modificati creano context brevi per operazione;
  - nessun accesso concorrente allo stesso context;
  - comportamento CRUD esistente preservato;
  - test pertinenti e build verdi.
- Validazione: test, build e smoke test delle pagine CRUD.

## Milestone M1 — Preparazione della partita

### AQ-010 — Completare il modello persistente della prima release

- Stato: `PLANNED`
- Priorità: P0
- Dipendenze: AQ-005
- Obiettivo: estendere lo schema con le sole informazioni necessarie a sessioni squadra, timer, regole di manche, override domanda, esiti, annullamento e recupero.
- Criteri di accettazione:
  - schema coerente con PRODUCT e ARCHITECTURE;
  - vincolo univoco per nome squadra nella partita;
  - vincolo una risposta per squadra e occorrenza domanda;
  - migrazione con test di applicazione su database temporaneo;
  - nessuna logica di UI inclusa nel task.
- Validazione: test di schema/migrazione e build.

### AQ-011 — Validare la transizione della partita a Pronta

- Stato: `PLANNED`
- Priorità: P1
- Dipendenze: AQ-010
- Obiettivo: impedire che una partita incompleta venga dichiarata pronta.
- Criteri di accettazione:
  - almeno una manche;
  - ogni manche giocabile ha almeno una domanda valida;
  - ordine domande non ambiguo;
  - errori mostrati in italiano;
  - regole testate senza dipendere dalla UI.
- Validazione: test unitari, build e smoke test configurazione.

### AQ-012 — Importare ed esportare domande in CSV

- Stato: `PLANNED`
- Priorità: P1
- Dipendenze: AQ-011
- Obiettivo: offrire round-trip CSV del catalogo con categorie e difficoltà controllate.
- Criteri di accettazione:
  - formato e intestazioni documentati;
  - UTF-8 e testi italiani preservati;
  - righe errate segnalate con numero e motivo;
  - nessuna importazione parziale silenziosa;
  - export reimportabile senza perdita dei campi supportati.
- Validazione: test round-trip, file con errori, build e smoke test UI.

## Milestone M2 — Lobby e squadre

### AQ-020 — Proteggere la dashboard admin

- Stato: `PLANNED`
- Priorità: P0
- Dipendenze: AQ-010
- Obiettivo: aggiungere autenticazione locale di base alle route e operazioni amministrative.
- Criteri di accettazione:
  - credenziale configurata localmente e non versionata;
  - route admin/regia non accessibili anonimamente;
  - proiettore e registrazione rimangono pubblici nella LAN;
  - logout disponibile;
  - test di autorizzazione.
- Validazione: test di autenticazione/autorizzazione, build e smoke test.

### AQ-021 — Registrare e amministrare le squadre

- Stato: `PLANNED`
- Priorità: P0
- Dipendenze: AQ-020
- Obiettivo: creare iscrizione via QR/form e CRUD admin per la partita attiva.
- Criteri di accettazione:
  - nome univoco per partita;
  - password richiesta e recuperabile dall'admin secondo la decisione accettata;
  - registrazione pubblica disponibile solo in lobby;
  - admin può aggiungere una squadra in ritardo;
  - validazioni e messaggi italiani.
- Validazione: test integrazione dei casi nominali/duplicati e smoke test mobile.

### AQ-022 — Rendere esclusiva la sessione del dispositivo squadra

- Stato: `PLANNED`
- Priorità: P0
- Dipendenze: AQ-021
- Obiettivo: fare in modo che l'ultimo login valido sia l'unico autorizzato a rispondere.
- Criteri di accettazione:
  - nuovo login invalida la sessione precedente;
  - dispositivo precedente riceve un messaggio chiaro e non può inviare risposte;
  - refresh sul dispositivo attivo non crea una seconda sessione;
  - sessione recuperabile dopo riavvio dell'app.
- Validazione: test con due client e build.

### AQ-023 — Completare la lobby pubblica

- Stato: `PLANNED`
- Priorità: P1
- Dipendenze: AQ-021
- Obiettivo: mostrare sul proiettore QR e conteggio aggiornato delle squadre iscritte.
- Criteri di accettazione:
  - URL generato dalla configurazione effettiva, senza porta hardcoded;
  - conteggio aggiornato senza refresh manuale;
  - stato lobby persistente;
  - registrazione pubblica chiusa all'avvio della partita.
- Validazione: test integrazione e smoke test da dispositivo nella LAN.

## Milestone M3 — Motore della partita

### AQ-030 — Implementare la macchina a stati persistente

- Stato: `PLANNED`
- Priorità: P0
- Dipendenze: AQ-010, AQ-023
- Obiettivo: rendere persistenti e validate le transizioni lobby, domanda, soluzione, classifica, fine manche e fine partita.
- Criteri di accettazione:
  - solo transizioni valide;
  - comandi ripetuti idempotenti;
  - ripresa dello stato dopo riavvio;
  - notifiche UI derivate dallo stato persistito;
  - test della matrice delle transizioni.
- Validazione: test unitari/integrati, riavvio simulato e build.

### AQ-031 — Implementare timer server-authoritative

- Stato: `PLANNED`
- Priorità: P0
- Dipendenze: AQ-030
- Obiettivo: gestire durata standard di manche e override per domanda senza fidarsi dell'orologio client.
- Criteri di accettazione:
  - scadenza persistita in UTC;
  - countdown coerente su proiettore e telefoni;
  - risposte dopo scadenza rifiutate;
  - riavvio ricostruisce correttamente tempo residuo o chiude una domanda già scaduta;
  - test con clock controllabile.
- Validazione: test timer/casi limite e build.

### AQ-032 — Implementare il client squadra e l'invio risposta

- Stato: `PLANNED`
- Priorità: P0
- Dipendenze: AQ-022, AQ-031
- Obiettivo: mostrare domanda e A/B/C/D e accettare una sola risposta immutabile.
- Criteri di accettazione:
  - soluzione non presente nel payload prima della chiusura;
  - doppio click/retry non duplica la risposta;
  - risposta non modificabile;
  - sessione sostituita e risposta tardiva rifiutate;
  - UI mobile leggibile.
- Validazione: test concorrenza/idempotenza, build e smoke test mobile.

### AQ-033 — Calcolare punteggio e astensioni

- Stato: `PLANNED`
- Priorità: P0
- Dipendenze: AQ-032
- Obiettivo: applicare integralmente le formule di PRODUCT alla chiusura della domanda.
- Criteri di accettazione:
  - punti tempo tra 50% e 100%;
  - moltiplicatori su punti e malus;
  - astensioni gratuite fino alla soglia per manche;
  - oltre soglia stesso malus dell'errore;
  - calcolo deterministico e idempotente;
  - dettaglio persistito per consentire audit e ricalcolo.
- Validazione: test tabellari su corretto, errato, limite tempo, soglia astensioni e moltiplicatori.

### AQ-034 — Mostrare soluzione e distribuzione risposte

- Stato: `PLANNED`
- Priorità: P1
- Dipendenze: AQ-033
- Obiettivo: aggiornare proiettore e telefoni dopo la chiusura.
- Criteri di accettazione:
  - proiettore mostra soluzione e conteggi A/B/C/D;
  - telefono mostra corretto, errato o astenuto e variazione punti;
  - nessun conteggio live durante la domanda;
  - squadre iscritte in ritardo gestite coerentemente.
- Validazione: test view model e smoke test multi-client.

### AQ-035 — Implementare classifiche e podio

- Stato: `PLANNED`
- Priorità: P1
- Dipendenze: AQ-033
- Obiettivo: produrre classifica parziale, di fine manche e finale.
- Criteri di accettazione:
  - regia può scegliere la classifica dopo ogni domanda;
  - classifica obbligatoria a fine manche;
  - finale con podio e vincitori;
  - parità visualizzata ex aequo;
  - risultati ricostruibili dai dettagli persistiti.
- Validazione: test ordinamento/parità e smoke test proiettore.

### AQ-036 — Annullare una domanda e ricalcolare

- Stato: `PLANNED`
- Priorità: P0
- Dipendenze: AQ-033, AQ-035
- Obiettivo: annullare una specifica domanda giocata senza cancellare il catalogo.
- Criteri di accettazione:
  - punti, malus e consumi astensione dell'occorrenza vengono neutralizzati;
  - classifiche ricalcolate;
  - domanda catalogo segnalata per revisione;
  - operazione tracciata e idempotente;
  - test con annullamento prima e dopo il superamento della soglia astensioni.
- Validazione: test di ricalcolo, build e smoke test regia.

## Milestone M4 — Esercizio reale

### AQ-040 — Verificare recupero completo dopo riavvio

- Stato: `PLANNED`
- Priorità: P0
- Dipendenze: AQ-030, AQ-031, AQ-033
- Obiettivo: riprendere una partita senza perdita o duplicazione di stato.
- Criteri di accettazione:
  - test per lobby, domanda aperta, domanda scaduta, soluzione e fine manche;
  - sessioni squadra e classifiche coerenti;
  - procedura manuale documentata.
- Validazione: test integrazione e prova di arresto/riavvio.

### AQ-041 — Ripulire residui del prototipo e avvisi

- Stato: `PLANNED`
- Priorità: P2
- Dipendenze: AQ-040
- Obiettivo: rimuovere pagine demo, codice commentato, campi inutilizzati e avvisi non giustificati senza rifattorizzazioni architetturali.
- Criteri di accettazione:
  - nessuna pagina template raggiungibile;
  - nessun warning del compilatore nel codice proprietario;
  - dipendenze vulnerabili o incoerenti risolte con versioni compatibili;
  - rinomina dei refusi valutata separatamente per impatto.
- Validazione: build, test e controllo navigazione.

### AQ-042 — Preparare distribuzione Windows offline

- Stato: `PLANNED`
- Priorità: P0
- Dipendenze: AQ-040, AQ-041
- Obiettivo: rendere avvio, accesso LAN e conservazione dati gestibili da una persona non tecnica.
- Criteri di accettazione:
  - pacchetto riproducibile per Windows;
  - procedura di avvio semplice;
  - URL/QR LAN affidabile;
  - istruzioni firewall e scelta rete;
  - posizione database e procedura di copia documentate;
  - nessuna dipendenza da Internet a runtime.
- Validazione: prova su macchina/cartella pulita e almeno due dispositivi nella LAN.

### AQ-043 — Collaudare il carico di 50 squadre

- Stato: `PLANNED`
- Priorità: P1
- Dipendenze: AQ-042
- Obiettivo: verificare il flusso critico con 50 squadre simulate.
- Criteri di accettazione:
  - 50 sessioni registrate;
  - invii concorrenti senza duplicati o perdita;
  - chiusura domanda e classifica entro tempi adatti alla serata;
  - risultati e limiti documentati.
- Validazione: test di carico locale ripetibile.

## Decisioni ancora non trasformate in task

Nessuna decisione funzionale blocca `AQ-001`. Eventuali nuove esigenze vanno registrate in `DECISIONS.md` e poi trasformate in task atomici senza interrompere l'ordine corrente, salvo priorità esplicita del proprietario.
