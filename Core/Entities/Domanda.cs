using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class Domanda
    {
        public int Id { get; set; }
        public string Categoria { get; set; } = string.Empty;   // per filtro
        public string Difficolta { get; set; } = string.Empty;  // per filtro
        public string Testo { get; set; } = string.Empty;
        public string RispostaA { get; set; } = string.Empty;
        public string RispostaB { get; set; } = string.Empty;
        public string RispostaC { get; set; } = string.Empty;
        public string RispostaD { get; set; } = string.Empty;
        public char RispostaEsatta { get; set; }  // 'A','B','C','D'
        public bool FlagErrore { get; set; } = false;
        public bool FlgDeleted { get; set; } = false;
        public ICollection<MancheDomanda> Manches { get; set; } = new List<MancheDomanda>();
    }
}
