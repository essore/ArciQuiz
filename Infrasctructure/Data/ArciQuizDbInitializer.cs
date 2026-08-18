using System;
using System.Linq;
using Core.Entities;

namespace Infrasctructure.Data;

public static class ArciQuizDbInitializer
{
    public static void Seed(ArciQuizDbContext db)
    {
        // se c'è già almeno una partita, non facciamo nulla
        if (db.Partite.Any())
            return;

        // 1) Partita
        var partita = new Partita
        {
            Titolo = "ArciQuiz Demo Night"
        };

        // 2) Manche
        var manche = new Manche
        {
            Partita = partita,
            TempoRispostaSecondi = 20,
            PuntiBase = 2000,
            PenalitaErrore = false,
            MaxAstensioni = 3
        };

        // 3) Domande
        var domanda1 = new Domanda
        {
            Categoria = "Cultura generale",
            Difficolta = "Facile",
            Testo = "In che regione si trovano le Marche?",
            RispostaA = "Nel nord Italia",
            RispostaB = "Nel centro Italia",
            RispostaC = "Nel sud Italia",
            RispostaD = "Sono un'isola",
            RispostaEsatta = 'B'
        };

        var domanda2 = new Domanda
        {
            Categoria = "Cinema e TV",
            Difficolta = "Media",
            Testo = "Chi ha diretto il film 'La vita è bella'?",
            RispostaA = "Paolo Sorrentino",
            RispostaB = "Federico Fellini",
            RispostaC = "Roberto Benigni",
            RispostaD = "Giuseppe Tornatore",
            RispostaEsatta = 'C'
        };

        var domanda3 = new Domanda
        {
            Categoria = "Musica",
            Difficolta = "Facile",
            Testo = "Quale di questi è uno strumento a corde?",
            RispostaA = "Flauto",
            RispostaB = "Violino",
            RispostaC = "Tromba",
            RispostaD = "Batteria",
            RispostaEsatta = 'B'
        };

        // 4) Collegare le domande alla manche con ordine
        var md1 = new MancheDomanda
        {
            Manche = manche,
            Domanda = domanda1,
            Index = 1,
            ModificatorePunti = 1
        };

        var md2 = new MancheDomanda
        {
            Manche = manche,
            Domanda = domanda2,
            Index = 2,
            ModificatorePunti = 1
        };

        var md3 = new MancheDomanda
        {
            Manche = manche,
            Domanda = domanda3,
            Index = 3,
            ModificatorePunti = 2   // ultima vale doppio
        };

        // 5) Una squadra demo
        var player = new Player
        {
            NomeSquadra = "Gli Infallibili",
            Password = "demo",
            Partita = partita
        };

        var iscrizione = new PlayerPartita
        {
            Player = player,
            Partita = partita
        };

        // 6) Aggiungere tutto al context
        db.Partite.Add(partita);
        db.Manches.Add(manche);
        db.Domande.AddRange(domanda1, domanda2, domanda3);
        db.ManchesDomande.AddRange(md1, md2, md3);
        db.Players.Add(player);
        db.PlayersPartite.Add(iscrizione);

        db.SaveChanges();
    }
}
