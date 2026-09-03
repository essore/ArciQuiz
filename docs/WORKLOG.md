# Diario di lavoro

Registro append-only delle sessioni. Aggiungere le nuove voci in cima, senza riscrivere la cronologia.

Ogni voce deve indicare:

- data;
- task;
- risultato;
- file principali;
- verifiche realmente eseguite;
- rischi, assunzioni o blocchi.

## 2026-09-02 - AQ-041 Ripulire residui del prototipo e avvisi

- Task: AQ-041.
- Risultato:
  - rimosse le route demo `Counter` e `Weather`, i blocchi commentati del template e i riferimenti assoluti alla cache NuGet dal progetto web;
  - localizzati in italiano le pagine di errore e non trovata, il messaggio di errore Blazor e il dialogo di riconnessione;
  - valutata separatamente la rinomina dei refusi: resta esclusa perché coinvolge il nome del progetto `Infrasctructure` e supererebbe il perimetro della pulizia;
  - AQ-041 impostato a `BLOCKED` per l'impossibilità di ripristinare le patch di sicurezza delle dipendenze.
- File principali: `Web/Components/App.razor`, `Web/Components/Layout/MainLayout.razor`, `Web/Components/Layout/MinimalLayout.razor`, `Web/Components/Layout/ReconnectModal.razor`, `Web/Components/Pages/Error.razor`, `Web/Components/Pages/NotFound.razor`, `Web/Components/Pages/Counter.razor`, `Web/Components/Pages/Weather.razor`, `Web/Program.cs`, `Web/Web.csproj`, `Core/Core.csproj`, `docs/TODO.md`, `docs/PROJECT_STATE.md`.
- Verifiche:
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 110 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita, 0 errori, nessun warning del compilatore proprietario; restano avvisi NuGet per vulnerabilità e audit non disponibile;
  - controllo route con `rg`: nessuna route o testo delle pagine demo residua;
  - `git diff --check`: riuscito.
- Rischi, assunzioni o blocchi:
  - `dotnet restore Infrasctructure\\Infrasctructure.csproj --ignore-failed-sources --disable-parallel` ha confermato che NuGet è irraggiungibile (connessione a `127.0.0.1:9`); la patch EF Core 10.0.11, compatibile con net10.0 e dipendente da SQLitePCLRaw ≥2.1.12, non può quindi essere scaricata né verificata. Ripristinare l'accesso al feed e completare AQ-041 prima di promuovere AQ-042.

## 2026-09-02 - AQ-060 Ridisegnare la pagina giocatore per smartphone

- Task: AQ-060.
- Risultato:
  - ridisegnate domanda e opzioni della squadra con colori, bordi, icone e spaziature coerenti con il proiettore;
  - sostituito il countdown testuale con il componente locale a icona, secondi e barra fluida, passando al solo rendering la durata già configurata senza modificare il timer autorevole;
  - dopo la conferma la scelta ha un glow blu; alla soluzione la risposta corretta ha ✓ verde e contorno/glow oro, mentre le errate mostrano ✕ rossa e stato disabilitato;
  - per una risposta errata restano visibili il glow blu della scelta e l'evidenza verde/oro della corretta; per astensione non è presente alcun glow blu;
  - AQ-060 concluso e AQ-041 promosso a `READY`.
- File principali: `Web/Components/Pages/SquadraSessione.razor`, `Web/Components/Pages/SquadraSessione.razor.css`, `Web/Services/PlayerGameViewService.cs`, `Core.Tests/PlayerGameViewServiceTests.cs`, `docs/TODO.md`, `docs/PROJECT_STATE.md`.
- Verifiche:
  - `dotnet test Core.Tests\\Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter FullyQualifiedName~PlayerGameViewServiceTests`: 15 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 110 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita, 0 errori e 11 avvisi NuGet noti;
  - `git diff --check`: riuscito.
- Rischi, assunzioni o blocchi:
  - il collaudo visivo a larghezza smartphone resta non bloccante e registrato in AQ-060: l'istanza locale non è avviabile nella sandbox perché SQLite non può creare il database in `%LocalAppData%`; il payload continua a esporre la soluzione solo nella fase `ShowingAnswers`.

## 2026-09-02 - AQ-051 Clonare una partita

- Task: AQ-051.
- Risultato:
  - aggiunto il comando `Clona` nella dashboard e una pagina protetta per assegnare il titolo alla nuova partita;
  - introdotta la clonazione atomica di manche, regole, ordine e riferimenti al catalogo, con stato iniziale non attivo e senza dati runtime;
  - coperti con test una sorgente già giocata, l'assenza di dati di partecipazione nella copia e il rollback forzato in caso di errore;
  - AQ-051 concluso e AQ-060 promosso a `READY`.
- File principali: `Web/Services/PartitaCloneService.cs`, `Web/Components/Pages/ClonaPartita.razor`, `Web/Components/Pages/AdminHome.razor`, `Core.Tests/PartitaCloneServiceTests.cs`, `docs/TODO.md`, `docs/PROJECT_STATE.md`.
- Verifiche:
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false --filter FullyQualifiedName~PartitaCloneServiceTests`: 2 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 109 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita, 0 errori e 11 avvisi NuGet noti.
- Rischi, assunzioni o blocchi:
  - lo smoke browser della dashboard resta un collaudo manuale non bloccante, documentato in AQ-051; la clonazione dei dati è verificata su SQLite temporaneo.

## 2026-09-02 - Pianificazione AQ-060 restyling giocatore smartphone

- Task: AQ-060, aggiunto su richiesta del proprietario.
- Risultato:
  - pianificato il restyling della pagina giocatore mobile, coerente con proiettore, slider e stati di risposta;
  - la selezione squadra riceverà un glow blu distinto dalla risposta corretta verde/oro e dalle errate disabilitate;
  - AQ-041 dipende ora anche da AQ-060, così il cleanup resta successivo al nuovo intervento UI.
- File principali: `docs/TODO.md`, `docs/WORKLOG.md`.
- Verifiche:
  - controllata la coerenza di posizione, dipendenze e stato della coda.
- Rischi, assunzioni o blocchi:
  - nessuna modifica funzionale applicata; implementazione e collaudo mobile sono demandati ad AQ-060.

## 2026-09-02 - AQ-050 Barra anticipata di un secondo

- Task: AQ-050, rifinitura richiesta dal proprietario.
- Risultato:
  - la barra CSS usa una durata visiva di un secondo inferiore rispetto al timer autorevole;
  - con `1 s` ancora mostrato, la barra è già a zero; il server continua ad accettare risposte fino alla scadenza effettiva;
  - la soglia rossa resta calcolata sul tempo autorevole, non sulla durata visiva abbreviata.
- File principali: `Web/Components/ProjectorCountdown.razor`, `docs/TODO.md`.
- Verifiche:
  - `dotnet test ArciQuiz.slnx --no-restore -c Release -m:1 /p:UseSharedCompilation=false`: 107 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -c Release -m:1 /p:UseSharedCompilation=false`: riuscita, 0 errori e 11 avvisi NuGet noti;
  - `git diff --check`: riuscito.
- Rischi, assunzioni o blocchi:
  - il collaudo visivo multi-risoluzione resta non bloccante e registrato in AQ-050.

## 2026-09-02 - AQ-050 Timer persistente nella soluzione

- Task: AQ-050, rifinitura richiesta dal proprietario.
- Risultato:
  - mantenuto il timer nella stessa posizione tra domanda e risposte durante la soluzione;
  - a tempo scaduto mostra `0 s`, icona timeout e barra/stile grigi, evitando lo spostamento delle opzioni.
- File principali: `Web/Components/ProjectorCountdown.razor`, `Web/Components/ProjectorCountdown.razor.css`, `Web/Components/Pages/Proiettore/ProiettorePartita.razor`, `docs/TODO.md`, `docs/PROJECT_STATE.md`.
- Verifiche:
  - `dotnet test ArciQuiz.slnx --no-restore -c Release -m:1 /p:UseSharedCompilation=false`: 107 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -c Release -m:1 /p:UseSharedCompilation=false`: riuscita, 0 errori e 11 avvisi NuGet noti;
  - `git diff --check`: riuscito.
- Rischi, assunzioni o blocchi:
  - il collaudo visivo multi-risoluzione resta non bloccante e registrato in AQ-050.

## 2026-09-02 - AQ-050 Timer critico al 20%

- Task: AQ-050, rifinitura richiesta dal proprietario.
- Risultato:
  - confermata nel sorgente la posizione del timer tra domanda e opzioni, come nel mockup;
  - sotto il 20% residuo barra, icona e secondi diventano rossi.
- File principali: `Web/Components/ProjectorCountdown.razor`, `Web/Components/ProjectorCountdown.razor.css`, `docs/TODO.md`.
- Verifiche:
  - `dotnet test ArciQuiz.slnx --no-restore -c Release -m:1 /p:UseSharedCompilation=false`: 107 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -c Release -m:1 /p:UseSharedCompilation=false`: riuscita, 0 errori e 11 avvisi NuGet noti;
  - `git diff --check`: riuscito.
- Rischi, assunzioni o blocchi:
  - la soglia è strettamente inferiore al 20%, come richiesto; il collaudo visivo multi-risoluzione resta non bloccante e registrato in AQ-050.

## 2026-09-02 - AQ-050 Enfasi risposta corretta e timer fluido

- Task: AQ-050, rifinitura richiesta dal proprietario.
- Risultato:
  - aggiunti contorno oro e glow animato alla risposta corretta, conservando il ✓ verde;
  - spostato il timer tra domanda e opzioni, al 90% della larghezza disponibile;
  - aggiunta icona SVG locale per mantenere il funzionamento offline;
  - resa continua la barra con interpolazione CSS di un secondo, senza polling o elaborazione server aggiuntivi.
- File principali: `Web/Components/Pages/Proiettore/ProiettorePartita.razor`, `Web/Components/Pages/Proiettore/ProiettorePartita.razor.css`, `Web/Components/ProjectorCountdown.razor`, `Web/Components/ProjectorCountdown.razor.css`, `docs/TODO.md`, `docs/PROJECT_STATE.md`.
- Verifiche:
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 107 test superati;
  - build Debug: compilazione dei progetti completata durante i test, ma copia finale di `Web.exe` bloccata da un processo esterno che usa il file;
  - `dotnet build ArciQuiz.slnx --no-restore -c Release -m:1 /p:UseSharedCompilation=false`: riuscita, 0 errori e 11 avvisi NuGet noti;
  - `git diff --check`: riuscito.
- Rischi, assunzioni o blocchi:
  - nessun caricamento esterno di icone: l'SVG è nel componente per rispettare il funzionamento LAN senza Internet;
  - il collaudo visivo multi-risoluzione resta non bloccante e registrato in AQ-050.

## 2026-09-02 - AQ-050 Icone di esito e timer a barra

- Task: AQ-050, rifinitura richiesta dal proprietario.
- Risultato:
  - sostituito il badge testuale della soluzione con ✓ verde nella risposta corretta;
  - aggiunte ✕ rosse circolari e sfondo grigio disabilitato alle risposte errate;
  - aggiunto `ProjectorCountdown`, con secondi e barra che diminuisce dal 100% allo zero;
  - icone e stato disabilitato restano esclusivi della fase soluzione.
- File principali: `Web/Components/Pages/Proiettore/ProiettorePartita.razor`, `Web/Components/Pages/Proiettore/ProiettorePartita.razor.css`, `Web/Components/ProjectorCountdown.razor`, `Web/Components/ProjectorCountdown.razor.css`, `docs/TODO.md`, `docs/PROJECT_STATE.md`.
- Verifiche:
  - `dotnet test Core.Tests\\Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter FullyQualifiedName~PlayerGameViewServiceTests`: 14 test superati;
  - primo tentativo di `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: un errore SQLite intermittente nella fixture di `SquadreServiceTests`;
  - ripetizione di `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 107 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita, 0 errori e 11 avvisi NuGet noti;
  - `git diff --check`: riuscito.
- Rischi, assunzioni o blocchi:
  - il collaudo visivo multi-risoluzione resta non bloccante e registrato in AQ-050 perché la policy browser ha negato l'URL locale.

## 2026-09-02 - AQ-050 Ridisegno proiettore e risultati domanda

- Task: AQ-050.
- Risultato:
  - ridisegnata la vista del proiettore con quattro opzioni colorate, contrastate e identificabili anche dalla lettera;
  - dopo la chiusura, l'opzione corretta riceve un bordo verde e il badge testuale `Risposta corretta`;
  - aggiunto un riepilogo pubblico di risposte corrette, errate, astensioni e squadra o squadre con risposta valida più veloce;
  - rimossi dalla vista pubblica gli enum e le informazioni tecniche di fase;
  - aggiunti stili responsive specifici per layout 4:3 e 16:9;
  - AQ-050 concluso e AQ-051 promosso a `READY`.
- File principali: `Web/Components/Pages/Proiettore/ProiettorePartita.razor`, `Web/Components/Pages/Proiettore/ProiettorePartita.razor.css`, `Web/Services/ProjectorQuestionResultsService.cs`, `Core.Tests/PlayerGameViewServiceTests.cs`, `docs/TODO.md`, `docs/PROJECT_STATE.md`.
- Verifiche:
  - `dotnet test Core.Tests\\Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter FullyQualifiedName~PlayerGameViewServiceTests`: 14 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 107 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita, 0 errori e 11 avvisi NuGet noti;
  - avvio dell'app con database SQLite temporaneo nel worktree: riuscito;
  - verifica browser del proiettore: non eseguibile, apertura dell'URL locale negata dalla policy browser;
  - `git diff --check`: riuscito.
- Rischi, assunzioni o blocchi:
  - una risposta valida per il riepilogo è una scelta A/B/C/D confermata e non annullata; può essere errata, perché il requisito non richiede la sola risposta corretta;
  - collaudo visivo a cinque risoluzioni residuo e non bloccante, registrato in AQ-050.

## 2026-09-02 - Pianificazione aggiunta casuale di domande filtrate

- Task: pianificazione della nuova funzione richiesta dal proprietario per la configurazione delle domande di una manche.
- Risultato:
  - aggiunto AQ-059 in fondo alla coda;
  - previsti filtri distinti per categoria e difficoltà, conteggio delle domande disponibili e quantità modificabile tra 1 e il totale filtrato;
  - specificata l'aggiunta casuale senza ripetizioni, atomica e in coda all'ordine esistente;
  - mantenuta l'aggiunta manuale della singola domanda.
- File principali: `docs/TODO.md`, `docs/WORKLOG.md`.
- Verifiche:
  - confrontata la richiesta con la pagina `MancheDomandeConfig` e con il vincolo che impedisce di riutilizzare una domanda nella stessa partita;
  - `git diff --check`.
- Rischi, assunzioni o blocchi:
  - il task considera disponibili soltanto domande utilizzabili e non già associate alla partita; l'implementazione resta fuori da questa modifica documentale.

---

## 2026-09-02 - AQ-058 Ripristino avvio manche successiva

- Task: AQ-058.
- Risultato:
  - introdotto un ordinamento condiviso delle manche per `Ordine` e ID, usato sia dal motore sia dalla regia;
  - dopo la fine di una manche la regia distingue correttamente l'avvio della successiva dalla conclusione della partita anche con valori `Ordine` duplicati;
  - le nuove partite numerano le manche iniziali progressivamente e la configurazione assegna alla nuova manche il successivo ordine massimo disponibile;
  - AQ-058 concluso e AQ-050 promosso a `READY`.
- File principali: `Core/Services/MancheOrderingService.cs`, `Web/Services/PartitaStateMachineService.cs`, `Web/Components/Pages/RegiaPartita.razor`, `Web/Components/Pages/NuovaPartita.razor`, `Web/Components/Pages/PartitaConfig.razor`, `Core.Tests/PartitaStateMachineServiceTests.cs`, `docs/TODO.md`, `docs/PROJECT_STATE.md`.
- Verifiche:
  - `dotnet test Core.Tests\\Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter FullyQualifiedName~PartitaStateMachineServiceTests`: 6 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 105 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita, 0 errori e 3 avvisi NuGet noti;
  - avvio con credenziali temporanee di processo: bloccato prima di Kestrel dalla migrazione SQLite perché il database locale è in sola lettura;
  - `git diff --check`: riuscito.
- Rischi, assunzioni o blocchi:
  - il pareggio di `Ordine` viene risolto dall'ID persistito, quindi le partite storiche restano percorribili in modo stabile;
  - smoke UI residuo non bloccante registrato in AQ-058: serve un database SQLite scrivibile e una configurazione admin locale.

---

## 2026-09-02 - AQ-049 Controllo visibilità QR sul proiettore

- Task: AQ-049.
- Risultato:
  - aggiunto in regia il comando esplicito `Mostra QR` / `Nascondi QR`;
  - il proiettore riceve subito il cambio tramite lo stato runtime condiviso e rimuove interamente il componente QR quando nascosto;
  - una seconda sessione proiettore usa lo stato runtime corrente, senza sovrascrivere la visibilità scelta con lo stato persistito;
  - AQ-049 concluso e AQ-058 promosso a `READY`.
- File principali: `Core/Enums/GamePhase.cs`, `Web/Services/GameStateService cs.cs`, `Web/Components/Pages/RegiaPartita.razor`, `Web/Components/Pages/Proiettore/ProiettorePartita.razor`, `Core.Tests/GameStateServiceTests.cs`, `docs/TODO.md`, `docs/PROJECT_STATE.md`.
- Verifiche:
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false --filter FullyQualifiedName~GameStateServiceTests`: 2 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 104 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita, 0 errori e 3 avvisi noti;
  - `git diff --check`: riuscito.
- Rischi, assunzioni o blocchi:
  - la visibilità del QR è volutamente runtime: dopo un riavvio torna visibile;
  - lo smoke su due sessioni non è eseguibile qui perché l'avvio dell'app fallisce sul database SQLite locale in sola lettura; il test umano è registrato in AQ-049.

---

## 2026-09-02 - Segnalazione regressione passaggio tra manche

- Task: pianificazione del bug segnalato dal proprietario durante una partita reale.
- Risultato:
  - aggiunto AQ-058 come task P0 subito dopo AQ-049;
  - registrato che, dopo la conclusione della prima manche, la regia può non esporre l'avvio della seconda;
  - i criteri coprono sia le nuove partite sia quelle esistenti con valori `Ordine` duplicati;
  - AQ-050 dipende ora da AQ-058, così la regressione funzionale viene affrontata prima del ridisegno del proiettore.
- File principali: `docs/TODO.md`, `docs/WORKLOG.md`.
- Verifiche:
  - confrontata la logica della regia con l'ordinamento del motore e con la creazione multipla delle manche;
  - `git diff --check`.
- Rischi, assunzioni o blocchi:
  - causa probabile verificata nel codice: la regia richiede un `Ordine` maggiore, mentre più manche create insieme possono ricevere lo stesso ordine; l'implementazione della correzione resta fuori da questa modifica documentale.

---

## 2026-09-01 - Protocollo TestUmano per verifiche residue

- Task: aggiornamento operativo richiesto dal proprietario.
- Risultato:
  - definita in `AGENTS.md` e `docs/TODO.md` la dicitura ricercabile `TestUmano: DA ESEGUIRE` per prove manuali non bloccanti;
  - un test umano mancante richiede ora una valutazione esplicita del suo impatto sui task successivi prima di usare `BLOCKED`;
  - AQ-048 registra i passi, il risultato atteso e il rischio residuo della sua prova manuale.
- File principali: `AGENTS.md`, `docs/TODO.md`, `docs/PROJECT_STATE.md`, `docs/WORKLOG.md`.
- Verifiche:
  - riesaminati stati della coda e vincoli di completamento; confermato che AQ-049 dipende da AQ-048 ma non dal collaudo browser residuo.
- Rischi, assunzioni o blocchi:
  - la classificazione non consente di chiudere task con errori di build o test automatici causati dalla modifica; tali errori restano da correggere o da bloccare secondo il loro impatto.

---

## 2026-09-01 - AQ-048 verifica manuale resa non bloccante

- Task: AQ-048.
- Risultato:
  - su decisione esplicita del proprietario, AQ-048 è concluso perché le verifiche automatiche e di compilazione sono già riuscite;
  - lo smoke dell'aggiornamento automatico e la verifica del layout a 1366×768 restano annotati come collaudo manuale residuo, senza bloccare la coda;
  - AQ-049 è promosso a `READY`.
- File principali: `docs/TODO.md`, `docs/PROJECT_STATE.md`, `docs/WORKLOG.md`.
- Verifiche:
  - riesaminati gli esiti già registrati: 3 test mirati, 103 test complessivi, build con 0 errori e `git diff --check` riuscito.
- Rischi, assunzioni o blocchi:
  - il browser locale continua a negare l'accesso all'URL dell'app; la prova manuale va eseguita in un ambiente browser disponibile prima della serata.

---

## 2026-09-01 - AQ-048 Riorganizzare la regia della partita

- Task: AQ-048.
- Risultato:
  - la regia espone un solo comando primario coerente con la fase persistita e lascia visibili solo le alternative consentite;
  - il selettore manuale della manche è assente, lo stato e il countdown restano in una testata fissa e l'annullamento è separato dai comandi di avanzamento;
  - l'apertura della regia verifica la partita attiva, richiede conferma prima della sostituzione e aggiorna l'interfaccia quando `GameStateService` notifica un cambio di fase;
  - aggiunto un test d'integrazione che verifica la richiesta di conferma senza modificare la partita attiva.
- File principali: `Web/Components/Pages/RegiaPartita.razor`, `Web/Services/PartitaAttivaService.cs`, `Core.Tests/PartitaAttivaServiceTests.cs`.
- Verifiche:
  - `dotnet test Core.Tests\Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter FullyQualifiedName~PartitaAttivaServiceTests`: 3 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 103 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: compilazione completata con 0 errori e 3 avvisi NuGet noti;
  - `git diff --check`: nessun errore di spaziatura.
- Rischi, assunzioni o blocchi:
  - il browser locale ha negato l'accesso a `127.0.0.1`, quindi non è stato possibile completare lo smoke dell'aggiornamento alla scadenza né la verifica manuale a 1366×768;
  - AQ-048 resta `BLOCKED` esclusivamente in attesa di queste verifiche essenziali.

---

## 2026-08-31 - Estensione della coda UX e manutenzione dati

- Task: aggiornamento del piano su richiesta esplicita del proprietario.
- Risultato:
  - aggiornato AQ-048 affinché la regia mostri soltanto le operazioni possibili, attivi la partita selezionata e reagisca agli avanzamenti prodotti dal timer;
  - estesa la validazione di AQ-050 alla resa flessibile su proiettori e TV 4:3 o 16:9, da 800×600 a 4K;
  - aggiunti in fondo alla coda AQ-052–AQ-057 per regole di manche, preparazione guidata, identità visiva, archiviazione del catalogo, backup SQLite e reset dei dati;
  - mantenuto AQ-048 come unico task `IN_PROGRESS`; tutti i nuovi task sono `PLANNED` e dipendono in sequenza dalla coda esistente.
- File principali: `docs/TODO.md`, `docs/WORKLOG.md`.
- Verifiche: controllo manuale di stati, dipendenze e ordine della coda; `git diff --check`.
- Rischi, assunzioni o blocchi:
  - l'apertura della regia rende attiva la partita selezionata; quando sostituisce un'altra partita attiva è richiesta una conferma esplicita;
  - il reset completo richiede un backup automatico verificato e non è consentito durante una partita in corso.

---

## 2026-08-19 - AQ-047 Impedire associazioni duplicate e nascondere gli ID interni

- Task: AQ-047.
- Risultato:
  - aggiunto un servizio server-side che rifiuta, prima del salvataggio, una domanda già associata a un'altra manche della stessa partita con un messaggio italiano;
  - l'elenco delle domande disponibili esclude tutte quelle già usate nella partita corrente, lasciandole riutilizzabili in un'altra partita;
  - rimossi gli identificativi tecnici dalle pagine di configurazione partita/manche e dal catalogo domande; collegamenti e operazioni continuano a usare gli ID internamente;
  - AQ-047 concluso e AQ-048 promosso a `READY`.
- File principali: `Web/Services/MancheDomandaAssociationService.cs`, `Web/Components/Pages/MancheDomandeConfig.razor`, `Web/Components/Pages/PartitaConfig.razor`, `Web/Components/Pages/DomandeAdmin.razor`, `Core.Tests/MancheDomandaAssociationServiceTests.cs`.
- Verifiche:
  - `dotnet test Core.Tests\Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter FullyQualifiedName~MancheDomandaAssociationServiceTests`: 2 test superati;
  - `rg -n -i 'Id:|<th>Id</th>|#[{]?_.*Id|@[a-zA-Z_]+\.Id' Web\Components\Pages\PartitaConfig.razor Web\Components\Pages\MancheDomandeConfig.razor Web\Components\Pages\DomandeAdmin.razor`: restano solo i parametri tecnici delle route;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 102 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: compilazione completata con 0 errori e 3 avvisi noti;
  - `git diff --check`: nessun errore di spaziatura.
- Rischi residui:
  - lo smoke interattivo della configurazione manche/domande va ripetuto con un'istanza locale disponibile; il vincolo funzionale è coperto dal servizio e dai test d'integrazione.

---

## 2026-08-19 - AQ-046 Correggere il contatore domande di `/partita`

- Task: AQ-046.
- Risultato:
  - il caricamento della configurazione partita include ora le associazioni alle domande di ciascuna manche;
  - il contatore mostra quindi il numero effettivo di domande della partita visualizzata, incluso lo zero per una manche vuota;
  - aggiunto test d'integrazione sui riepiloghi di una partita vuota e di una popolata;
  - AQ-046 concluso e AQ-047 promosso a `READY`.
- File principali: `Web/Components/Pages/PartitaConfig.razor`, `Core.Tests/PartitaConfigSummaryTests.cs`, `docs/TODO.md`, `docs/PROJECT_STATE.md`.
- Verifiche:
  - `dotnet test Core.Tests\Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter FullyQualifiedName~PartitaConfigSummaryTests`: 1 test superato;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 100 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: compilazione completata con 0 errori e 3 avvisi noti;
  - `git diff --check`: nessun errore di spaziatura.
- Rischi residui:
  - lo smoke interattivo da browser con partite vuote e popolate va ripetuto con un'istanza locale disponibile; la query e i due stati sono coperti dal test d'integrazione.

---

## 2026-08-19 - AQ-045 Semplificare menu e layout della dashboard admin

- Task: AQ-045.
- Risultato:
  - rimossi dalla dashboard i collegamenti rapidi prototipali a gestione domande ed elenco partite;
  - posizionato `Nuova partita` nell'intestazione dell'elenco, visibile direttamente e adattato ai viewport stretti tramite contenitore responsive;
  - mantenuta la voce `Gestione domande` nel menu laterale amministrativo, protetto come le altre route admin;
  - AQ-045 concluso e AQ-046 promosso a `READY`.
- File principali: `Web/Components/Pages/AdminHome.razor`, `Web/Components/Layout/NavMenu.razor`, `docs/TODO.md`.
- Verifiche:
  - `rg -n "Gestisci domande|Elenco partite|Nuova partita|Partite" Web\Components\Pages\AdminHome.razor Web\Components\Layout\NavMenu.razor`: confermate rimozione dalla dashboard e presenza della navigazione amministrativa;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: compilazione completata con 0 errori e 3 avvisi noti;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 99 test superati alla seconda esecuzione; la prima ha avuto un errore intermittente di handle SQLite nel test concorrente preesistente delle risposte.
- Rischi residui:
  - lo smoke a larghezza desktop e ridotta va ripetuto con un'istanza locale in ascolto; il connettore browser non ha trovato un'applicazione disponibile su `127.0.0.1:5000`.

---

## 2026-08-19 - Chiarimento layout dashboard admin

- Task: correzione del criterio di `AQ-045` su richiesta del proprietario.
- Risultato:
  - il collegamento rapido `Gestisci domande` viene rimosso dalla pagina dashboard;
  - la voce resta nel menu di navigazione admin.
- File principali: `docs/TODO.md`, `docs/WORKLOG.md`.
- Verifiche:
  - verificata la modifica del solo criterio relativo a `Gestisci domande`.
- Rischi, assunzioni o blocchi:
  - nessuno; la pagina e le funzioni del catalogo restano disponibili.

## 2026-08-19 - Pianificazione clonazione partita

- Task: aggiornamento della coda su richiesta del proprietario.
- Risultato:
  - aggiunto `AQ-051` per clonare la configurazione completa di una partita senza ereditare squadre, risposte, punteggi o stato runtime;
  - collegato `AQ-051` alla conclusione della nuova sequenza di pulizia funzionale, prima della pulizia tecnica `AQ-041`.
- File principali: `docs/TODO.md`, `docs/WORKLOG.md`.
- Verifiche:
  - controllata la coda esistente e preservato `AQ-045` come unico task `READY`;
  - verificato che la clonazione prevista non attivi automaticamente la nuova partita.
- Rischi, assunzioni o blocchi:
  - la clonazione riusa le domande del catalogo tramite associazioni; non crea copie delle entità `Domanda`.

## 2026-08-19 - AQ-044 Gestire partita attiva, storico e navigazione admin

- Task: AQ-044.
- Risultato:
  - aggiunta la selezione persistita `IsAttiva` con indice SQLite filtrato che consente una sola partita attiva;
  - l'admin può rendere attiva una partita non conclusa dall'elenco, aprire la regia e il proiettore della partita specifica e consultare lo storico senza cancellazioni;
  - ingresso pubblico, iscrizione e login senza identificativo esplicito usano solo la partita attiva;
  - la conclusione dalla regia disattiva la partita e il servizio rifiuta di riattivare una partita conclusa;
  - AQ-044 concluso e AQ-045 promosso a `READY`.
- File principali: `Core/Entities/Partita.cs`, `Infrasctructure/Migrations/20260819151955_AddActiveGame.cs`, `Web/Services/PartitaAttivaService.cs`, `Web/Components/Pages/AdminHome.razor`, `Core.Tests/PartitaAttivaServiceTests.cs`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~SquadreServiceTests|FullyQualifiedName~PartitaAttivaServiceTests|FullyQualifiedName~PlayerEntryServiceTests|FullyQualifiedName~PartitaStateMachineServiceTests"`: 27 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 99 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: compilazione completata con 0 errori e 3 avvisi noti;
  - `dotnet tool run dotnet-ef migrations has-pending-model-changes --project Infrasctructure\Infrasctructure.csproj --startup-project Web\Web.csproj --no-build`: nessuna modifica pendente al modello.
- Rischi residui:
  - lo smoke interattivo di dashboard, regia e proiettore va ripetuto con un'istanza locale in ascolto; il connettore browser non ha trovato un'applicazione disponibile su `127.0.0.1:5000`.

---

## 2026-08-19 - Pianificazione pulizia funzionale prima della distribuzione

- Task: aggiornamento della coda su richiesta del proprietario.
- Risultato:
  - aggiunti task per partita attiva/storico e navigazione admin, dashboard, contatore domande, associazioni duplicate, regia e proiettore;
  - `AQ-044` promosso a `READY` e `AQ-041` spostato dopo la nuova sequenza;
  - mantenuta la conservazione dello storico prevista dal prodotto: la conclusione è pianificata, la cancellazione definitiva resta da decidere separatamente.
- File principali: `docs/TODO.md`, `docs/PROJECT_STATE.md`, `docs/WORKLOG.md`.
- Verifiche:
  - controllato `git status --short` prima delle modifiche;
  - verificata la presenza di un solo task `READY` nella coda.
- Rischi, assunzioni o blocchi:
  - il significato operativo di cancellazione definitiva delle partite non è stato introdotto perché in conflitto con la conservazione dello storico richiesta dal prodotto.

## 2026-08-19 - AQ-040 Verificare recupero completo dopo riavvio

- Task: AQ-040.
- Risultato:
  - aggiunta una suite d'integrazione che arresta e riapre il `DbContext` sullo stesso database SQLite per lobby, domanda aperta, domanda scaduta, soluzione e fine manche;
  - verificati il tempo residuo, la chiusura idempotente della domanda scaduta con due sole astensioni attese, le sessioni squadra e la classifica persistita;
  - documentata nel README la procedura manuale di ripresa della partita;
  - AQ-040 concluso e AQ-041 promosso a `READY`.
- File principali: `Core.Tests/RestartRecoveryTests.cs`, `README.md`, `docs/TODO.md`, `docs/PROJECT_STATE.md`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~RestartRecoveryTests`: 5 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 97 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: compilazione completata con 0 errori e 3 avvisi noti.
- Rischi residui:
  - il test simula l'arresto e il riavvio chiudendo e riaprendo il contesto SQLite; resta da effettuare uno smoke interattivo quando il connettore browser sarà disponibile.

---

## 2026-08-19 - AQ-036 Annullare una domanda e ricalcolare

- Task: AQ-036.
- Risultato:
  - aggiunto il comando server-side per annullare una singola domanda già chiusa, con motivo e timestamp persistiti;
  - neutralizzate le risposte dell'occorrenza e segnalata la domanda catalogo con `FlagErrore`;
  - ricalcolate le domande successive della manche, così che astensioni gratuite, malus e classifica derivata restino coerenti;
  - corretto il passaggio alla domanda successiva quando quella corrente è stata annullata;
  - AQ-036 concluso e AQ-040 promosso a `READY`.
- File principali: `Web/Services/QuestionCancellationService.cs`, `Web/Services/PartitaStateMachineService.cs`, `Web/Components/Pages/RegiaPartita.razor`, `Core.Tests/QuestionCancellationServiceTests.cs`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter FullyQualifiedName~QuestionCancellationServiceTests`: 2 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 92 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: compilazione completata con 0 errori e 3 avvisi noti.
- Rischi residui:
  - lo smoke interattivo della regia va ripetuto quando il connettore browser sarà disponibile.

---

## 2026-08-19 - AQ-035 Implementare classifiche e podio

- Task: AQ-035.
- Risultato:
  - aggiunto `LeaderboardService`, che ricostruisce i punteggi da `MancheRispostaRicevuta`, includendo anche le squadre senza risposte ed escludendo risposte e occorrenze annullate;
  - ordinamento per punti decrescenti, parità ex aequo e posizione successiva coerente;
  - il proiettore mostra classifiche parziali, di fine manche e finale, con podio delle posizioni dalla prima alla terza;
  - AQ-035 concluso e AQ-036 promosso a `READY`.
- File principali: `Web/Services/LeaderboardService.cs`, `Web/Components/Pages/Proiettore/ProiettorePartita.razor`, `Core.Tests/LeaderboardServiceTests.cs`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter FullyQualifiedName~LeaderboardServiceTests`: 2 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 90 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: compilazione completata con 0 errori e 3 avvisi noti.
- Rischi residui:
  - lo smoke visivo del proiettore va ripetuto quando il connettore browser sarà disponibile.

---

## 2026-08-19 - AQ-034 Mostrare soluzione e distribuzione risposte

- Task: AQ-034.
- Risultato:
  - il proiettore visualizza soluzione e conteggi aggregati A/B/C/D soltanto nella fase persistita `ShowingAnswers`;
  - il telefono mostra l'esito personale corretto, errato o astenuto e la variazione punti già calcolata e persistita;
  - una squadra registrata dopo la chiusura riceve un messaggio di mancata partecipazione e variazione nulla, senza creare retroattivamente una risposta;
  - AQ-034 concluso e AQ-035 promosso a `READY`.
- File principali: `Web/Services/ProjectorAnswerDistributionService.cs`, `Web/Services/PlayerGameViewService.cs`, `Web/Components/Pages/Proiettore/ProiettorePartita.razor`, `Web/Components/Pages/SquadraSessione.razor`, `Core.Tests/PlayerGameViewServiceTests.cs`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter FullyQualifiedName~PlayerGameViewServiceTests`: 12 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 88 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: compilazione completata con 0 errori e 3 avvisi noti;
  - avvio isolato con database temporaneo in `artifacts/` riuscito.
- Rischi residui:
  - lo smoke visivo multi-client va ripetuto quando il connettore browser sarà disponibile; il suo runtime non riesce a risolvere una dipendenza attendibile sul percorso Windows.

---

## 2026-08-19 - AQ-033 Calcolare punteggio e astensioni

- Task: AQ-033.
- Risultato:
  - implementata la logica deterministica di calcolo del punteggio e registrazione automatica delle astensioni (`QuestionScoringService`);
  - il coefficiente di velocità varia da 1.0 (risposta immediata) a 0.5 (allo scadere del timer) e viene arrotondato con `AwayFromZero`;
  - applicati i moltiplicatori di manche e di domanda a punti e malus;
  - alla chiusura della domanda (manuale o da timer) vengono registrate automaticamente le astensioni per le squadre senza risposta;
  - le astensioni sono gratuite fino alla soglia `MaxAstensioni` di manche, dopodiché comportano il medesimo malus delle risposte errate;
  - il calcolo è deterministico e idempotente.
- File principali: `Web/Services/QuestionScoringService.cs`, `Web/Services/PartitaStateMachineService.cs`, `Web/Services/GameTimerService.cs`, `Core.Tests/QuestionScoringServiceTests.cs`, `Core.Tests/PlayerAnswerServiceTests.cs`.
- Verifiche:
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 84 test superati (0 errori);
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: compilazione completata con 0 errori.
- Rischi residui:
  - avvisi NuGet noti su dipendenze e audit non raggiungibile.

---

## 2026-08-18 - AQ-032 Implementare il client squadra e l'invio risposta

- Task: AQ-032.
- Risultato:
  - aggiunto il servizio server-side di registrazione della prima risposta A/B/C/D, con verifica di sessione esclusiva, domanda corrente e scadenza;
  - il vincolo univoco già presente viene gestito come conferma idempotente durante invii concorrenti o retry;
  - la vista squadra riceve solo il proprio codice di risposta già registrato e non espone la soluzione durante la domanda;
  - la shell mobile usa pulsanti leggibili, disabilita le alternative dopo la conferma e mostra lo stato della risposta immutabile.
- File principali: `Web/Services/PlayerAnswerService.cs`, `Web/Services/PlayerGameViewService.cs`, `Web/Components/Pages/SquadraSessione.razor`, `Web/Components/Pages/SquadraSessione.razor.css`, `Core.Tests/PlayerAnswerServiceTests.cs`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter FullyQualifiedName~PlayerAnswerServiceTests`: 5 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 68 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita con 0 errori e 3 avvisi noti;
  - avvio locale dell'app riuscito; markup e CSS della shell verificati a layout mobile. Il connettore browser non ha completato lo smoke interattivo per un errore del runtime del plugin.
- Rischi residui:
  - restano gli avvisi NuGet noti (dipendenza SQLite vulnerabile e audit non raggiungibile);
  - la verifica visiva interattiva con browser va ripetuta quando il connettore sarà disponibile.

---

## 2026-08-18 - Semplificare l'apertura del proiettore dalla regia

- Task: modifica UX richiesta esplicitamente dal proprietario.
- Risultato:
  - rimosso dalla pagina regia il QR diretto al proiettore;
  - mantenuto invariato il bottone `Apri proiettore`, che continua ad aprire la vista in una nuova scheda;
  - rimossi injection, campi e codice commentato LAN/QR rimasti inutilizzati nel componente.
- File principali: `Web/Components/Pages/RegiaPartita.razor`, `docs/WORKLOG.md`.
- Verifiche:
  - build completa su output temporaneo separato: riuscita con 0 errori e 3 avvisi noti;
  - suite completa sullo stesso output: 63 test superati;
  - ricerca nel componente: nessun riferimento residuo a QR, `LanQr`, `ILanAddressService` o `IQrCodeService`; bottone `Apri proiettore` presente.
- Rischi residui: la build standard resta subordinata all'arresto dell'istanza `Web.exe` avviata dal proprietario; la compilazione degli stessi sorgenti su output separato è riuscita.

---

## 2026-08-18 - Distinguere login e navigazione per ruolo

- Task: miglioramento UX richiesto esplicitamente dal proprietario dopo la separazione delle autenticazioni admin e squadra.
- Risultato:
  - le pagine mostrano chiaramente `Login admin` e `Login squadra`, con un pulsante reciproco per passare all'altro accesso;
  - il menu usa la policy squadra: il giocatore vede soltanto `Partita` ed `Esci dalla squadra`, mentre il menu admin mantiene Home, gestione domande e logout amministrativo;
  - brand del menu distinti in `ArciQuiz Squadra` e `ArciQuiz Admin`.
- File principali: `Web/Program.cs`, `Web/Components/Layout/NavMenu.razor`, `docs/PROJECT_STATE.md`, `docs/WORKLOG.md`.
- Verifiche:
  - build completa su output temporaneo separato: riuscita con 0 errori e 5 avvisi noti;
  - suite completa sullo stesso output: 63 test superati;
  - smoke HTTP: titoli e pulsanti reciproci presenti; dopo login il menu squadra contiene soltanto brand squadra, partita e logout squadra, senza voci amministrative.
- Rischi residui:
  - la build standard nella cartella `bin` non è stata possibile perché l'istanza del proprietario `Web.exe` PID 26300 era in esecuzione; non è stata arrestata;
  - gli artefatti di verifica restano in `%TEMP%\arciquiz-menu-build` perché la rimozione di file temporanei non è stata autorizzata nella sessione.

---

## 2026-08-18 - Correzione autenticazione area squadra

- Task: correzione regressione segnalata dal proprietario dopo AQ-024/AQ-025.
- Problema: dopo il login da `/gioca`, il cookie squadra veniva creato ma il circuito Blazor autenticava soltanto lo schema predefinito admin; `AuthorizeRouteView` considerava quindi la squadra anonima e `RedirectToLogin` la inviava a `/login`.
- Risultato:
  - aggiunto uno schema di autenticazione selettore che usa il cookie squadra per `/squadra/*` e per il circuito `/_blazor` quando quel cookie è presente, mantenendo il cookie admin sulle altre richieste;
  - la policy squadra richiede autenticazione e claim identificativo della squadra, senza poter essere soddisfatta da una sessione admin;
  - il redirect dei componenti non autorizzati riconosce l'area squadra e torna all'ingresso `/gioca` invece del login amministrativo;
  - aggiunti sei casi di test sulla selezione dello schema, inclusa la connessione interattiva Blazor con e senza cookie squadra.
- File principali: `Web/Program.cs`, `Web/Services/PlayerSessionService.cs`, `Web/Components/RedirectToLogin.razor`, `Core.Tests/PlayerEntryServiceTests.cs`.
- Verifiche:
  - test mirati `PlayerEntryServiceTests|PlayerGameViewServiceTests`: 21 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 63 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita con 0 errori e 3 avvisi noti;
  - smoke HTTP su server e database temporanei: login squadra termina su `/squadra`, mostra la squadra e non il login admin; rientro da `/gioca` torna a `/squadra`; `/admin` anonimo continua a terminare su `/login`.
- Rischi residui:
  - il database `arciquiz-auth-fix-smoke.db` resta nella cartella temporanea del profilo perché l'autorizzazione alla sua rimozione è stata rifiutata; non appartiene al repository e non contiene dati reali.

---

## 2026-08-18 - AQ-025 Creare la shell mobile-first della squadra

- Task: AQ-025, assegnato esplicitamente dal proprietario insieme ad AQ-024.
- Risultato:
  - sostituita la pagina squadra prototipale con una shell mobile-first per attesa, domanda, tempo scaduto, soluzione, classifica, fine manche e fine partita;
  - introdotto un view model server-side che valida la sessione esclusiva e include domanda, opzioni, scadenza e soluzione soltanto nelle fasi consentite;
  - refresh, notifiche di stato e rientro dal QR ricaricano la vista dal database; nome squadra e stato connessione rimangono riconoscibili;
  - corretta durante lo smoke la protezione del componente usando una policy dedicata allo schema cookie squadra, perché Blazor non supporta `AuthenticationSchemes` direttamente sull'attributo del componente;
  - `AQ-025` dichiarato `DONE` e `AQ-032` promosso a `READY`, senza avviarlo.
- File principali: `Web/Services/PlayerGameViewService.cs`, `Web/Components/Pages/SquadraSessione.razor`, `Web/Components/Pages/SquadraSessione.razor.css`, `Web/Program.cs`, `Core.Tests/PlayerGameViewServiceTests.cs`.
- Verifiche:
  - test mirati `PlayerEntryServiceTests|PlayerGameViewServiceTests`: 15 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 57 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita con 0 errori e 5 avvisi noti;
  - smoke HTTP su server e database temporanei: login, rendering dello stato di attesa e rientro da `/gioca` con la stessa sessione verificati; database temporaneo rimosso;
  - `dotnet tool run dotnet-ef migrations has-pending-model-changes --project Infrasctructure/Infrasctructure.csproj --startup-project Web/Web.csproj --no-build`: nessuna modifica modello pendente.
- Rischi residui:
  - il plugin browser integrato non si è inizializzato a causa del percorso Windows del profilo contenente spazi; non è stato quindi possibile acquisire uno screenshot a larghezza mobile. Viewport, CSS responsive e flusso runtime HTTP sono stati verificati;
  - selezione e conferma della risposta appartengono ad AQ-032 e non sono state avviate.

---

## 2026-08-18 - AQ-024 Creare l'ingresso unico della squadra

- Task: AQ-024, assegnato esplicitamente dal proprietario dopo l'analisi del flusso utente.
- Risultato:
  - aggiunto `/gioca` come ingresso stabile per ogni QR del proiettore, con routing server-side verso registrazione, login o area squadra in base a stato e sessione;
  - la registrazione HTTP protetta da antiforgery crea immediatamente sessione esclusiva e cookie persistente per 12 ore; un token sostituito nel database continua a invalidare il vecchio dispositivo;
  - aggiunti percorsi mobile chiari per lobby, partita iniziata, partita conclusa, assenza partita e sessione sostituita; il vecchio `/registrazione` reindirizza all'ingresso supportato;
  - `AQ-024` dichiarato `DONE`; AQ-025 è stato poi eseguito per assegnazione esplicita del proprietario.
- File principali: `Web/Services/PlayerEntryService.cs`, `Web/Services/PlayerSessionService.cs`, `Web/Program.cs`, `Web/Components/Pages/LobbyProiettore.razor`, `Web/Components/Pages/Proiettore/ProiettorePartita.razor`, `Core.Tests/PlayerEntryServiceTests.cs`.
- Verifiche:
  - test mirati `PlayerEntryServiceTests|PlayerGameViewServiceTests`: 15 test superati;
  - suite completa e build riportate nella voce AQ-025;
  - smoke HTTP su server e database temporanei: `/gioca` in lobby, form mobile con antiforgery, registrazione con autenticazione immediata e rientro dal QR verificati; database temporaneo rimosso.
- Rischi residui: nessuno specifico per l'ingresso unico; l'invio risposta rimane deliberatamente fuori ambito.

---

## 2026-08-18 - AQ-031 Implementare timer server-authoritative

- Task: AQ-031.
- Risultato:
  - la macchina a stati persiste `ScadenzaUtc` all'apertura della domanda usando il tempo standard della manche o l'override della singola domanda;
  - aggiunti `GameTimerService` per chiusura e validazione temporale e un `BackgroundService` che chiude automaticamente le domande scadute e notifica le UI;
  - proiettore e area squadra usano lo stesso componente countdown basato su `TimeProvider`, mentre l'accettazione resta decisa esclusivamente dal server;
  - configurazione e validazione supportano la durata personalizzata della domanda e rifiutano durate non positive anche se la UI viene bypassata;
  - `AQ-031` dichiarato `DONE` e `AQ-032` promosso a `READY`, senza avviarlo.
- File principali: `Web/Services/GameTimerService.cs`, `Web/Services/GameTimerBackgroundService.cs`, `Web/Services/PartitaStateMachineService.cs`, `Web/Components/ServerCountdown.razor`, `Web/Components/Pages/Proiettore/ProiettorePartita.razor`, `Web/Components/Pages/SquadraSessione.razor`, `Web/Components/Pages/MancheDomandeConfig.razor`, `Core.Tests/GameTimerServiceTests.cs`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter "FullyQualifiedName~GameTimerServiceTests"`: 5 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 42 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita con 0 errori e 3 avvisi noti;
  - test con `TimeProvider` manuale su durata standard, override, istante limite e riapertura del database superati.
- Rischi residui:
  - lo smoke test runtime aggiuntivo non è stato eseguito perché l'autorizzazione all'avvio locale è stata rifiutata; test e build richiesti dal task sono completi;
  - la registrazione delle risposte dovrà chiamare `GameTimerService.VerificaRispostaAsync` in AQ-032;
  - restano la vulnerabilità transitiva SQLite e la credenziale admin predefinita già note.

---

## 2026-08-18 - AQ-030 Implementare la macchina a stati persistente

- Task: AQ-030.
- Risultato:
  - aggiunta la fase persistente della partita con migrazione compatibile con partite esistenti;
  - introdotto un servizio testabile per le transizioni lobby, domanda, soluzione, classifica, fine manche e fine partita;
  - i comandi usano gli identificativi attesi di manche/domanda per rendere innocui i retry e rifiutano le transizioni fuori sequenza;
  - regia e proiettore derivano lo stato dal database; il singleton rimane soltanto un canale di notifica e il proiettore recupera la fase dopo la ricreazione del contesto;
  - `AQ-030` dichiarato `DONE` e `AQ-031` promosso a `READY`, senza avviarlo.
- File principali: `Web/Services/PartitaStateMachineService.cs`, `Core/Entities/Partita.cs`, `Core/Enums/GamePhase.cs`, `Web/Components/Pages/RegiaPartita.razor`, `Web/Components/Pages/Proiettore/ProiettorePartita.razor`, `Infrasctructure/Migrations/20260818122159_AddPersistentGamePhase.cs`, `Core.Tests/PartitaStateMachineServiceTests.cs`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter "FullyQualifiedName~PartitaStateMachineServiceTests"`: 5 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 36 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita con 0 errori e 3 avvisi noti;
  - smoke test con database temporaneo separato: migrazioni applicate, proiettore e lobby hanno restituito `200`, regia anonima `302` verso il login; database temporaneo rimosso.
- Rischi residui:
  - timer e accettazione effettiva delle risposte appartengono rispettivamente ad AQ-031 e AQ-032;
  - restano la vulnerabilità transitiva SQLite e la credenziale admin predefinita già note.

---

## 2026-08-18 - Chiusura AQ-023 Completare la lobby pubblica

- Task: AQ-023, eseguito su priorità esplicita del proprietario prima di AQ-022.
- Risultato:
  - aggiunta la lobby pubblica `/lobby/{id}` per proiettore, derivata dallo stato persistito `Pronta`, con QR di iscrizione e conteggio squadre aggiornato ogni due secondi;
  - estratto `LanUrlService`, che genera URL con IP LAN e porta/scheme dall'endpoint Kestrel configurato, senza porta hardcoded nei componenti QR;
  - la lobby segnala la chiusura delle iscrizioni al passaggio persistito a `InCorso`;
  - `AQ-023` dichiarato `DONE`; con AQ-022 nel frattempo completato, `AQ-030` promosso a `READY`.
- File principali: `Web/Services/LanUrlService.cs`, `Web/Components/Pages/LobbyProiettore.razor`, `Web/Components/LanQr.razor`, `Core.Tests/LanUrlServiceTests.cs`, `docs/TODO.md`, `docs/PROJECT_STATE.md`, `docs/WORKLOG.md`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter "FullyQualifiedName~LanUrlServiceTests"`: 2 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 31 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita con 0 errori e 3 avvisi;
  - smoke test LAN del proprietario: QR, registrazione, aggiornamento conteggio e chiusura lobby verificati con esito positivo.
- Rischi residui:
  - credenziale admin predefinita presente in `Web/appsettings.json`, modifica preesistente fuori dall'ambito AQ-023.

---

## 2026-08-18 - Chiusura AQ-022 Rendere esclusiva la sessione del dispositivo squadra

- Task: AQ-022.
- Risultato:
  - aggiunto accesso squadra con cookie dedicato e token persistito sia sulla squadra sia sulla sua iscrizione alla partita;
  - un nuovo accesso con credenziali valide sostituisce il token precedente; la pagina squadra informa il dispositivo sostituito e non lo considera più autorizzato;
  - il controllo della sessione usa il database, quindi rimane valido dopo un refresh e dopo la ricreazione del `DbContext`/riavvio dell'app;
  - aggiunti tre test per due sessioni concorrenti, refresh e recupero persistito;
  - `AQ-022` dichiarato `DONE` e `AQ-023` promosso a `READY`, senza avviarlo.
- File principali: `Web/Services/PlayerSessionService.cs`, `Web/Components/Pages/SquadraSessione.razor`, `Web/Program.cs`, `Core.Tests/SquadreServiceTests.cs`, `docs/TODO.md`, `docs/PROJECT_STATE.md`, `docs/WORKLOG.md`.
- Verifiche:
  - `dotnet test Core.Tests/Core.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false --filter "FullyQualifiedName~SquadreServiceTests"`: 7 test superati;
  - `dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: 29 test superati;
  - `dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false`: riuscita con 0 errori e 3 avvisi noti.
- Rischi residui: l'invio delle risposte sarà introdotto in AQ-032; dovrà usare `SessioneValidaAsync` prima di registrare una risposta.

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
