using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{

    public class MancheDomanda
    {
        public int Id { get; set; }

        public int MancheId { get; set; }
        public int? DomandaId { get; set; }  // se 0/null = mostra classifica

        public int Index { get; set; }           // ordine
        public int ModificatorePunti { get; set; } = 1; // es. x3 su ultima domanda

        public DateTime? DtStart { get; set; }
        public DateTime? DtEnd { get; set; }

        public Manche? Manche { get; set; }
        public Domanda? Domanda { get; set; }

        public ICollection<MancheRispostaRicevuta> Risposte { get; set; } = new List<MancheRispostaRicevuta>();
    }
}
