# Prodotto ArciQuiz

## Missione

Consentire a un'associazione culturale con budget ridotto di organizzare una serata quiz affidabile usando un solo PC Windows, un proiettore e gli smartphone delle squadre collegati alla stessa rete Wi-Fi, anche senza accesso a Internet.

Il carico ordinario è circa 30 squadre; il sistema deve essere progettato per un massimo di 50 squadre connesse.

## Prima release

La prima release deve coprire un'intera serata:

1. preparazione di partita, manche e domande;
2. iscrizione delle squadre;
3. conduzione manuale delle domande;
4. raccolta delle risposte entro un timer;
5. calcolo di punti, malus e astensioni;
6. visualizzazione di soluzione, distribuzione delle risposte e classifiche;
7. conclusione con podio e vincitore;
8. conservazione dello storico e ripresa dopo riavvio.

Non sono obiettivi della prima release:

- identità persistente delle squadre tra serate;
- più partite attive contemporaneamente;
- vista spettatore su smartphone;
- domande diverse da quattro opzioni con una sola risposta corretta;
- cloud o dipendenza da servizi Internet;
- meccanismo automatico dedicato agli spareggi.

## Ruoli e dispositivi

### Admin/presentatore

È normalmente la stessa persona e usa un monitor privato sul PC di regia. Può:

- creare e configurare la partita;
- gestire domande e manche;
- iscrivere o correggere squadre;
- vedere e reimpostare le credenziali delle squadre;
- avviare partita, manche e singole domande;
- mostrare o nascondere la classifica quando consentito;
- annullare una domanda errata;
- terminare manche e partita.

La dashboard non deve essere accessibile anonimamente. La credenziale iniziale viene configurata localmente e non deve essere versionata.

### Squadra

Usa un solo smartphone attivo. Si iscrive tramite QR oppure viene inserita dall'admin.

- Il nome deve essere univoco nella partita.
- La squadra sceglie nome e password.
- Nella prima release la password è recuperabile dall'admin per assistere bambini e persone poco pratiche. Questo rischio è accettato per il contesto locale e amichevole.
- Un nuovo login valido invalida la sessione precedente e diventa l'unico dispositivo autorizzato a rispondere.
- La squadra non mantiene una propria identità tra partite diverse.

### Proiettore

È una vista pubblica controllata dalla regia. Non espone comandi amministrativi.

## Preparazione della partita

Prima della serata l'admin:

1. crea una nuova partita;
2. decide il numero di manche;
3. configura le regole di ogni manche;
4. prepara le domande tramite form o importazione CSV;
5. associa e ordina le domande nelle manche;
6. verifica che ogni domanda sia valida;
7. porta la partita nello stato `Pronta`.

Una partita può essere pronta solo se ha almeno una manche e ogni manche giocabile contiene almeno una domanda valida.

## Catalogo domande

Nella prima release ogni domanda contiene:

- testo;
- quattro opzioni A/B/C/D;
- una sola risposta corretta;
- categoria;
- difficoltà;
- indicatore di revisione/errore.

Le difficoltà disponibili sono:

- Facile;
- Media;
- Difficile.

Le categorie iniziali sono:

- Cultura generale;
- Storia;
- Geografia;
- Scienza e natura;
- Letteratura;
- Arte;
- Cinema e TV;
- Musica;
- Sport;
- Attualità;
- Territorio e associazione;
- Bambini;
- Altro.

L'importazione e l'esportazione delle domande usano CSV con intestazioni documentate e validazione riga per riga. Una riga non valida non deve essere importata silenziosamente.

## Ciclo della serata

### Fase 0 — Lobby e iscrizioni

Il proiettore mostra:

- titolo della partita;
- QR diretto alla registrazione;
- numero delle squadre iscritte.

Le squadre possono registrarsi autonomamente. Dopo l'avvio della partita la registrazione pubblica è chiusa, ma l'admin può inserire manualmente una squadra in ritardo.

### Fase 1 — Domanda aperta

Il presentatore avvia manualmente ogni domanda.

Il proiettore mostra testo, quattro risposte e conto alla rovescia. Lo smartphone mostra le stesse informazioni con quattro pulsanti selezionabili.

- Il timer è autorevole sul server.
- Ogni manche ha una durata standard.
- Una singola domanda può avere più tempo, per esempio per facilitare i bambini.
- La prima risposta valida viene confermata e non può essere cambiata.
- Non viene mostrato in tempo reale quante squadre hanno già risposto.

### Fase 2 — Chiusura e soluzione

Allo scadere del timer il server rifiuta nuove risposte e registra come astenute le squadre senza risposta.

Il proiettore mostra:

- risposta corretta;
- distribuzione aggregata delle risposte A/B/C/D.

Lo smartphone mostra la soluzione e il feedback personale: corretto, errato oppure astenuto.

### Fase 3 — Decisione della regia

Dopo il calcolo del punteggio il presentatore sceglie se:

- avviare la domanda successiva;
- mostrare la classifica parziale.

### Fase 4 — Fine manche

La classifica parziale viene mostrata obbligatoriamente. Le astensioni disponibili si azzerano per la manche successiva. Il presentatore avvia manualmente la manche seguente.

### Fase 5 — Fine partita

Il proiettore mostra:

- classifica finale;
- podio delle prime tre squadre;
- squadra o squadre vincitrici.

In caso di parità la prima release può mostrare un ex aequo. Il presentatore può gestire uno spareggio fuori dal flusso automatico oppure aggiungere una domanda prima di chiudere la partita.

## Regole di punteggio

Ogni manche configura:

- punti base per risposta corretta;
- malus base per risposta errata, predefinito a 500 punti;
- moltiplicatore della manche;
- durata standard;
- numero massimo di astensioni senza malus.

Una domanda può configurare un moltiplicatore aggiuntivo e una durata diversa da quella standard della manche.

### Risposta corretta

Il coefficiente tempo varia linearmente da `1,0` per una risposta immediata a `0,5` allo scadere:

```text
coefficienteTempo = 0,5 + 0,5 × tempoRimanente / tempoTotale
punti = puntiBase × moltiplicatoreManche × moltiplicatoreDomanda × coefficienteTempo
```

Il risultato viene arrotondato a un intero con una regola deterministica unica per tutti i client.

### Risposta errata

```text
malus = -malusBase × moltiplicatoreManche × moltiplicatoreDomanda
```

Il moltiplicatore si applica quindi sia ai punti positivi sia ai malus.

### Astensione

- Finché la squadra non supera `MaxAstensioni` nella manche: zero punti.
- Ogni astensione successiva: stesso malus di una risposta errata.
- Il conteggio riparte da zero in ogni manche.

## Annullamento di una domanda

Se una domanda è errata, l'admin può annullare la specifica occorrenza giocata nella manche.

L'annullamento deve:

- rimuovere tutti i punti, malus e consumi di astensione prodotti da quella occorrenza;
- ricalcolare le classifiche;
- conservare traccia dell'annullamento;
- segnalare la domanda del catalogo come da revisionare;
- non cancellare automaticamente la domanda dal catalogo.

L'operazione deve essere idempotente: annullare due volte la stessa occorrenza non cambia nuovamente il punteggio.

## Persistenza e affidabilità

- Dati, stato della partita, timer e risposte devono risiedere sul PC di regia.
- Una partita deve poter riprendere dopo la chiusura o il riavvio dell'applicazione.
- Lo storico delle partite e dei punteggi deve essere conservato.
- L'esportazione CSV delle domande è richiesta.
- Cloud e sincronizzazione remota sono fuori dalla prima release.
- Backup completo ed esportazione della classifica non sono ancora requisiti confermati.

## Requisiti non funzionali

- Funzionamento senza Internet dopo installazione e configurazione.
- Esperienza di avvio adatta a una persona non tecnica.
- Supporto a 50 squadre connesse senza perdita o duplicazione delle risposte.
- Interfaccia in italiano.
- Sicurezza di base coerente con una LAN amichevole, senza lasciare la dashboard admin pubblica.
- Nessun segreto amministrativo nei file versionati o nei log.

