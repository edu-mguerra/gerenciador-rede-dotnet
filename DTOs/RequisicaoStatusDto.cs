namespace GerenciadorRede.API.DTOs
{
    public class RequisicaoStatusDto
    {
        public string MACAddress { get; set; } = string.Empty;
        public bool Bloquear { get; set; }
    }
}