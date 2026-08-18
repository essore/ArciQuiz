using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class Player
    {
        public int Id { get; set; }
        public int? PartitaId { get; set; }
        public string NomeSquadra { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string? SessionToken { get; set; }
        public DateTime DtIscrizioneUtc { get; set; } = DateTime.UtcNow;
        public DateTime? DtUltimoAccessoUtc { get; set; }

        public Partita? Partita { get; set; }
        public ICollection<PlayerPartita> IscrizioniPartite { get; set; } = new List<PlayerPartita>();
        public ICollection<MancheRispostaRicevuta> Risposte { get; set; } = new List<MancheRispostaRicevuta>();
    }
}

