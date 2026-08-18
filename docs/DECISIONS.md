# Registro delle decisioni

Questo documento contiene decisioni durevoli. Non è un diario e non descrive lo stato di implementazione.

| ID | Data | Decisione | Conseguenza |
|---|---|---|---|
| D-001 | 2026-07-10 | ArciQuiz è destinato ad associazioni culturali low budget. | Soluzioni semplici, locali e mantenibili hanno precedenza su infrastrutture sofisticate. |
| D-002 | 2026-07-10 | Target normale 30 squadre, massimo progettuale 50. | Testare concorrenza e UX fino a 50 dispositivi squadra. |
| D-003 | 2026-07-10 | PC regia, proiettore e smartphone operano nella stessa Wi-Fi, anche senza Internet. | Nessuna dipendenza runtime da cloud, CDN o API Internet. |
| D-004 | 2026-07-10 | Admin e presentatore sono la stessa persona nella prima release. | Un'unica dashboard privata può coprire preparazione e conduzione. |
| D-005 | 2026-07-10 | È consentita una sola partita attiva. | Stato runtime e UI possono ottimizzare questo vincolo, conservando lo storico delle concluse. |
| D-006 | 2026-07-10 | Registrazione via QR/form o inserimento admin; dopo l'avvio solo l'admin può aggiungere ritardatari. | La registrazione pubblica si chiude all'avvio. |
| D-007 | 2026-07-10 | Il nome squadra è univoco nella partita e l'identità non persiste tra serate. | Vincolo univoco per partita; nessun account globale squadra. |
| D-008 | 2026-07-10 | Un solo telefono attivo per squadra; l'ultimo login invalida il precedente. | Serve una sessione esclusiva persistente. |
| D-009 | 2026-07-10 | Password squadra leggibile dall'admin nella prima release. | Rischio accettato per assistenza a bambini e anziani; non applicare alla credenziale admin e non scrivere password nei log. |
| D-010 | 2026-07-10 | Dashboard protetta da autenticazione locale di base. | Credenziale configurata localmente e non versionata. |
| D-011 | 2026-07-10 | Solo domande A/B/C/D con una risposta corretta. | Altri tipi di domanda sono fuori dalla prima release. |
| D-012 | 2026-07-10 | Ogni domanda parte manualmente; timer standard per manche con override più lungo per singola domanda. | Il server governa inizio e scadenza; la UI non avanza automaticamente alla domanda successiva. |
| D-013 | 2026-07-10 | La prima risposta confermata non può cambiare. | Registrazione idempotente e vincolo una risposta per squadra/domanda. |
| D-014 | 2026-07-10 | Punti corretti dal 100% al 50% in modo lineare rispetto al tempo; malus base predefinito 500. | Formula unica server-side, moltiplicatori applicati a punti e malus. |
| D-015 | 2026-07-10 | Astensioni gratuite fino a `MaxAstensioni` per manche, poi stesso malus dell'errore. | Conteggio azzerato a ogni manche e ripristinabile in caso di annullamento. |
| D-016 | 2026-07-10 | Nessun conteggio live delle squadre che hanno risposto; distribuzione A/B/C/D mostrata dopo la chiusura. | Riduce complessità live e influenza sui partecipanti. |
| D-017 | 2026-07-10 | Classifica opzionale dopo una domanda, obbligatoria a fine manche e finale con podio. | La regia decide le classifiche intermedie; parità visualizzabile ex aequo. |
| D-018 | 2026-07-10 | Una domanda errata viene annullata nella specifica manche e il punteggio viene ricalcolato. | Conservare audit, neutralizzare anche astensioni e malus e segnalare il catalogo per revisione. |
| D-019 | 2026-07-10 | Nessun motore dedicato agli spareggi nella prima release. | L'ex aequo è valido; eventuale domanda extra viene gestita manualmente prima della chiusura. |
| D-020 | 2026-07-10 | Import ed export domande in CSV; categorie e difficoltà da elenchi. | Formato stabile, validato e adatto a strumenti economici. |
| D-021 | 2026-07-10 | Storico partite e punteggi conservato; partita recuperabile dopo riavvio. | Il database è fonte di verità, non il singleton in memoria. |
| D-022 | 2026-07-10 | Prima release locale Windows con SQLite e senza cloud. | Packaging e posizione dati devono essere comprensibili a utenti non tecnici. |
| D-023 | 2026-07-10 | Nessuna vista spettatore nella prima release. | Le sole viste pubbliche sono registrazione, client squadra autenticato e proiettore. |
| D-024 | 2026-07-10 | Il proprietario sceglie il branch; agenti senza push e senza decisioni funzionali inventate. | L'agente lavora sul branch corrente, marca `BLOCKED` quando serve e non crea commit salvo richiesta. |
| D-025 | 2026-07-10 | Ogni sessione autonoma completa un solo task `READY`. | TODO ordinato, stato e worklog aggiornati prima di fermarsi. |
| D-026 | 2026-08-18 | Lo stato `Pronta` rappresenta la lobby pubblica. | La registrazione pubblica è consentita solo per partite `Pronta` e si chiude al passaggio a `InCorso`; l'admin può comunque inserire squadre ritardatarie. |

## Decisioni aperte non bloccanti

- Necessità di esportare classifiche e risultati oltre alla consultazione dello storico.
- Necessità di un comando di backup/ripristino oltre alla copia documentata del database locale.
- Eventuale correzione del nome progetto `Infrasctructure`, da valutare solo quando non crea rumore rispetto alle priorità funzionali.
