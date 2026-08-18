# Formato CSV delle domande

Il catalogo usa un CSV UTF-8 con intestazione obbligatoria e separatore `,`:

```text
Categoria,Difficolta,Testo,RispostaA,RispostaB,RispostaC,RispostaD,RispostaEsatta,FlagErrore
```

I valori che contengono virgole, virgolette o ritorni a capo sono racchiusi tra virgolette; una virgolette interna è raddoppiata. `RispostaEsatta` accetta `A`, `B`, `C` o `D`; `FlagErrore` accetta `True` o `False`.

Le categorie ammesse sono: Cultura generale, Storia, Geografia, Scienza e natura, Letteratura, Arte, Cinema e TV, Musica, Sport, Attualità, Territorio e associazione, Bambini, Altro. Le difficoltà ammesse sono: Facile, Media, Difficile.

L'importazione valida tutte le righe prima di salvare: se sono presenti errori, nessuna domanda viene importata e la UI indica numero di riga e motivo. L'export include tutte le domande non cancellate e può essere reimportato senza perdita dei campi indicati.
