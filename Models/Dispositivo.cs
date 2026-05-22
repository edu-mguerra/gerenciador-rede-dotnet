namespace GerenciadorRede.API.Models
{
    public class Dispositivo
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        //oq vai indentificar o aparelho(valor imutavel)
        public string MACAddress { get; set; } = string.Empty;

        //ip
        public string IPAddress { get; set; } = string.Empty;

        public string NomeAmigavel { get; set; } = string.Empty;

        public StatusDispositivo Status { get; set; } = StatusDispositivo.Offiline;

        public DateTime UltimoSinalVisto { get; set; } = DateTime.UtcNow;

    }
}
