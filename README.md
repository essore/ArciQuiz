# ArciQuiz

ArciQuiz è un'applicazione locale per organizzare serate quiz di associazioni culturali con budget ridotto.

Una postazione Windows gestisce amministrazione e regia, alimenta una vista pubblica per il proiettore e coordina gli smartphone delle squadre tramite la stessa rete Wi-Fi. Il funzionamento non deve dipendere da Internet.

## Stato attuale

Il repository contiene un prototipo Blazor su .NET 10 con persistenza SQLite. Sono presenti parti della configurazione delle partite, del catalogo domande, della regia e del proiettore.

Il prodotto non è ancora utilizzabile per una serata completa. La build e la suite di test verificate l'11 luglio 2026 sono verdi; iscrizione, client squadra, timer completo, calcolo punteggi e recupero dopo riavvio non sono ancora implementati.

Lo stato verificato è descritto in [docs/PROJECT_STATE.md](docs/PROJECT_STATE.md).

## Documentazione

- [AGENTS.md](AGENTS.md): regole vincolanti per gli agenti autonomi.
- [docs/PRODUCT.md](docs/PRODUCT.md): obiettivo, utenti e regole di gioco.
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md): architettura attuale e direzione approvata.
- [docs/PROJECT_STATE.md](docs/PROJECT_STATE.md): fotografia tecnica corrente.
- [docs/TODO.md](docs/TODO.md): piano operativo ordinato e task atomici.
- [docs/DECISIONS.md](docs/DECISIONS.md): registro delle decisioni durevoli.
- [docs/WORKLOG.md](docs/WORKLOG.md): diario append-only delle sessioni.

## Struttura della soluzione

- `Core`: entità e stati di dominio.
- `Infrasctructure`: accesso dati EF Core e inizializzazione SQLite. Il refuso nel nome è preesistente e non va corretto incidentalmente.
- `Web`: applicazione Blazor, pagine, servizi runtime e asset.

## Comandi di riferimento

Da eseguire dalla root:

```powershell
dotnet tool restore
dotnet restore ArciQuiz.slnx
dotnet test ArciQuiz.slnx
dotnet build ArciQuiz.slnx
```

Il comando canonico è eseguire prima i test e poi la build completa.

Le migrazioni EF Core usano il tool locale `dotnet-ef` dichiarato in
`.config/dotnet-tools.json`; non è richiesta un'installazione globale.

## Dati SQLite locali

L'applicazione conserva il database in `%LocalAppData%\ArciQuiz\arciquiz.db`.
Questo è il solo percorso runtime usato dall'app; i file associati di SQLite
(`-wal`, `-shm` e `-journal`) sono dati locali ignorati da Git. Per il backup,
arrestare l'app e copiare l'intera cartella `%LocalAppData%\ArciQuiz`.

I database creati dal prototipo precedente nella directory di esecuzione non
vengono spostati né modificati automaticamente. Prima del primo avvio della
nuova versione, conservarne una copia con gli eventuali file `-wal` e `-shm`.
Il nuovo database viene creato separatamente nella cartella dati stabile; la
reimportazione dei dati del prototipo non è ancora supportata.

## Modalità di lavoro

Il proprietario avvia gli agenti quando dispone del budget di token. Ogni agente autonomo completa un solo task `READY`, verifica il risultato, aggiorna la documentazione operativa e si ferma. Il protocollo completo è in [AGENTS.md](AGENTS.md).
