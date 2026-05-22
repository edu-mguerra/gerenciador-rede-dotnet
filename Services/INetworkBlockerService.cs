using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorRede.API.Services
{
    public interface INetworkBlockerService
    {
        Task IniciarMotorBloqueioAsync(CancellationToken stoppingToken);
    }
}