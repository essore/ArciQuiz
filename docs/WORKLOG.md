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
