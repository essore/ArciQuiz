# Stato del progetto

Ultima verifica: **2026-08-18**  
Commit ispezionato prima della documentazione: `3b476a5` (`master`)

Stadio: **prototipo compilabile con baseline di test (63 casi), ingresso QR unico e shell squadra mobile-first (AQ-025 completato)**

## Baseline verificata

Comandi eseguiti:

```powershell
dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false
dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false
```

Esito: test riusciti (63 casi, inclusi selezione dello schema cookie per area admin/squadra e circuito Blazor, routing/sessione squadra, view model per fase, matrice delle transizioni, idempotenza, timer con clock controllato, limite di scadenza e recupero dopo riavvio) e build riuscita, 0 errori e 3 avvisi noti.

Completati `AQ-024` e `AQ-025`: il QR del proiettore punta sempre a `/gioca`, che instrada registrazione, login o rientro dalla sessione; la squadra dispone di una shell mobile ricostruita dallo stato persistito e filtrata per non esporre la soluzione prima della fase consentita. `AQ-032` è ora il task autorizzato in coda (`READY`).

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
- ingresso squadra unico `/gioca`, con registrazione che autentica immediatamente il dispositivo e cookie persistente per la serata;
- pagine di login admin/squadra esplicite e collegate reciprocamente, con menu Blazor distinto per ruolo;
- lobby pubblica su proiettore con QR stabile verso l'ingresso unico e conteggio squadre aggiornato;
- shell squadra mobile-first per attesa, domanda, tempo scaduto, soluzione, classifica, fine manche e fine partita;
- macchina a stati persistente con comandi idempotenti e recupero dopo riavvio;
- timer server-authoritative con override per domanda, chiusura automatica e countdown condiviso;
- creazione partita e manche;
- configurazione di manche e associazione/ordine delle domande;
- validazione server-side della transizione della partita a `Pronta`, con messaggi italiani;
- dashboard admin prototipale;
- regia prototipale per avanzare tra domande;
- vista proiettore collegata allo stato runtime;
- generazione QR e scoperta indirizzo LAN.

## Funzioni parziali o divergenti

- la shell squadra e il timer derivano dallo stato persistito, ma selezione e invio delle risposte non sono ancora implementati;
- flag errore associato alla domanda globale, mentre il requisito richiede anche l'annullamento della specifica occorrenza giocata;
- pagine e codice demo del template ancora presenti;
- UI e logica dati concentrate nei componenti Razor.

## Funzioni mancanti per la prima release

- invio della risposta dal client smartphone;
- risposta immutabile e idempotente;
- registrazione automatica delle astensioni alla chiusura;
- punteggio con velocità, moltiplicatori, malus e soglia astensioni;
- soluzione e distribuzione delle risposte;
- classifiche parziali, di manche e finale;
- annullamento domanda e ricalcolo;
- storico partite e punteggi;
- packaging e avvio Windows per utenti non tecnici;
- CI.

## Debito e rischi noti

- dipendenze transitive vulnerabili;
- suite di test ancora limitata a sessantatré casi;
- smoke visivo automatico a larghezza mobile non eseguito per un errore del plugin browser sul percorso Windows del profilo; flusso HTTP reale, viewport e CSS responsive sono stati verificati;
- credenziale admin predefinita presente in `Web/appsettings.json`; va rimossa dai file versionati prima della distribuzione;
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
