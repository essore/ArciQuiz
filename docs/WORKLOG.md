# Diario di lavoro

Registro append-only delle sessioni. Aggiungere le nuove voci in cima, senza riscrivere la cronologia.

Ogni voce deve indicare:

- data;
- task;
- risultato;
- file principali;
- verifiche realmente eseguite;
- rischi, assunzioni o blocchi.

---

## 2026-08-18 - Chiusura AQ-021 Registrare e amministrare le squadre

- Task: AQ-021.
- Risultato:
  - aggiunto `SquadreService` con registrazione pubblica esclusiva della lobby `Pronta`, controllo duplicati e inserimento admin anche durante la partita;
  - aggiunte la pagina pubblica `/registrazione`, QR nella dashboard per una partita pronta e pagina admin CRUD delle squadre, con password recuperabile e modificabile;
  - l'eliminazione è rifiutata se la squadra ha già risposte registrate, per non alterare retroattivamente i risultati;
  - aggiunti quattro test di integrazione SQLite; `AQ-021` dichiarato `DONE` e `AQ-022` promosso a `READY`.
- File principali: `Web/Services/SquadreService.cs`, `Web/Components/Pages/RegistrazioneSquadra.razor`, `Web/Components/Pages/SquadreAdmin.razor`, `Core.Tests/SquadreServiceTests.cs`, `docs/DECISIONS.md`, `docs/TODO.md`, `docs/PROJECT_STATE.md`, `docs/WORKLOG.md`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter "FullyQualifiedName~SquadreServiceTests"`: 4 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 26 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita con 0 errori e 3 avvisi;
  - smoke test mobile del proprietario: registrazione, duplicato, gestione admin e chiusura iscrizioni pubbliche verificati con esito positivo.
- Rischi residui: nessuno specifico per AQ-021.

---

## 2026-08-18 - AQ-021 Registrare e amministrare le squadre (bloccato)

- Task: AQ-021.
- Blocco: il requisito impone registrazione pubblica solo in lobby e chiusura all'avvio, ma `PartitaStato` non contiene `Lobby` né una mappatura approvata (`Nuova`, `Configurazione`, `Pronta`, `InCorso`, `Conclusa`). Serve decidere quale stato rappresenta la lobby e quale transizione la chiude, mantenendo l'inserimento tardivo admin disponibile.
- Verifiche: ispezionati modello persistente, stati partita, pagine amministrative e configurazione runtime; nessuna modifica funzionale applicata per non inventare una regola pubblica.

---

## 2026-08-18 - Chiusura AQ-020 Proteggere la dashboard admin

- Task: AQ-020.
- Risultato: il proprietario ha completato lo smoke test manuale con esito positivo: route amministrative anonime reindirizzate al login, login e logout funzionanti, proiettore pubblico accessibile senza autenticazione. `AQ-020` dichiarato `DONE`; `AQ-021` promosso a `READY`.
- Verifiche aggiuntive: controllo manuale del proprietario su `/admin`, `/login`, `/logout` e `/proiettore/1`.
- Rischi residui: nessuno specifico per AQ-020.

---

## 2026-08-18 - AQ-020 Proteggere la dashboard admin (bloccato)

- Task: AQ-020.
- Risultato parziale:
  - aggiunta autenticazione cookie locale, con credenziali in `appsettings.Local.json` ignorato da Git oppure nelle variabili d'ambiente;
  - protette le route e l'endpoint amministrativi; proiettore e futura registrazione rimangono pubblici;
  - aggiunti login e logout con protezione antiforgery e nove test sul perimetro delle route e sulle credenziali.
- File principali: `Web/Program.cs`, `Web/Services/AdminAccessService.cs`, `Web/Components/Routes.razor`, `Core.Tests/AdminAccessServiceTests.cs`, `.gitignore`, `Web/appsettings.Local.example.json`, `docs/TODO.md`, `docs/PROJECT_STATE.md`, `docs/WORKLOG.md`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter "FullyQualifiedName~AdminAccessServiceTests"`: 9 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 22 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita con 0 errori e 3 avvisi.
- Blocco:
  - l'avvio locale temporaneo necessario allo smoke test è stato rifiutato. Serve autorizzarlo per verificare `302` sulle route admin anonime, login, logout e accesso pubblico al proiettore; fino ad allora AQ-020 resta `BLOCKED`.

---

## 2026-08-18 - AQ-012 Importare ed esportare domande in CSV

- Task: AQ-012.
- Risultato:
  - aggiunto `DomandeCsvService` nel Core con formato CSV UTF-8 stabile, escaping di virgole/virgolette/ritorni a capo e categorie/difficoltà controllate;
  - l'importazione valida tutte le righe prima di salvare: in caso di errore non importa nulla e mostra in UI numero e motivo;
  - aggiunti download del catalogo CSV e caricamento CSV nella pagina domande;
  - documentato il formato in `docs/CSV_DOMANDE.md` e allineato il seed alle categorie previste;
  - `AQ-012` dichiarato `DONE`; `AQ-020` promosso a `READY`.
- File principali: `Core/Services/DomandeCsvService.cs`, `Core.Tests/DomandeCsvServiceTests.cs`, `Web/Components/Pages/DomandeAdmin.razor`, `Web/Program.cs`, `docs/CSV_DOMANDE.md`, `docs/TODO.md`, `docs/PROJECT_STATE.md`, `docs/WORKLOG.md`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter "FullyQualifiedName~DomandeCsvServiceTests"`: 2 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 13 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita con 0 errori e 3 avvisi;
  - smoke test in Development con database temporaneo separato: `GET /domande` e `GET /api/domande/export` hanno restituito `200`; export con intestazione UTF-8 verificata.
- Rischi residui:
  - vulnerabilità nota di gravità alta nella dipendenza transitiva `SQLitePCLRaw.lib.e_sqlite3` 2.1.11;
  - audit NuGet non raggiungibile nell'ambiente di verifica.

---

## 2026-08-18 - AQ-011 Validare la transizione della partita a Pronta

- Task: AQ-011.
- Risultato:
  - aggiunto `PartitaProntaValidator` nel Core, con messaggi di validazione in italiano per assenza di manche, domande mancanti o non valide e ordine duplicato;
  - la configurazione carica la partita completa e blocca soltanto la transizione effettiva a `PartitaStato.Pronta` se la validazione fallisce;
  - aggiunti cinque test unitari indipendenti dalla UI;
  - `AQ-011` dichiarato `DONE` e `AQ-012` promosso a `READY`.
- File principali: `Core/Services/PartitaProntaValidator.cs`, `Core.Tests/PartitaProntaValidatorTests.cs`, `Web/Components/Pages/PartitaConfig.razor`, `docs/TODO.md`, `docs/PROJECT_STATE.md`, `docs/WORKLOG.md`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter "FullyQualifiedName~PartitaProntaValidatorTests"`: 5 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 11 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita con 0 errori e 3 avvisi;
  - smoke test in Development con database SQLite temporaneo separato: migrazioni e seed applicati, `GET /partita/1` ha restituito `200` e la pagina di configurazione è stata caricata; processo e file temporanei rimossi al termine.
- Rischi residui:
  - vulnerabilità nota di gravità alta nella dipendenza transitiva `SQLitePCLRaw.lib.e_sqlite3` 2.1.11;
  - audit NuGet non raggiungibile nell'ambiente di verifica.

---

## 2026-08-18 - AQ-010 Completare il modello persistente della prima release

- Task: AQ-010.
- Risultato:
  - esteso il modello di dominio/EF Core in `Core/Entities` (`Partita`, `Manche`, `MancheDomanda`, `Domanda`, `Player`, `PlayerPartita`, `MancheRispostaRicevuta`);
  - configurati i vincoli univoci in `Infrasctructure/Data/ArciQuizDbContext.cs`:
    - nome squadra univoco per partita (`Player.PartitaId` + `Player.NomeSquadra`);
    - una sola risposta per squadra e occorrenza domanda (`MancheRispostaRicevuta.PlayerId` + `MancheRispostaRicevuta.MancheDomandaId`);
  - aggiunte proprietà per timer server-authoritative, regole manche (malus base 500 predefinito, moltiplicatore, astensioni gratuite), override domanda, tracciamento/audit annullamento e recupero stato;
  - generata la migrazione EF Core `AddPersistenceModelExtensions`;
  - ampliata la suite di test xUnit in `Core.Tests/DatabaseMigrationTests.cs` coprendo migrazione completa, vincoli univoci squadra/risposta e persistenza proprietà estese;
  - `AQ-010` dichiarato `DONE`;
  - promosso `AQ-011` (Validare la transizione della partita a Pronta) a `READY`.
- File principali: `Core/Entities/*.cs`, `Infrasctructure/Data/ArciQuizDbContext.cs`, `Infrasctructure/Migrations/*`, `Core.Tests/DatabaseMigrationTests.cs`, `docs/TODO.md`, `docs/PROJECT_STATE.md`, `docs/WORKLOG.md`.
- Verifiche:
  - `dotnet tool run dotnet-ef migrations add AddPersistenceModelExtensions --project Infrasctructure\Infrasctructure.csproj --startup-project Web\Web.csproj --no-build`: migrazione generata con successo;
  - `dotnet test ArciQuiz.slnx`: 6 test eseguiti e superati (100% verdi);
  - `dotnet build ArciQuiz.slnx`: compilazione completata con 0 errori;
  - `git status --short`: verificato diff e assenza di file non tracciati estranei.
- Rischi residui: nessuno specifico per AQ-010.

---

## 2026-08-18 - Chiusura AQ-005 e promozione AQ-010

- Task: AQ-005.
- Risultato:
  - rimossa l'inizializzazione del logger `EventLog` di Windows in `Web/Program.cs` per eliminare la dipendenza da privilegi elevati all'avvio/migrazione;
  - eseguito lo smoke test delle pagine CRUD (catalogo domande, nuova partita, manche, regia) con conferma di corretto funzionamento a runtime;
  - `AQ-005` dichiarato `DONE`;
  - promosso `AQ-010` (Completare il modello persistente della prima release) a `READY`.
- File principali: `Web/Program.cs`, `docs/TODO.md`, `docs/PROJECT_STATE.md`, `docs/WORKLOG.md`.
- Verifiche:
  - `dotnet test ArciQuiz.slnx`: riuscito, 2 test superati;
  - `dotnet build ArciQuiz.slnx`: riuscita, 0 errori;
  - smoke test runtime sulle pagine dell'Admin eseguito con successo.
- Rischi residui: nessuno specifico per AQ-005.

---

## 2026-07-11 - AQ-005 Rendere sicuro il ciclo di vita del DbContext

- Task: AQ-005.
- Risultato:
  - convertiti i sei componenti Blazor che iniettavano direttamente `ArciQuizDbContext` al pattern `IDbContextFactory<ArciQuizDbContext>` già usato dal proiettore;
  - caricamenti eseguiti con context brevi e `AsNoTracking`;
  - operazioni CRUD eseguite in context separati, ricaricando esplicitamente le entità modificate prima di salvarle;
  - preservati i flussi di catalogo domande, partite, manches, configurazione domande e regia;
  - AQ-005 impostato a `BLOCKED`: non è stato possibile completare lo smoke test runtime richiesto.
- File principali: `Web/Components/Pages/AdminHome.razor`, `DomandeAdmin.razor`, `NuovaPartita.razor`, `PartitaConfig.razor`, `MancheDomandeConfig.razor`, `RegiaPartita.razor`, `docs/TODO.md`, `docs/PROJECT_STATE.md`.
- Verifiche:
  - `dotnet test ArciQuiz.slnx -m:1`: riuscito, 2 test superati;
  - `dotnet build ArciQuiz.slnx -m:1`: riuscita, 0 errori;
  - `rg -n -g '*.razor' '@inject ArciQuizDbContext|\\bDb\\.' Web`: nessun context iniettato direttamente o accesso residuo al context precedente;
  - `dotnet run --project Web --no-build --urls http://127.0.0.1:5123`: non riuscito nel sandbox, perché il logger non può scrivere nel registro eventi Windows durante l'errore di migrazione;
  - tentativo di esecuzione autorizzata rifiutato dal proprietario.
- Blocco e rischio residuo:
  - eseguire lo smoke test delle pagine CRUD in un ambiente con permesso di scrittura nel registro eventi Windows; fino ad allora resta non verificato l'avvio end-to-end, pur con test e build verdi.

## 2026-07-11 - Coerenza solution e chiusura AQ-003

- Task: richiesta esplicita del proprietario successiva alla revisione tecnica.
- Risultato:
  - riconosciuto `ArciQuiz.slnx` come nome definitivo scelto dal proprietario;
  - aggiornati i comandi operativi in AGENTS, README e TODO;
  - rivalidati i criteri di AQ-003 e aggiornato lo stato a `DONE`;
  - aggiunto il tool manifest locale con `dotnet-ef` 10.0.0;
  - verificato che il modello EF Core è allineato alla migrazione corrente;
  - AQ-005 resta l'unico task `READY`.
- File principali: `.config/dotnet-tools.json`, `AGENTS.md`, `README.md`, `docs/TODO.md`, `docs/PROJECT_STATE.md`, `docs/WORKLOG.md`.
- Verifiche:
  - `dotnet tool restore`: riuscito;
  - `dotnet tool run dotnet-ef migrations has-pending-model-changes --project Infrasctructure\Infrasctructure.csproj --startup-project Web\Web.csproj --no-build`: nessuna modifica pendente al modello;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 -v minimal`: 2 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 -v minimal`: riuscita con 0 errori e 3 avvisi;
  - `git check-ignore -v`: WAL e SHM locali ignorati;
  - `git ls-files 'Web/*.db' 'Web/*.db-*'`: nessun artefatto SQLite tracciato.
- Rischi residui:
  - vulnerabilità nota di gravità alta nella dipendenza transitiva `SQLitePCLRaw.lib.e_sqlite3` 2.1.11;
  - audit NuGet non raggiungibile nell'ambiente sandbox;
  - due campi inutilizzati nella pagina regia segnalati durante la ricompilazione dei test.

## 2026-07-11 - AQ-004 Stabilire migrazioni e percorso dati

- Task: AQ-004, eseguito su priorità esplicita del proprietario nonostante AQ-003 bloccato.
- Risultato:
  - sostituito `EnsureCreated` con `Database.Migrate()` e generata la migrazione iniziale `InitialCreate`;
  - impostato il percorso runtime stabile `%LocalAppData%\ArciQuiz\arciquiz.db`;
  - allineati gli strumenti EF Core alla versione 10 e aggiunta la factory design-time;
  - limitato il seed demo al solo ambiente Development;
  - documentata la conservazione dei database del prototipo, non spostati automaticamente;
  - aggiunto il test di applicazione della migrazione su database temporaneo;
  - AQ-004 concluso e AQ-005 promosso a `READY`.
- File principali: `Web/Program.cs`, `Web/appsettings.json`, `Infrasctructure/Migrations/`, `Infrasctructure/Data/ArciQuizDbContextFactory.cs`, `Core.Tests/DatabaseMigrationTests.cs`, `README.md`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-restore -m:1 -v minimal`: 2 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 -v minimal`: riuscita con 0 errori e 3 avvisi;
  - generazione della migrazione con `dotnet-ef migrations add InitialCreate` riuscita.
  - avvio controllato in Production: migrazione applicata e database creato in `%LocalAppData%\ArciQuiz\arciquiz.db`.
- Rischi residui:
  - il passaggio dati dal database del prototipo alla nuova posizione non è automatizzato;
  - restano gli avvisi di audit NuGet non raggiungibile nell'ambiente e i due campi inutilizzati in regia.

## 2026-07-11 - AQ-003 Correggere la gestione degli artefatti SQLite

- Task: AQ-003.
- Risultato parziale:
  - aggiunte a `.gitignore` le varianti runtime SQLite (`.db`, journal, WAL e SHM);
  - rimossi dall'indice, senza cancellarli dal disco, `Web/arciquiz.db-shm` e `Web/arciquiz.db-wal`;
  - documentato in `README.md` e `docs/PROJECT_STATE.md` il percorso corrente basato su `AppContext.BaseDirectory`;
  - l'avvio controllato ha creato i file SQLite solo in posizioni ignorate dal repository.
- File principali: `.gitignore`, `README.md`, `docs/PROJECT_STATE.md`, `docs/TODO.md`, `Web/arciquiz.db-shm`, `Web/arciquiz.db-wal`.
- Verifiche:
  - `git check-ignore -v` conferma l'ignore per database, journal, WAL e SHM;
  - `git status --short` mostra esclusivamente la rimozione dall'indice dei due artefatti SQLite, preservati nel worktree;
  - `dotnet test Web.slnx`, `dotnet build Web.slnx`, le varianti `--no-restore` e `dotnet restore Web.slnx --disable-parallel`: non riusciti durante la risoluzione degli asset, senza errori MSBuild;
  - `dotnet run --project Web/Web.csproj --no-build --no-launch-profile --urls http://127.0.0.1:5055`: eseguito; gli artefatti SQLite risultanti sono ignorati.
- Blocco:
  - la build essenziale non è attualmente verificabile nell'ambiente; AQ-003 resta `BLOCKED` finché restore/build non tornano a produrre un esito diagnostico e verificabile.

## 2026-07-10 - AQ-002 Creare la baseline di test

- Task: AQ-002.
- Risultato:
  - aggiunto il progetto xUnit `Core.Tests` alla soluzione;
  - aggiunto un test sul versionamento monotono di `GameStateService`;
  - aggiornati i comandi canonici in `README.md` e `AGENTS.md`;
  - AQ-002 concluso e AQ-003 promosso a `READY`.
- File principali: `Core.Tests/Core.Tests.csproj`, `Core.Tests/GameStateServiceTests.cs`, `Web.slnx`, `README.md`, `AGENTS.md`, `docs/TODO.md`, `docs/PROJECT_STATE.md`.
- Verifiche:
  - `dotnet test Web.slnx`: riuscito;
  - `dotnet vstest Core.Tests/bin/Debug/net10.0/Core.Tests.dll --Logger:"console;verbosity=detailed"`: 1 test superato;
  - `dotnet build Web.slnx --no-restore`: riuscita con 0 errori e 12 avvisi.
- Rischi residui:
  - restano gli avvisi sulle dipendenze vulnerabili e sull'header di autorizzazione;
  - la suite iniziale copre solo il versionamento dello stato runtime e va estesa nei task futuri.

## 2026-07-10 - AQ-001 Ripristinare la compilazione

- Task: AQ-001.
- Risultato:
  - aggiornati i due costruttori di `GameState` al contratto corrente;
  - impostati valori prototipali espliciti: nessun timer o messaggio pubblico e risposte non accettate;
  - AQ-001 concluso e AQ-002 promosso a `READY`.
- File principali: `Web/Services/GameStateService cs.cs`, `Web/Components/Pages/RegiaPartita.razor`, `docs/TODO.md`, `docs/PROJECT_STATE.md`.
- Verifiche:
  - `dotnet build Web.slnx --no-restore`: riuscita con 0 errori e 13 avvisi preesistenti.
- Rischi residui:
  - gli avvisi sulle dipendenze vulnerabili, sull'header di autorizzazione e sui campi inutilizzati restano fuori dall'ambito di AQ-001;
  - timer e flusso risposte saranno implementati in task successivi.

## 2026-07-10 — Audit e bootstrap documentale

- Task: analisi iniziale e preparazione del lavoro autonomo.
- Risultato:
  - ispezionati struttura, cronologia Git, progetti, entità, DbContext, servizi, pagine Razor, configurazione e asset proprietari;
  - raccolte dal proprietario le regole della serata, dei ruoli, delle risposte e del punteggio;
  - creata la documentazione separata per prodotto, architettura, stato, decisioni e backlog;
  - definito il protocollo a un solo task per agente.
- File principali: `AGENTS.md`, `README.md`, `docs/*.md`.
- Verifiche:
  - `git status --short` prima delle modifiche: pulito;
  - `dotnet build Web.slnx --no-restore`: fallita con 2 errori e 13 avvisi;
  - confermata assenza di Markdown nel repository e nella cronologia precedente.
- Rischi residui:
  - build non verde, tracciata in `AQ-001`;
  - nessuna suite test;
  - regole implementate ancora molto distanti dalla specifica.
