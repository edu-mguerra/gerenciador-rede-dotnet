using System;

namespace GerenciadorRede.API.Models
{
    public class HistoricoRede
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DispositivoId { get; set; }
        public TipoEventoRede TipoEvento { get; set; }
        public DateTime DataHora { get; set; } = DateTime.UtcNow;
        public string? IPDestino { get; set; }
        public int? PortaDestino { get; set; }
        public string? Protocolo { get; set; }
    }
}