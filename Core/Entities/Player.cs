using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class Player
    {
        public int Id { get; set; }
        public string NomeSquadra { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public ICollection<PlayerPartita> IscrizioniPartite { get; set; } = new List<PlayerPartita>();
    }
}
