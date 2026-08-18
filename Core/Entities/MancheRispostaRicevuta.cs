using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class MancheRispostaRicevuta
    {
        public int Id { get; set; }

        public int PlayerId { get; set; }
        public int MancheDomandaId { get; set; }

        public DateTime DtRisposta { get; set; } = DateTime.UtcNow;
        public char? Risposta { get; set; }     // 'A','B','C','D' oppure null se astenuto
        public bool IsAstenuto { get; set; } = false;
        public bool IsCorrect { get; set; }     // calcolato
        public int PuntiAssegnati { get; set; } // punteggio netto (+ punti corretti, - malus o penalità astensione, 0 astensione gratuita)
        public int? TempoImpiegatoMs { get; set; } // tempo di risposta in ms
        public double? CoefficienteTempo { get; set; } // coefficiente tempo applicato (tra 0.5 e 1.0)
        public bool AstensionePenalizzata { get; set; } = false; // true se astensione oltre soglia MaxAstensioni
        public bool IsAnnullata { get; set; } = false; // true se la risposta è neutralizzata da annullamento domanda

        public Player? Player { get; set; }
        public MancheDomanda? MancheDomanda { get; set; }
    }
}

