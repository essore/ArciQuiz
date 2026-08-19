# Stato del progetto

Ultima verifica: **2026-08-19**  
Commit ispezionato prima della documentazione: `3b476a5` (`master`)

Stadio: **prototipo compilabile con baseline di test (99 casi), partita attiva persistita e navigazione admin esplicita (AQ-044 completato)**

## Baseline verificata

Comandi eseguiti:

```powershell
dotnet test ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false
dotnet build ArciQuiz.slnx --no-restore -m:1 /p:UseSharedCompilation=false
```

Esito: test riusciti (99 casi, inclusi calcolo coefficiente tempo, punteggi con moltiplicatori, malus, registrazione automatica astensioni con soglia per manche, idempotenza, routing/sessione squadra, invio risposta, soluzione/distribuzione, classifiche, annullamento, macchina a stati, recupero su SQLite riaperto e partita attiva) e build riuscita, 0 errori e 3 avvisi noti.

Completato `AQ-045`: la dashboard espone direttamente l'elenco partite e il comando `Nuova partita`, senza duplicare collegamenti prototipali; la gestione domande resta nel menu admin protetto. `AQ-046` è ora il task autorizzato in coda (`READY`); `AQ-041` resta pianificato dopo i task funzionali.

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
- invio autorevole della prima risposta A/B/C/D, idempotente anche in caso di invii concorrenti e rifiutato per sessione sostituita o timer scaduto;
- calcolo deterministico e persistito dei punteggi, malus, coefficiente di velocità e astensioni automatiche con gestione della soglia per manche;
- soluzione sul proiettore con distribuzione risposte A/B/C/D e feedback personale con variazione punti sul telefono;
- classifica parziale, di fine manche e finale con podio ed ex aequo;
- annullamento tracciato della singola occorrenza giocata con ricalcolo di punteggi, astensioni e classifiche;
- selezione persistita di una sola partita attiva, con indice SQLite che impedisce attivazioni concorrenti;
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

- pagine e codice demo del template ancora presenti;
- UI e logica dati concentrate nei componenti Razor.

## Funzioni mancanti per la prima release

- storico partite e punteggi;
- packaging e avvio Windows per utenti non tecnici;
- CI.

## Debito e rischi noti

- dipendenze transitive vulnerabili;
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
