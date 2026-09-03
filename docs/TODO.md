# Piano di lavoro

## Protocollo della coda

La coda è ordinata. Un agente autonomo prende esclusivamente il primo task `READY`, completa un solo task e segue `AGENTS.md`.

Deve esserci al massimo un task `READY`. Quando viene concluso, l'agente promuove a `READY` il primo task `PLANNED` con dipendenze soddisfatte.

Stati ammessi: `PLANNED`, `READY`, `IN_PROGRESS`, `BLOCKED`, `DONE`.

`TestUmano` non è uno stato: una voce `TestUmano: DA ESEGUIRE` documenta una prova manuale mirata non bloccante, con passi, risultato atteso, contesto e rischio residuo. Le verifiche che condizionano funzionalità, sicurezza, dati o i task successivi richiedono invece lo stato `BLOCKED`.

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

- Stato: `DONE`
- Priorità: P1
- Dipendenze: AQ-047
- Obiettivo: rendere la regia leggibile e coerente con la partita selezionata, riducendo i comandi ambigui.
- Criteri di accettazione:
  - il proiettore è apribile con un'azione evidente in alto a destra;
  - la regia mostra soltanto le operazioni consentite dalla fase corrente, con una sola azione primaria chiaramente riconoscibile;
  - i bottoni usano icone standard accompagnate da testo e da un'etichetta accessibile;
  - il selettore manuale della manche viene rimosso: manche e domanda correnti derivano dallo stato persistito della partita;
  - aprire la regia rende attiva la partita selezionata; se esiste un'altra partita attiva, il cambio richiede una conferma esplicita;
  - la regia si aggiorna quando il timer o un altro circuito cambia la fase, senza richiedere refresh o un comando fallito;
  - stato corrente, countdown e azione primaria restano visibili sul monitor di un portatile senza scorrimento nel flusso ordinario;
  - le operazioni distruttive, come l'annullamento di una domanda, restano separate dai comandi di avanzamento.
- Validazione: test della selezione/attivazione completati (3 casi), suite completa (103 casi) e build completate.
- TestUmano: DA ESEGUIRE — Avviare l'app in un browser locale disponibile, aprire la regia di una partita con timer breve e attendere la scadenza senza interagire; verificare il passaggio automatico di fase senza refresh e la visibilità di stato, countdown e azione primaria a 1366×768. Rischio residuo: il browser dell'ambiente agente ha negato l'accesso all'URL locale.

### AQ-049 — Aggiungere il controllo di visibilità del QR sul proiettore

- Stato: `DONE`
- Priorità: P1
- Dipendenze: AQ-048
- Obiettivo: permettere al presentatore di mostrare o nascondere il QR senza dover cambiare pagina.
- Criteri di accettazione:
  - la regia dispone di un comando esplicito mostra/nascondi QR;
  - il proiettore applica il comando alla partita corrente senza refresh manuale;
  - quando il QR è nascosto non rimane uno spazio vuoto o un elemento cliccabile;
  - il QR continua a puntare all'ingresso corretto della partita.
- Validazione: test del contratto runtime QR, suite completa (104 casi) e build completati.
- TestUmano: DA ESEGUIRE — Avviare l'app con un database SQLite scrivibile, aprire la regia e due sessioni del proiettore della stessa partita. Premere `Nascondi QR` e verificare che entrambi i proiettori rimuovano il QR senza refresh, spazio vuoto o elemento cliccabile; premere `Mostra QR` e verificare che ricompaia in entrambe le sessioni con destinazione `/gioca`. Rischio residuo: l'ambiente agente non può avviare l'app perché il database locale è in sola lettura.

### AQ-058 — Ripristinare l'avvio della manche successiva dalla regia

- Stato: `DONE`
- Priorità: P0
- Dipendenze: AQ-049
- Obiettivo: consentire alla regia di avviare la manche successiva dopo la conclusione di quella corrente, anche per partite create con ordini manche duplicati dal comportamento precedente.
- Criteri di accettazione:
  - dopo la conclusione di una manche non finale, la regia mostra l'azione `Avvia manche successiva`;
  - l'azione avvia la prima domanda valida della manche determinata dall'ordinamento condiviso tra motore e UI;
  - la rilevazione della manche successiva resta corretta quando più manche esistenti hanno lo stesso valore `Ordine`;
  - le nuove partite assegnano alle manche un ordine progressivo stabile e l'aggiunta di una manche usa l'ordine successivo disponibile;
  - dopo l'avvio vengono aggiornati manche corrente, domanda corrente, fase, timer, regia, proiettore e client squadra;
  - alla conclusione dell'ultima manche viene proposta la conclusione della partita e non l'avvio di una manche inesistente;
  - refresh e riavvio dell'app ricostruiscono correttamente la manche corrente.
- Validazione: test di regressione con due manche con `Ordine` duplicato e una terza con `Ordine` distinto, recupero su nuovo contesto, conclusione dell'ultima manche, suite completa (105 casi) e build completati.
- TestUmano: DA ESEGUIRE — Con un `appsettings.Local.json` amministrativo e un database SQLite scrivibile, creare una partita con almeno due manche, concludere la prima dalla regia e verificare che appaia `Avvia manche successiva`; premerlo e verificare su regia, proiettore e client squadra la prima domanda della seconda manche senza refresh. Ripetere con due manche aventi lo stesso ordine e verificare che l'ultima mostri solo `Concludi partita`. Rischio residuo: l'ambiente agente non può avviare l'app sul database SQLite locale in sola lettura.

### AQ-050 — Ridisegnare la vista proiettore e i risultati della domanda

- Stato: `DONE`
- Priorità: P1
- Dipendenze: AQ-058
- Obiettivo: rendere la vista proiettore pulita, leggibile durante la visualizzazione del qr e delle domande / risposte.
- Criteri di accettazione:
  - le risposte A/B/C/D hanno quattro colori basici, distinti e leggibili anche da lontano;
  - dopo la chiusura viene evidenziata visivamente l'opzione corretta, senza affidarsi alla sola dicitura `risposta corretta: B`;
  - durante la domanda non vengono mostrati conteggi o statistiche parziali;
  - dopo la chiusura sono mostrati conteggi di risposte corrette, errate e astensioni;
  - dopo la chiusura viene mostrata la squadra con la risposta valida più veloce; in caso di parità il risultato è esplicito;
  - il layout resta lineare, contrastato e leggibile nelle fasi lobby, domanda, soluzione e classifica;
  - nessuna informazione tecnica o in inglese, come gli enum di fase, viene mostrata al pubblico;
  - le informazioni essenziali restano visibili senza scorrimento e senza dipendere dalla sola percezione del colore;
  - il layout si adatta a proiettori e TV 4:3 o 16:9 senza assumere una risoluzione o un rapporto d'aspetto specifici.
- Validazione: test del view model per conteggi, astensioni, risposta più veloce e parità, suite completa (107 casi) e build completati.
- TestUmano: DA ESEGUIRE — Avviare l'app con il database della serata, aprire il proiettore in 800×600, 1024×768, 1280×720, 1920×1080 e 3840×2160. Verificare nelle fasi lobby, domanda, soluzione e classifica che titolo, QR o domanda, quattro opzioni e risultati essenziali siano leggibili senza scorrimento; nella soluzione verificare ✓ verde con contorno e glow oro sulla risposta corretta, ✕ rossa e sfondo grigio disabilitato sulle altre opzioni, conteggi corretti/errati/astensioni e il messaggio di parità della risposta valida più veloce. Durante la domanda verificare che l'icona timer, i secondi e la barra al 90% della pagina compaiano tra domanda e risposte, diminuendo fluidamente dal 100% fino a zero al passaggio da `2 s` a `1 s`; la risposta deve restare accettabile fino allo zero. Dopo la scadenza verificare che timer e barra restino nello stesso punto con `0 s`, icona timeout e stile grigio. Rischio residuo: il browser locale ha negato l'apertura dell'URL dell'app durante la verifica agente.

### AQ-051 — Clonare una partita

- Stato: `DONE`
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
- Validazione: test del servizio/database con una partita sorgente popolata e già giocata, rollback forzato su errore di salvataggio, suite completa (109 casi) e build completati.
- TestUmano: DA ESEGUIRE — Accedere alla dashboard, scegliere `Clona` su una partita già giocata, inserire un titolo e confermare. Verificare il redirect alla configurazione della nuova partita, la presenza di manche/domande/regole della sorgente e l'assenza di squadre, risposte, punteggi e stato attivo; controllare che la sorgente non cambi. Rischio residuo: lo smoke browser locale non è eseguibile nell'ambiente di automazione, mentre il percorso dati è coperto dai test d'integrazione.

### AQ-060 — Ridisegnare la pagina giocatore per smartphone

- Stato: `DONE`
- Priorità: P1
- Dipendenze: AQ-051
- Obiettivo: rendere la pagina giocatore mobile coerente con la vista proiettore, mantenendo invariati timer, accettazione delle risposte e regole di gioco autorevoli sul server.
- Criteri di accettazione:
  - domanda, opzioni A/B/C/D, colori, bordi, icone e spaziature riprendono il linguaggio visivo del proiettore e restano leggibili su smartphone senza scorrimento orizzontale;
  - durante la domanda il countdown mostra l'icona, i secondi e una barra fluida coerente con il proiettore, senza modificare la scadenza effettiva o l'accettazione server-authoritative della risposta;
  - dopo la conferma, l'opzione scelta dalla squadra è riconoscibile con un glow blu di selezione, distinto dagli stati di esito;
  - alla chiusura, la risposta corretta mostra spunta verde e contorno/glow oro; le risposte errate mostrano una X rossa e uno stato grigio disabilitato;
  - se la squadra ha selezionato una risposta errata, rimangono visibili contemporaneamente il glow blu della scelta della squadra e il glow verde/oro della risposta corretta; se ha scelto quella corretta, prevale lo stato di risposta corretta;
  - in caso di astensione non viene mostrato alcun glow di selezione e resta evidenziata soltanto la risposta corretta;
  - la soluzione e gli esiti personali non vengono mai inclusi nella vista o nel payload prima della chiusura della domanda;
  - non vengono introdotte dipendenze da Internet o nuove librerie UI.
- Validazione: test del view model per domanda aperta, countdown con durata standard e override, risposta confermata, risposta corretta, risposta errata con doppia evidenza e astensione; suite completa (110 casi) e build completati.
- TestUmano: DA ESEGUIRE — Su uno smartphone o browser a 360 px, accedere a una squadra e provare domanda aperta, risposta confermata, soluzione con risposta corretta, errata e astenuta. Verificare timer a icona/barra, nessuno scorrimento orizzontale, glow blu della scelta errata contemporaneo a verde/oro della soluzione e assenza di soluzione prima della chiusura. Rischio residuo: lo smoke dell'istanza locale non è eseguibile nell'automazione perché SQLite non può creare il database in `%LocalAppData%`; stati e payload sono coperti dai test del view model.

### AQ-041 — Ripulire residui del prototipo e avvisi

- Stato: `BLOCKED`
- Priorità: P2
- Dipendenze: AQ-040, AQ-044, AQ-045, AQ-046, AQ-047, AQ-048, AQ-049, AQ-050, AQ-051, AQ-060
- Obiettivo: rimuovere pagine demo, codice commentato, campi inutilizzati e avvisi non giustificati senza rifattorizzazioni architetturali.
- Criteri di accettazione:
  - nessuna pagina template raggiungibile;
  - nessun warning del compilatore nel codice proprietario;
  - dipendenze vulnerabili o incoerenti risolte con versioni compatibili;
  - rinomina dei refusi valutata separatamente per impatto.
- Validazione: build, test e controllo navigazione.
- Blocco: l'ambiente non può raggiungere `https://api.nuget.org/v3/index.json` (connessione deviata a `127.0.0.1:9`), quindi non è possibile ripristinare e verificare l'aggiornamento EF Core 10.0.11 che risolve le dipendenze vulnerabili. Ripristinare l'accesso a NuGet e aggiornare tutti i riferimenti EF Core alla stessa patch 10.0.11, quindi rieseguire restore, test e build.

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

## Milestone M5 — Esperienza d'uso e manutenzione dei dati

### AQ-052 — Completare la configurazione delle regole di manche

- Stato: `PLANNED`
- Priorità: P0
- Dipendenze: AQ-043
- Obiettivo: rendere configurabili e coerenti con il motore di punteggio tutte le regole previste per una manche.
- Criteri di accettazione:
  - la configurazione espone punti base, malus base, moltiplicatore della manche, durata standard e numero massimo di astensioni gratuite;
  - il malus base predefinito è 500 e le nuove manche non disattivano implicitamente la penalità per errore;
  - la UI non espone stati tecnici o opzioni che il server rifiuta successivamente;
  - i valori esistenti vengono caricati e salvati senza perdita;
  - la validazione usa messaggi italiani e impedisce valori non ammessi prima del salvataggio.
- Validazione: test dei valori predefiniti e del round-trip di tutte le regole, suite completa, build e smoke test della configurazione.

### AQ-053 — Guidare la preparazione della partita

- Stato: `PLANNED`
- Priorità: P1
- Dipendenze: AQ-052
- Obiettivo: trasformare la preparazione in un percorso comprensibile da dati iniziali a lobby pronta, senza richiedere la conoscenza degli stati interni.
- Criteri di accettazione:
  - il percorso mostra chiaramente i passaggi dati partita, manche, domande, squadre e verifica finale;
  - la creazione di una partita porta direttamente alla sua configurazione;
  - lo stato `Pronta` viene raggiunto tramite un'azione esplicita accompagnata da una checklist dei requisiti mancanti;
  - gli stati tecnici non sono modificabili tramite selettori generici;
  - l'utente può tornare ai passaggi precedenti senza perdere modifiche già salvate;
  - dashboard e navigazione distinguono preparazione, conduzione della serata e storico.
- Validazione: test delle transizioni esposte dalla UI e smoke test completo dalla creazione alla lobby.

### AQ-054 — Definire un'identità visiva coerente e responsive

- Stato: `PLANNED`
- Priorità: P1
- Dipendenze: AQ-053
- Obiettivo: sostituire l'aspetto da template con un sistema visivo caldo e riconoscibile, adatto a un'associazione culturale e coerente tra amministrazione, regia e client squadra.
- Criteri di accettazione:
  - colori, tipografia, spaziature, bordi, stati e azioni usano variabili e componenti condivisi senza introdurre una nuova libreria UI;
  - azioni primarie, secondarie e distruttive sono distinguibili anche quando disabilitate;
  - form, tabelle, messaggi e stati vuoti hanno una gerarchia visiva uniforme;
  - l'area amministrativa resta utilizzabile sul monitor di un portatile e degrada correttamente a larghezze ridotte;
  - focus, contrasto e significato delle azioni non dipendono soltanto dal colore;
  - la personalizzazione non modifica i comportamenti funzionali già verificati.
- Validazione: controllo accessibilità di base, smoke test delle pagine principali e confronto visivo a larghezze desktop e ridotte.

### AQ-055 — Archiviare e ripristinare il catalogo domande

- Stato: `PLANNED`
- Priorità: P1
- Dipendenze: AQ-054
- Obiettivo: gestire cataloghi di alcune centinaia di domande e consentire la sostituzione di un set importato senza rompere partite o storico.
- Criteri di accettazione:
  - l'eliminazione singola viene presentata come archiviazione e continua a usare il soft delete;
  - un comando esplicito consente di archiviare in blocco le domande attualmente disponibili;
  - le domande usate da partite non concluse vengono protette e il riepilogo indica quante non sono state archiviate;
  - le domande usate soltanto da partite concluse possono essere archiviate senza alterare storico, risultati o visualizzazione delle partite passate;
  - le domande archiviate non compaiono nel catalogo ordinario né tra quelle associabili a nuove manche;
  - un filtro dedicato permette di vedere e ripristinare le domande archiviate;
  - filtri e operazioni restano adeguati a cataloghi di alcune centinaia di elementi.
- Validazione: test di archiviazione singola, massiva, protezione delle partite non concluse, ripristino e integrità dello storico; smoke test dopo importazione CSV.

### AQ-056 — Creare backup SQLite verificabili

- Stato: `PLANNED`
- Priorità: P0
- Dipendenze: AQ-055
- Obiettivo: creare una copia consistente e comprensibile del database prima delle operazioni distruttive e permettere all'utente di individuarla.
- Criteri di accettazione:
  - il backup usa una modalità compatibile con SQLite aperto e include tutte le transazioni concluse;
  - il file riceve un nome con data e ora in una cartella locale documentata;
  - il backup viene aperto e verificato prima di dichiarare l'operazione riuscita;
  - un errore di backup impedisce l'avvio del reset;
  - la UI mostra percorso, esito e istruzioni essenziali di conservazione senza esporre dettagli tecnici inutili.
- Validazione: test su database popolato, verifica di apertura e conteggi della copia, simulazione di errore e build completa.

### AQ-057 — Aggiungere pulizia dello storico e reset completo

- Stato: `PLANNED`
- Priorità: P1
- Dipendenze: AQ-056
- Obiettivo: permettere all'amministratore di svuotare i dati operativi in modo intenzionale, atomico e recuperabile.
- Criteri di accettazione:
  - sono disponibili due operazioni distinte: eliminare serate e risultati conservando il catalogo, oppure eseguire il reset completo di tutti i dati applicativi;
  - entrambe mostrano prima i conteggi di partite, manche, squadre, risposte, statistiche e domande interessate;
  - nessuna operazione è consentita mentre una partita è in corso;
  - il reset completo richiede la digitazione di `RESET ARCIQUIZ` e un backup automatico riuscito;
  - partite, manche, squadre, sessioni, risposte, punteggi e statistiche vengono rimossi rispettando le relazioni; nel reset completo vengono rimosse anche le domande;
  - schema, migrazioni e credenziale amministrativa locale restano disponibili;
  - stato runtime e sessioni squadra vengono invalidati senza richiedere il riavvio dell'app;
  - un errore non lascia un ambiente parzialmente cancellato.
- Validazione: test transazionali dei due livelli di pulizia, vincoli FK, blocco durante la partita, backup obbligatorio, rollback su errore e smoke test di ripartenza da ambiente vuoto.

### AQ-059 — Aggiungere domande casuali filtrate alla manche

- Stato: `PLANNED`
- Priorità: P1
- Dipendenze: AQ-057
- Obiettivo: velocizzare la composizione di una manche permettendo di aggiungere in una sola operazione un numero configurabile di domande scelte casualmente tra quelle disponibili dopo il filtro.
- Criteri di accettazione:
  - la sezione `Aggiungi domande` espone filtri distinti per categoria e difficoltà, oltre all'eventuale ricerca testuale;
  - ogni filtro consente di selezionare tutti i valori oppure un valore tra quelli ammessi dal catalogo;
  - il numero di domande disponibili viene aggiornato immediatamente quando cambia un filtro ed esclude domande archiviate, non valide o già associate a una manche della stessa partita;
  - un campo numerico consente di scegliere una quantità compresa tra 1 e il numero di domande disponibili dopo il filtro;
  - il comando `Aggiungi domande casuali` seleziona la quantità richiesta senza ripetizioni e associa tutte le domande alla manche con indici consecutivi in coda a quelle esistenti;
  - quando non esistono domande disponibili, quantità e comando risultano disabilitati e viene mostrato un messaggio esplicito;
  - se la quantità non è valida o le disponibilità cambiano prima del salvataggio, nessuna associazione parziale viene lasciata nel database e l'utente riceve un messaggio comprensibile;
  - resta disponibile l'aggiunta manuale della singola domanda dai risultati filtrati.
- Validazione: test dei filtri combinati, dei limiti 1 e numero massimo disponibile, dell'estrazione senza duplicati, dell'ordine assegnato, dell'atomicità in caso di conflitto e smoke test della pagina con catalogo popolato.

## Decisioni ancora non trasformate in task

Le decisioni correnti sono rappresentate nei task della coda. Eventuali nuove esigenze vanno registrate in `DECISIONS.md` e poi trasformate in task atomici senza interrompere l'ordine corrente, salvo priorità esplicita del proprietario.
