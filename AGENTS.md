# AGENTS.md

## Scopo

Questo file è il contratto operativo per gli agenti che lavorano su ArciQuiz.
Si applica all'intero repository, salvo la presenza futura di un `AGENTS.md` più specifico in una sottocartella.

ArciQuiz è un progetto hobby per associazioni culturali con budget ridotto. Deve gestire serate quiz in una rete Wi-Fi locale, potenzialmente senza Internet, con circa 30 squadre e un massimo progettuale di 50.

## Fonti di verità e ordine di lettura

All'inizio di ogni sessione autonoma:

1. eseguire `git status --short` e non sovrascrivere modifiche non proprie;
2. leggere questo file;
3. leggere `docs/PROJECT_STATE.md`;
4. leggere `docs/TODO.md`;
5. leggere `docs/PRODUCT.md` per le regole funzionali;
6. leggere `docs/ARCHITECTURE.md` se il task coinvolge codice, dati o runtime;
7. leggere `docs/DECISIONS.md` se serve comprendere una scelta già presa;
8. consultare le implementazioni più vicine al codice da modificare.

In caso di conflitto valgono, in ordine:

1. richiesta esplicita più recente del proprietario;
2. comportamento verificato da test;
3. decisioni registrate in `docs/DECISIONS.md`;
4. specifica di prodotto in `docs/PRODUCT.md`;
5. codice corrente;
6. altre note descrittive.

Se il codice diverge dalla specifica, non assumere che il codice rappresenti il comportamento desiderato: registrare la divergenza e applicare il task assegnato.

## Protocollo per una sessione autonoma

Quando il proprietario avvia un agente senza assegnare un task specifico:

1. selezionare il primo task `READY` in `docs/TODO.md`, rispettando l'ordine del file;
2. modificare subito il suo stato in `IN_PROGRESS`;
3. completare un solo task;
4. applicare la modifica minima sufficiente;
5. eseguire le verifiche richieste dal task;
6. controllare il diff e lo stato Git;
7. aggiornare il task a `DONE` oppure `BLOCKED`, valutando prima se un test umano residuo blocca davvero i task successivi;
8. aggiornare `docs/PROJECT_STATE.md` solo se lo stato reale del progetto è cambiato;
9. aggiungere una voce a `docs/WORKLOG.md`;
10. promuovere a `READY` il primo task `PLANNED` le cui dipendenze sono tutte concluse;
11. fermarsi.

Deve esserci al massimo un task `READY` e un task `IN_PROGRESS`.

Se non esistono task `READY`, non scegliere autonomamente un lavoro diverso. Segnalare che la coda richiede una decisione del proprietario.

Se il proprietario assegna un task esplicito, quel task ha precedenza sulla coda. Aggiornare comunque TODO, stato e diario quando il lavoro corrisponde a un elemento del piano.

## Stati dei task

- `PLANNED`: definito ma non ancora eseguibile o non ancora selezionato.
- `READY`: prossimo task autorizzato ed eseguibile.
- `IN_PROGRESS`: task preso in carico nella sessione corrente.
- `BLOCKED`: impossibile procedere senza una decisione o una dipendenza esterna.
- `DONE`: criteri di accettazione soddisfatti e verifiche registrate.

Un task non può essere dichiarato `DONE` se la build o le verifiche richieste falliscono per effetto della modifica.

## Test umani residui

`TestUmano` è una dicitura di `docs/TODO.md`, non uno stato del task. Viene usata solo quando sviluppo e verifiche automatiche pertinenti sono riusciti, ma una prova manuale concreta non è eseguibile nell'ambiente dell'agente o richiede un dispositivo, un browser o un contesto fisico disponibile al proprietario.

Prima di segnare un task `BLOCKED` per una verifica manuale mancante, valutare esplicitamente se il suo esito è necessario per proseguire in sicurezza con i task dipendenti:

1. se può invalidare una regola funzionale, un contratto dati, la sicurezza o una base tecnica usata dai task successivi, il task resta `BLOCKED`;
2. se verifica soltanto una resa o un'integrazione locale già coperta da test automatici e non condiziona i task successivi, il task può essere `DONE` con la voce `TestUmano: DA ESEGUIRE`;
3. la voce deve indicare passi mirati, risultato atteso, contesto necessario e rischio residuo;
4. dopo il collaudo, sostituire la voce con `TestUmano: ESEGUITO` e registrare l'esito nel diario.

Quando viene chiesto un riepilogo, cercare tutte le voci `TestUmano: DA ESEGUIRE` e riportare soltanto i test ancora aperti con i rispettivi passi manuali. Un task `DONE` con questa dicitura soddisfa la Definition of Done perché il test residuo è stato classificato esplicitamente non bloccante.

## Regole di implementazione

- Correttezza prima di leggibilità, leggibilità prima di minimizzazione del diff.
- Preferire comunque modifiche chirurgiche e non rifattorizzare aree estranee al task.
- Seguire il pattern più vicino nel repository prima di introdurre nuove astrazioni.
- Non aggiungere librerie se il framework o una dipendenza già presente risolvono il problema in modo adeguato.
- Mantenere la logica di dominio fuori dai componenti Razor quando il task introduce nuove regole riutilizzabili o testabili.
- Non esporre la soluzione corretta ai client prima della chiusura della domanda.
- Il server è autorevole per timer, accettazione delle risposte, punteggio e stato della partita.
- Rendere idempotente l'invio della risposta: una squadra può registrare al massimo una risposta per domanda.
- Non modificare retroattivamente i risultati senza una funzione esplicita e tracciabile, come l'annullamento di una domanda.
- Non ampliare il livello di sicurezza oltre quanto richiesto, ma non inserire credenziali admin, segreti o dati personali nei file versionati o nei log.

## Vincoli di prodotto da non reinterpretare

- Prima release solo con domande A/B/C/D e una risposta corretta.
- Una sola partita attiva.
- Un solo dispositivo attivo per squadra; il login più recente sostituisce il precedente.
- Una risposta confermata non è modificabile.
- Ogni domanda viene avviata manualmente dal presentatore.
- Il tempo standard appartiene alla manche; una domanda può avere un'estensione specifica.
- Le iscrizioni tardive sono consentite tramite intervento dell'admin.
- La password delle squadre è volutamente recuperabile dall'admin nella prima release. È un rischio accettato per il contesto d'uso; non estendere questa scelta alle credenziali amministrative.
- Nessuna vista spettatore nella prima release.
- Il sistema deve funzionare su un PC Windows in LAN senza dipendere da Internet.

Le regole complete sono in `docs/PRODUCT.md`.

## Gestione delle ambiguità

Non inventare una regola che incide su comportamento pubblico, punteggi, dati persistenti, sicurezza o recupero di una partita.

Se la risposta non è ricavabile dalle fonti di verità:

1. applicare solo eventuali parti indipendenti e sicure del task;
2. impostare il task a `BLOCKED`;
3. annotare nel task la domanda precisa e le alternative concrete;
4. registrare il blocco in `docs/WORKLOG.md`;
5. fermarsi.

Non usare `BLOCKED` per difficoltà tecniche risolvibili con analisi, test o documentazione ufficiale.

## Validazione

Per modifiche .NET, la verifica minima generale è:

```powershell
dotnet test ArciQuiz.slnx
dotnet build ArciQuiz.slnx
```

Quando esistono test pertinenti, eseguirli prima della build completa. Ogni task può richiedere verifiche aggiuntive.

Riportare soltanto comandi realmente eseguiti. Se una verifica non è possibile, indicare motivo e rischio residuo; applicare il protocollo `TestUmano` quando lo sviluppo è completo e la verifica non è essenziale per i task successivi.

## Git

- Lavorare sul branch già predisposto dal proprietario.
- Non creare e non cambiare branch autonomamente.
- Non eseguire push.
- Non creare commit salvo richiesta esplicita del proprietario.
- Non includere nel task modifiche preesistenti o non correlate.
- Non usare comandi distruttivi per ripulire il worktree.

## Aggiornamento della documentazione

- `docs/TODO.md`: stato operativo e criteri di accettazione dei task.
- `docs/PROJECT_STATE.md`: fotografia sintetica e verificata del sistema.
- `docs/WORKLOG.md`: registro append-only di ciò che è stato fatto e verificato.
- `docs/DECISIONS.md`: decisioni durevoli; non usarlo come diario.
- `docs/PRODUCT.md`: regole funzionali; modificarlo solo dopo una decisione del proprietario.
- `docs/ARCHITECTURE.md`: architettura effettiva o obiettivo già approvato.

Evitare di duplicare la stessa informazione in più documenti. Usare collegamenti quando possibile.

## Definition of Done

Un task è concluso solo quando:

- soddisfa tutti i criteri di accettazione;
- il diff è limitato al task;
- le verifiche previste hanno esito positivo;
- sono considerati almeno il caso nominale e i casi limite indicati;
- TODO e WORKLOG sono aggiornati;
- PROJECT_STATE è aggiornato se necessario;
- non sono rimasti commenti temporanei, segreti o artefatti generati non intenzionali.
