using System.Collections.Generic;
using System.Threading.Tasks;
using GerenciadorRede.API.Models;

namespace GerenciadorRede.API.Services
{
    public interface INetworkScannerService
    {
        Task<List<Dispositivo>> EscanearRedeAsync();
    }
}