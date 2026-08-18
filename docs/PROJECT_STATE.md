# Stato del progetto

Ultima verifica: **2026-08-18**  
Commit ispezionato prima della documentazione: `4f9df12` (`master`)  
Stadio: **prototipo compilabile con baseline di test (29 casi), migrazioni EF Core, modello persistente della prima release, catalogo CSV, dashboard admin protetta, registrazione squadre e sessione esclusiva del dispositivo squadra (AQ-022 completato)**

## Baseline verificata

Comandi eseguiti:

```powershell
dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false
dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false
```

Esito: test riusciti (29 casi su migrazioni, vincoli univoci squadra/risposta, persistenza estesa, validazione della partita pronta, CSV domande, perimetro admin, registrazione e sessione esclusiva delle squadre) e build riuscita, 0 errori e 3 avvisi noti.

Completato `AQ-022`: il login squadra crea una sessione persistita e sostituisce il dispositivo precedente, che riceve un messaggio esplicito al controllo successivo. `AQ-023` è ora il task autorizzato in coda (`READY`).

## Funzioni presenti

- soluzione .NET 10 a tre progetti;
- progetto xUnit con test sul versionamento monotono di `GameStateService`;
- tool locale `dotnet-ef` 10.0.0 dichiarato nel repository;
- Blazor Interactive Server;
- database SQLite con migrazione iniziale EF Core e seed limitato allo sviluppo;
- catalogo domande con creazione, modifica, filtro e soft delete;
- import/export CSV del catalogo con validazione atomica e segnalazione degli errori per riga;
- autenticazione amministrativa locale per dashboard, regia e operazioni amministrative;
- registrazione pubblica delle squadre in lobby e gestione admin con recupero/modifica password;
- login squadra con cookie dedicato e sessione esclusiva persistita;
- creazione partita e manche;
- configurazione di manche e associazione/ordine delle domande;
- validazione server-side della transizione della partita a `Pronta`, con messaggi italiani;
- dashboard admin prototipale;
- regia prototipale per avanzare tra domande;
- vista proiettore collegata allo stato runtime;
- generazione QR e scoperta indirizzo LAN.

## Funzioni parziali o divergenti

- stati partita/manche presenti ma non governati da un motore coerente;
- `GameState` esteso con valori prototipali neutri; timer, accettazione risposte e messaggi pubblici non sono ancora implementati;
- timer previsto dal modello ma non implementato;
- entità per squadre e risposte presenti ma prive del flusso applicativo;
- configurazione Kestrel/QR hardcoded sulla porta 5000;
- password squadra memorizzata in chiaro senza flusso login;
- flag errore associato alla domanda globale, mentre il requisito richiede anche l'annullamento della specifica occorrenza giocata;
- pagine e codice demo del template ancora presenti;
- UI e logica dati concentrate nei componenti Razor.

## Funzioni mancanti per la prima release

- client smartphone per leggere e rispondere;
- timer server-authoritative con override per domanda;
- risposta immutabile e idempotente;
- chiusura automatica e astensione;
- punteggio con velocità, moltiplicatori, malus e soglia astensioni;
- soluzione e distribuzione delle risposte;
- classifiche parziali, di manche e finale;
- annullamento domanda e ricalcolo;
- persistenza e ripresa della partita;
- storico partite e punteggi;
- packaging e avvio Windows per utenti non tecnici;
- CI.

## Debito e rischi noti

- smoke test runtime da ripetere per AQ-005 in un ambiente autorizzato a scrivere nel registro eventi Windows;
- stato live singleton come fonte di verità volatile;
- dipendenze transitive vulnerabili;
- suite di test ancora limitata a ventisei casi;
- nomi con refusi (`Infrasctructure`, `GameStateService cs.cs`);
- file demo e ampie porzioni commentate;
- possibile conflitto tra endpoint Kestrel e profili di lancio;
- percorso effettivo del database non evidente all'utente e non predisposto per backup.

## Assunzioni operative correnti

- una sola partita attiva;
- massimo 50 squadre;
- singolo processo Windows e singolo database SQLite;
- LAN affidabile ma non autenticata a livello di rete;
- nessuna dipendenza da Internet durante la serata;
- il proprietario sceglie il branch prima di avviare l'agente;
- nessun commit o push automatico.

## Gestione artefatti locali

Gli artefatti runtime SQLite (`.db`, WAL, SHM e journal) sono ignorati da Git.
Il database viene creato in `%LocalAppData%\ArciQuiz\arciquiz.db` e il seed
demo viene applicato solo nell'ambiente Development. I database precedenti
nella directory di esecuzione non vengono modificati automaticamente; la
procedura di conservazione è documentata nel README.

## Come aggiornare questo documento

Aggiornare solo fatti verificati che descrivono lo stato corrente. Non inserire qui attività future dettagliate: appartengono a [TODO.md](TODO.md). Non inserire il resoconto delle sessioni: appartiene a [WORKLOG.md](WORKLOG.md).
