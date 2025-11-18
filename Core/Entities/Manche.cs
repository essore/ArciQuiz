using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{

    public class Manche
    {
        public int Id { get; set; }
        public int PartitaId { get; set; }
        public MancheStato Stato { get; set; } = MancheStato.Nuova;

        public int TempoRispostaSecondi { get; set; } = 20;   // tempo risposta
        public int PuntiBase { get; set; } = 2000;            // punti per risposta
        public bool PenalitaErrore { get; set; } = false;     // se true: errore scala punti
        public int MaxAstensioni { get; set; } = 3;           // se penalità = true

        public Partita? Partita { get; set; }
        public ICollection<MancheDomanda> Domande { get; set; } = new List<MancheDomanda>();
    }
}
