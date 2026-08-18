using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class PlayerPartita
    {
        public int PlayerId { get; set; }
        public int PartitaId { get; set; }

        public string? SessionToken { get; set; }
        public DateTime DtIscrizioneUtc { get; set; } = DateTime.UtcNow;
        public DateTime? DtUltimoAccessoUtc { get; set; }

        public Player? Player { get; set; }
        public Partita? Partita { get; set; }
    }
}

