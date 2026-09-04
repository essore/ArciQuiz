# Distribuire ArciQuiz su Windows

Questa procedura crea un pacchetto che funziona senza Internet durante la
serata. Internet e il SDK .NET 10 servono soltanto sul PC usato per creare il
pacchetto.

## Creare il pacchetto

Dal repository, in PowerShell:

```powershell
.\scripts\Publish-Windows.ps1
```

Il comando produce `artifacts\ArciQuiz-windows` con un runtime .NET incluso.
Copiare l'intera cartella sul PC di regia; non spostare solo `ArciQuiz.exe`.
Per un PC Windows ARM usare:

```powershell
.\scripts\Publish-Windows.ps1 -RuntimeIdentifier win-arm64
```

## Configurare e avviare

1. Nella cartella pubblicata, copiare `appsettings.Local.example.json` con il
   nome `appsettings.Local.json`.
2. Aprire il nuovo file con Blocco note e impostare utente e password admin.
   Il file resta solo su quel PC e non va condiviso.
3. Fare doppio clic su `Avvia-ArciQuiz.cmd` e lasciare aperta la finestra nera
   durante la serata.
4. Sul PC di regia aprire `http://localhost:5000`; sul proiettore aprire la
   vista pubblica dalla regia. Il QR indica alle squadre l'indirizzo LAN.

L'app ascolta sulla porta HTTP `5000` di tutte le interfacce del PC. HTTP è
intenzionale: i telefoni della stessa Wi-Fi devono poter aprire il QR senza
certificati locali non attendibili.

## Scegliere la rete e il firewall

Prima di avviare, collegare PC, proiettore e telefoni alla stessa Wi-Fi e
impostare la rete Windows come **Privata**, non Pubblica. Consentire a
`ArciQuiz.exe` l'accesso alle reti private quando Windows Firewall lo chiede.
Se il messaggio non compare, creare una regola in ingresso per TCP porta 5000,
profilo Privato, limitata alla rete locale. Non aprire la porta sul profilo
Pubblico.

Normalmente il QR usa il primo indirizzo IPv4 privato disponibile. Se il PC ha
più reti attive (per esempio Wi-Fi e VPN) e il QR mostra l'indirizzo sbagliato,
disattivare temporaneamente l'interfaccia non usata oppure impostare nel file
`appsettings.Local.json` l'indirizzo scelto:

```json
{
  "Admin": {
    "Username": "admin",
    "Password": "una-password-locale"
  },
  "Lobby": {
    "PublicBaseUrl": "http://192.168.1.40:5000"
  }
}
```

Sostituire l'esempio con l'indirizzo IPv4 del PC di regia. Conservare porta e
protocollo coerenti con la configurazione dell'app.

## Conservare e ripristinare i dati

Il database è in `%LocalAppData%\ArciQuiz`. Per una copia sicura, arrestare
ArciQuiz chiudendo la sua finestra, poi copiare l'intera cartella `ArciQuiz`:
include il database e gli eventuali file `-wal`, `-shm` e `-journal`.

Per ripristinare una copia, arrestare ArciQuiz e sostituire l'intera cartella
`%LocalAppData%\ArciQuiz` con quella salvata. Non copiare solo il file `.db`
mentre l'app è in esecuzione.

## Collaudo prima della serata

Su una cartella pulita del PC di regia, configurare e avviare il pacchetto.
Aprire il QR da almeno due telefoni collegati alla stessa Wi-Fi, iscrivere due
squadre e verificare che entrambe arrivino alla pagina di gioco. Provare anche
un riavvio dell'app: il database e la partita devono restare disponibili.
