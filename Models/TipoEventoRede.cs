namespace GerenciadorRede.API.Models
{
    //rastreia oq aconteceu na rede, para alimentar a linha de tempoo
    public enum TipoEventoRede
    {
        Conectou = 0,
        Desconectou = 1,
        BloqueadoPeloAdmin = 2,
        LiberadoPeloAdmin = 3,
        Navegacao = 4,
    }
}
