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
  - `dotnet build ArciQuiz.slnx --no-restore` termina con zero errori.
- Validazione: build completa e controllo del diff.

### AQ-002 — Creare la baseline di test

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-001
- Obiettivo: aggiungere un progetto di test coerente con .NET 10 e almeno un test di dominio significativo, senza testare dettagli Razor.
- Criteri di accettazione:
  - il progetto test è incluso nella soluzione;
  - esiste un test sul versionamento monotono o su una regola pura estratta senza rifattorizzazioni estese;
  - `dotnet test ArciQuiz.slnx` e `dotnet build ArciQuiz.slnx` sono verdi;
  - README e AGENTS riportano il comando canonico.
- Validazione: test e build completi.

### AQ-003 — Correggere la gestione degli artefatti SQLite

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-002
- Obiettivo: impedire il versionamento di database runtime, file WAL/SHM e altri artefatti locali.
- Criteri di accettazione:
  - `.gitignore` copre gli artefatti SQLite runtime;
  - i file runtime già tracciati vengono rimossi dall'indice senza cancellare dati utente non verificati;
  - avvio/build non ricreano file non ignorati nel worktree;
  - il percorso dati corrente è documentato.
- Validazione: `git status --short`, build e avvio controllato se possibile.

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

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-004
- Obiettivo: evitare `DbContext` di lunga durata nei componenti Blazor, seguendo il pattern factory già presente nel proiettore.
- Criteri di accettazione:
  - i componenti modificati creano context brevi per operazione;
  - nessun accesso concorrente allo stesso context;
  - comportamento CRUD esistente preservato;
  - test pertinenti e build verdi.
- Validazione: test, build e smoke test delle pagine CRUD completati con successo (rimozione EventLog Windows e verifica esecuzione runtime confermata).

## Milestone M1 — Preparazione della partita

### AQ-010 — Completare il modello persistente della prima release

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-005
- Obiettivo: estendere lo schema con le sole informazioni necessarie a sessioni squadra, timer, regole di manche, override domanda, esiti, annullamento e recupero.
- Criteri di accettazione:
  - schema coerente con PRODUCT e ARCHITECTURE;
  - vincolo univoco per nome squadra nella partita;
  - vincolo una risposta per squadra e occorrenza domanda;
  - migrazione con test di applicazione su database temporaneo;
  - nessuna logica di UI inclusa nel task.
- Validazione: test di schema/migrazione e build completati con successo (6 test xUnit superati, 0 errori di compilazione).

### AQ-011 — Validare la transizione della partita a Pronta

- Stato: `DONE`
- Priorità: P1
- Dipendenze: AQ-010
- Obiettivo: impedire che una partita incompleta venga dichiarata pronta.
- Criteri di accettazione:
  - almeno una manche;
  - ogni manche giocabile ha almeno una domanda valida;
  - ordine domande non ambiguo;
  - errori mostrati in italiano;
  - regole testate senza dipendere dalla UI.
- Validazione: test unitari, build e smoke test configurazione completati (11 test xUnit superati, 0 errori di compilazione).

### AQ-012 — Importare ed esportare domande in CSV

- Stato: `DONE`
- Priorità: P1
- Dipendenze: AQ-011
- Obiettivo: offrire round-trip CSV del catalogo con categorie e difficoltà controllate.
- Criteri di accettazione:
  - formato e intestazioni documentati;
  - UTF-8 e testi italiani preservati;
  - righe errate segnalate con numero e motivo;
  - nessuna importazione parziale silenziosa;
  - export reimportabile senza perdita dei campi supportati.
- Validazione: test round-trip e file con errori, build e smoke test UI completati (13 test xUnit superati, 0 errori di compilazione).

## Milestone M2 — Lobby e squadre

### AQ-020 — Proteggere la dashboard admin

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-010
- Obiettivo: aggiungere autenticazione locale di base alle route e operazioni amministrative.
- Criteri di accettazione:
  - credenziale configurata localmente e non versionata;
  - route admin/regia non accessibili anonimamente;
  - proiettore e registrazione rimangono pubblici nella LAN;
  - logout disponibile;
  - test di autorizzazione.
- Validazione: test di autorizzazione, build e smoke test completati (22 test xUnit superati, 0 errori di compilazione; redirect anonimo, login, logout e proiettore pubblico verificati manualmente dal proprietario).

### AQ-021 — Registrare e amministrare le squadre

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-020
- Obiettivo: creare iscrizione via QR/form e CRUD admin per la partita attiva.
- Criteri di accettazione:
  - nome univoco per partita;
  - password richiesta e recuperabile dall'admin secondo la decisione accettata;
  - registrazione pubblica disponibile solo in lobby;
  - admin può aggiungere una squadra in ritardo;
  - validazioni e messaggi italiani.
- Validazione: test di registrazione nominale, duplicato e inserimento admin ritardato, build e smoke test mobile completati (26 test xUnit superati, 0 errori di compilazione).

### AQ-022 — Rendere esclusiva la sessione del dispositivo squadra

- Stato: `DONE`
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

- Stato: `DONE`
- Priorità: P1
- Dipendenze: AQ-021
- Obiettivo: mostrare sul proiettore QR e conteggio aggiornato delle squadre iscritte.
- Criteri di accettazione:
  - URL generato dalla configurazione effettiva, senza porta hardcoded;
  - conteggio aggiornato senza refresh manuale;
  - stato lobby persistente;
  - registrazione pubblica chiusa all'avvio della partita.
- Validazione: test di generazione URL LAN, build e smoke test da dispositivo nella LAN completati (31 test xUnit superati, 0 errori di compilazione).

### AQ-024 — Creare l'ingresso unico della squadra

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-022, AQ-023, AQ-031
- Obiettivo: consentire a una squadra di entrare, registrarsi o recuperare la sessione partendo sempre dallo stesso QR, senza digitare URL.
- Criteri di accettazione:
  - il proiettore mostra in ogni fase un QR stabile verso `/gioca`;
  - `/gioca` instrada in base a stato partita e sessione valida;
  - la registrazione crea immediatamente anche cookie e sessione del dispositivo;
  - il cookie persiste per la durata della serata e resta subordinato al token esclusivo nel database;
  - login, sessione sostituita e iscrizioni chiuse hanno percorsi mobile chiari;
  - nessun link deve essere inserito manualmente dalla squadra.
- Validazione: test di routing/sessione inclusi nella suite completa (63 test superati), build riuscita e smoke HTTP reale di registrazione, login e rientro da `/gioca` completato; verificata la separazione tra cookie squadra e admin, incluso il circuito Blazor.

### AQ-025 — Creare la shell mobile-first della squadra

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-024, AQ-031
- Obiettivo: presentare alla squadra una singola UI mobile derivata dallo stato persistito, pronta per l'invio risposta di AQ-032.
- Criteri di accettazione:
  - stati distinti per attesa, domanda, tempo scaduto, soluzione, classifica, fine manche e fine partita;
  - domanda e opzioni sono leggibili su smartphone, senza esporre la soluzione durante la domanda;
  - nome squadra e stato connessione sono sempre riconoscibili;
  - refresh e riapertura dal QR ricostruiscono la vista corrente;
  - il view model inviato alla UI contiene soltanto i dati consentiti nella fase corrente;
  - layout verificato a larghezza mobile.
- Validazione: test del view model per tutte le fasi inclusi nella suite completa (63 test superati), build riuscita e smoke HTTP della shell mobile completato; viewport e regole responsive verificati nel markup/CSS.

## Milestone M3 — Motore della partita

### AQ-030 — Implementare la macchina a stati persistente

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-010, AQ-023
- Obiettivo: rendere persistenti e validate le transizioni lobby, domanda, soluzione, classifica, fine manche e fine partita.
- Criteri di accettazione:
  - solo transizioni valide;
  - comandi ripetuti idempotenti;
  - ripresa dello stato dopo riavvio;
  - notifiche UI derivate dallo stato persistito;
  - test della matrice delle transizioni.
- Validazione: test unitari/integrati e riavvio simulato completati (36 test xUnit superati); build riuscita con 0 errori.

### AQ-031 — Implementare timer server-authoritative

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-030
- Obiettivo: gestire durata standard di manche e override per domanda senza fidarsi dell'orologio client.
- Criteri di accettazione:
  - scadenza persistita in UTC;
  - countdown coerente su proiettore e telefoni;
  - risposte dopo scadenza rifiutate;
  - riavvio ricostruisce correttamente tempo residuo o chiude una domanda già scaduta;
  - test con clock controllabile.
- Validazione: test timer e casi limite completati (42 test xUnit superati); build riuscita con 0 errori.

### AQ-032 — Implementare il client squadra e l'invio risposta

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-025
- Obiettivo: mostrare domanda e A/B/C/D e accettare una sola risposta immutabile.
- Criteri di accettazione:
  - soluzione non presente nel payload prima della chiusura;
  - doppio click/retry non duplica la risposta;
  - risposta non modificabile;
  - sessione sostituita e risposta tardiva rifiutate;
  - UI mobile leggibile.
- Validazione: test concorrenza/idempotenza (5 casi), suite completa (68 casi), build completa e verifica markup/CSS mobile completati.

### AQ-033 — Calcolare punteggio e astensioni

- Stato: `DONE`
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
- Validazione: creati test unitari tabellari per coefficiente tempo (1.0-0.5), punti base/moltiplicatori, malus e gestione soglia astensioni; verificate astensioni automatiche per squadre senza risposta e idempotenza (84 test xUnit superati, 0 errori di compilazione).

### AQ-034 — Mostrare soluzione e distribuzione risposte

- Stato: `DONE`
- Priorità: P1
- Dipendenze: AQ-033
- Obiettivo: aggiornare proiettore e telefoni dopo la chiusura.
- Criteri di accettazione:
  - proiettore mostra soluzione e conteggi A/B/C/D;
  - telefono mostra corretto, errato o astenuto e variazione punti;
  - nessun conteggio live durante la domanda;
  - squadre iscritte in ritardo gestite coerentemente.
- Validazione: test view model per esiti corretto/errato/astenuto e squadra iscritta in ritardo (12 casi pertinenti), suite completa (88 casi) e build completate; avvio isolato riuscito, smoke interattivo multi-client non automatizzato per errore del connettore browser.

### AQ-035 — Implementare classifiche e podio

- Stato: `DONE`
- Priorità: P1
- Dipendenze: AQ-033
- Obiettivo: produrre classifica parziale, di fine manche e finale.
- Criteri di accettazione:
  - regia può scegliere la classifica dopo ogni domanda;
  - classifica obbligatoria a fine manche;
  - finale con podio e vincitori;
  - parità visualizzata ex aequo;
  - risultati ricostruibili dai dettagli persistiti.
- Validazione: test unitario ordinamento/parità e test d'integrazione su punteggi persistiti e occorrenze annullate (2 casi), suite completa (90 casi) e build completate; smoke visivo proiettore da ripetere quando il connettore browser sarà disponibile.

### AQ-036 — Annullare una domanda e ricalcolare

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-033, AQ-035
- Obiettivo: annullare una specifica domanda giocata senza cancellare il catalogo.
- Criteri di accettazione:
  - punti, malus e consumi astensione dell'occorrenza vengono neutralizzati;
  - classifiche ricalcolate;
  - domanda catalogo segnalata per revisione;
  - operazione tracciata e idempotente;
  - test con annullamento prima e dopo il superamento della soglia astensioni.
- Validazione: test d'integrazione su annullamento prima/dopo il superamento della soglia, idempotenza e classifica ricalcolata (2 casi), suite completa (92 casi) e build completate; smoke interattivo regia da ripetere quando il connettore browser sarà disponibile.

## Milestone M4 — Esercizio reale

### AQ-040 — Verificare recupero completo dopo riavvio

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-030, AQ-031, AQ-033
- Obiettivo: riprendere una partita senza perdita o duplicazione di stato.
- Criteri di accettazione:
  - test per lobby, domanda aperta, domanda scaduta, soluzione e fine manche;
  - sessioni squadra e classifiche coerenti;
  - procedura manuale documentata.
- Validazione: 5 test d'integrazione su SQLite riaperto per lobby, domanda aperta/scaduta, soluzione e fine manche; verificate sessioni e classifica persistite, idempotenza della chiusura scaduta, suite completa (97 casi) e build completate. Procedura manuale in README.

## Milestone M4a — Pulizia funzionale e usabilità

### AQ-044 — Gestire partita attiva, storico e navigazione admin

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-040
- Obiettivo: rendere esplicita la partita attiva e permettere all'admin di aprire la regia e il proiettore della partita scelta, senza usare implicitamente l'ultima partita.
- Criteri di accettazione:
  - il bottone per aprire il proiettore apre una vista funzionante per la partita selezionata;
  - l'admin può scegliere quale partita è attiva, con una sola partita attiva alla volta;
  - dall'elenco è possibile aprire la regia di una partita specifica;
  - una partita vecchia può essere marcata come conclusa e non può tornare attiva senza un'azione esplicita;
  - lo storico e i punteggi delle partite concluse restano conservati; eventuale cancellazione definitiva richiede una decisione separata.
- Validazione: test d'integrazione sulla selezione esclusiva della partita attiva, sul rifiuto di riattivare una conclusa e sull'ingresso pubblico (2 casi); suite completa (99 casi), build e controllo migrazioni completati. Smoke interattivo da ripetere con un'istanza locale disponibile.

### AQ-045 — Semplificare menu e layout della dashboard admin

- Stato: `DONE`
- Priorità: P1
- Dipendenze: AQ-044
- Obiettivo: ridurre la duplicazione nella dashboard e rendere immediato l'avvio di una nuova partita.
- Criteri di accettazione:
  - il bottone/collegamento rapido `Gestisci domande` viene rimosso dalla pagina dashboard admin;
  - la voce `Gestisci domande` resta disponibile nel menu di navigazione admin;
  - l'elenco delle partite è visibile direttamente nella dashboard, senza un bottone dedicato per mostrarlo;
  - il bottone `Nuova partita` è posizionato in alto a destra dell'elenco o della relativa intestazione;
  - le operazioni restano disponibili solo agli utenti autorizzati.
- Validazione: controllo statico di dashboard e menu, suite completa (99 casi) e build completati. Smoke a larghezza desktop/ridotta da ripetere con un'istanza locale disponibile.

### AQ-046 — Correggere il contatore domande di `/partita`

- Stato: `DONE`
- Priorità: P1
- Dipendenze: AQ-045
- Obiettivo: mostrare il numero reale di domande associate alla partita visualizzata.
- Criteri di accettazione:
  - il contatore non mostra sempre zero quando esistono domande associate;
  - il conteggio riguarda la partita corrente e non l'ultima partita o il catalogo globale;
  - il valore si aggiorna dopo aggiunta o rimozione di un'associazione;
  - il caso di partita senza domande continua a mostrare zero.
- Validazione: test d'integrazione del caricamento del riepilogo con partite vuote e popolate, suite completa (100 casi) e build completati. Smoke interattivo da ripetere con un'istanza locale disponibile.

### AQ-047 — Impedire associazioni duplicate e nascondere gli ID interni

- Stato: `DONE`
- Priorità: P1
- Dipendenze: AQ-046
- Obiettivo: rendere coerente l'associazione delle domande alle manche della stessa partita e rimuovere gli identificativi tecnici dall'interfaccia.
- Criteri di accettazione:
  - la stessa domanda non può essere associata a due manche della stessa partita;
  - il vincolo viene verificato lato server prima del salvataggio, con messaggio chiaro in italiano;
  - le pagine di gestione manche/domande non mostrano gli ID tecnici nei testi, nelle tabelle o nei controlli visibili;
  - i collegamenti e le operazioni continuano a usare gli identificativi internamente senza esporli all'utente.
- Validazione: test d'integrazione di associazione nominale, duplicata nella stessa partita e riuso in un'altra partita, suite completa (102 casi) e build completati. Smoke interattivo della gestione manche/domande da ripetere con un'istanza locale disponibile.

### AQ-048 — Riorganizzare la regia della partita

- Stato: `IN_PROGRESS`
- Priorità: P1
- Dipendenze: AQ-047
- Obiettivo: rendere la regia leggibile e coerente con la partita selezionata, riducendo i comandi ambigui.
- Criteri di accettazione:
  - il proiettore è apribile con un'azione evidente in alto a destra;
  - i comandi sono distribuiti in tre righe: partita, manche e domanda;
  - i bottoni usano icone standard accompagnate da testo e da un'etichetta accessibile;
  - viene verificato se il selettore della manche è ancora necessario: se non serve al flusso corrente viene rimosso, altrimenti mostra chiaramente il contesto e non consente di agire sulla manche sbagliata;
  - la regia opera sulla partita selezionata e non ricade silenziosamente sull'ultima partita.
- Validazione: smoke test delle azioni di partita/manche/domanda e verifica manuale del layout.

### AQ-049 — Aggiungere il controllo di visibilità del QR sul proiettore

- Stato: `PLANNED`
- Priorità: P1
- Dipendenze: AQ-048
- Obiettivo: permettere al presentatore di mostrare o nascondere il QR senza dover cambiare pagina.
- Criteri di accettazione:
  - la regia dispone di un comando esplicito mostra/nascondi QR;
  - il proiettore applica il comando alla partita corrente senza refresh manuale;
  - quando il QR è nascosto non rimane uno spazio vuoto o un elemento cliccabile;
  - il QR continua a puntare all'ingresso corretto della partita.
- Validazione: smoke test dei due stati e verifica su una seconda sessione proiettore.

### AQ-050 — Ridisegnare la vista proiettore e i risultati della domanda

- Stato: `PLANNED`
- Priorità: P1
- Dipendenze: AQ-049
- Obiettivo: rendere la vista proiettore pulita, leggibile durante la visualizzazione del qr e delle domande / risposte.
- Criteri di accettazione:
  - le risposte A/B/C/D hanno quattro colori basici, distinti e leggibili anche da lontano;
  - dopo la chiusura viene evidenziata visivamente l'opzione corretta, senza affidarsi alla sola dicitura `risposta corretta: B`;
  - durante la domanda non vengono mostrati conteggi o statistiche parziali;
  - dopo la chiusura sono mostrati conteggi di risposte corrette, errate e astensioni;
  - dopo la chiusura viene mostrata la squadra con la risposta valida più veloce; in caso di parità il risultato è esplicito;
  - il layout resta lineare, contrastato e leggibile nelle fasi lobby, domanda, soluzione e classifica.
- Validazione: test del view model per conteggi, astensioni, risposta più veloce e parità, più verifica visiva del proiettore.

### AQ-051 — Clonare una partita

- Stato: `PLANNED`
- Priorità: P1
- Dipendenze: AQ-050
- Obiettivo: permettere all'admin di creare una nuova partita partendo dalla configurazione di una partita di test o precedente, mantenendo manche e domande ma ricominciando con dati di serata puliti.
- Criteri di accettazione:
  - l'admin può avviare la clonazione da una partita esistente e assegnare un nuovo nome/titolo alla partita clonata;
  - vengono copiate le manche, le relative regole, l'ordine e le associazioni alle domande del catalogo;
  - le domande del catalogo vengono riutilizzate tramite riferimento, senza creare duplicati del catalogo;
  - la nuova partita riceve identificativi propri e non modifica la partita sorgente;
  - non vengono copiati squadre, password, sessioni, risposte, punteggi, classifiche, annullamenti o stato runtime della partita sorgente;
  - la partita clonata parte in stato di preparazione e non diventa attiva automaticamente: l'admin può selezionarla esplicitamente e far iscrivere nuove squadre;
  - un errore durante la clonazione non lascia una partita parzialmente creata.
- Validazione: test del servizio/database con una partita sorgente popolata e già giocata, verifica che la configurazione sia completa e che i dati di partecipazione risultino vuoti, più smoke test dalla dashboard.

### AQ-041 — Ripulire residui del prototipo e avvisi

- Stato: `PLANNED`
- Priorità: P2
- Dipendenze: AQ-040, AQ-044, AQ-045, AQ-046, AQ-047, AQ-048, AQ-049, AQ-050, AQ-051
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
