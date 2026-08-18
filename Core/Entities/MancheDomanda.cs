using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class MancheDomanda
    {
        public int Id { get; set; }

        public int MancheId { get; set; }
        public int? DomandaId { get; set; }  // se 0/null = mostra classifica / pausa

        public int Index { get; set; }           // ordine
        public int ModificatorePunti { get; set; } = 1; // es. x2 o x3 su ultima domanda
        public int? DurataSecondiOverride { get; set; } // override durata; se null usa Manche.TempoRispostaSecondi

        public DateTime? DtStart { get; set; }
        public DateTime? DtEnd { get; set; }
        public DateTimeOffset? ScadenzaUtc { get; set; } // scadenza server-authoritative del timer

        public bool IsAnnullata { get; set; } = false;   // true se l'occorrenza è stata annullata dall'admin (D-018)
        public string? MotivoAnnullamento { get; set; }  // audit annullamento
        public DateTime? DtAnnullamentoUtc { get; set; } // timestamp annullamento

        public Manche? Manche { get; set; }
        public Domanda? Domanda { get; set; }

        public ICollection<MancheRispostaRicevuta> Risposte { get; set; } = new List<MancheRispostaRicevuta>();
    }
}

