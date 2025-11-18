using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{

    public class MancheRispostaRicevuta
    {
        public int Id { get; set; }

        public int PlayerId { get; set; }
        public int MancheDomandaId { get; set; }

        public DateTime DtRisposta { get; set; }
        public char Risposta { get; set; }      // 'A','B','C','D'
        public bool IsCorrect { get; set; }     // calcolato

        public Player? Player { get; set; }
        public MancheDomanda? MancheDomanda { get; set; }
    }
}
