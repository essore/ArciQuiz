using Core.Enums;
using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class Manche
    {
        public int Id { get; set; }
        public int PartitaId { get; set; }
        public int Ordine { get; set; } = 1;
        public MancheStato Stato { get; set; } = MancheStato.Nuova;

        public int TempoRispostaSecondi { get; set; } = 20;   // tempo risposta standard
        public int PuntiBase { get; set; } = 2000;            // punti per risposta corretta
        public int MalusBase { get; set; } = 500;             // malus base per risposta errata
        public int Moltiplicatore { get; set; } = 1;          // moltiplicatore della manche
        public bool PenalitaErrore { get; set; } = true;      // se true: errore scala punti
        public int MaxAstensioni { get; set; } = 3;           // astensioni senza malus per manche

        public DateTime? DtInizioUtc { get; set; }
        public DateTime? DtFineUtc { get; set; }

        public Partita? Partita { get; set; }
        public ICollection<MancheDomanda> Domande { get; set; } = new List<MancheDomanda>();
    }
}

