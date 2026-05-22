
using System.Net;
using System.Net.NetworkInformation;
using GerenciadorRede.API.Data;
using GerenciadorRede.API.Models;
using Microsoft.EntityFrameworkCore;
using PacketDotNet;
using SharpPcap;

namespace GerenciadorRede.API.Services
{
    public class NetworkBlockerService : INetworkBlockerService
    {
        private readonly ILogger<NetworkBlockerService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private ILiveDevice? _interfaceRedeAtiva;

        public NetworkBlockerService(ILogger<NetworkBlockerService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            ConfigurarInterfaceRede();
        }

        private void ConfigurarInterfaceRede()
        {
            try
            {
                _interfaceRedeAtiva = CaptureDeviceList.Instance
                    .FirstOrDefault(d => d.MacAddress != null && !d.Description.Contains("Loopback"));

                if (_interfaceRedeAtiva != null)
                {
                    _logger.LogInformation("SharpPcap associado com sucesso à interface: {Descricao}", _interfaceRedeAtiva.Description);
                }
                else
                {
                    _logger.LogWarning("Nenhuma interface de rede física foi encontrada pelo SharpPcap.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar dispositivos de captura do SharpPcap.");
            }
        }

        public async Task IniciarMotorBloqueioAsync(CancellationToken stoppingToken)
        {
            if (_interfaceRedeAtiva == null) return;

            _logger.LogInformation("Motor de Bloqueio ARP iniciado nos bastidores.");

            try
            {
                _interfaceRedeAtiva.Open(DeviceModes.Promiscuous, 1000);

                while (!stoppingToken.IsCancellationRequested)
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        var alvosParaBloquear = await dbContext.Dispositivos
                            .Where(d => d.Status == StatusDispositivo.Bloqueado)
                            .ToListAsync(stoppingToken);

                        if (alvosParaBloquear.Any())
                        {
                            var ipGateway = IPAddress.Parse("192.168.250.1");
                            var macGateway = PhysicalAddress.Parse("00-00-00-00-00-00");

                            var macLocal = _interfaceRedeAtiva.MacAddress;
                            if (macLocal == null) continue;

                            foreach (var alvo in alvosParaBloquear)
                            {
                                if (alvo.IPAddress != null && alvo.MACAddress != null &&
                                    IPAddress.TryParse(alvo.IPAddress, out var ipAlvo) &&
                                    PhysicalAddress.TryParse(alvo.MACAddress.Replace(":", "-"), out var macAlvo))
                                {
                                    _logger.LogWarning("🛑 Injetando pacote de bloqueio ARP -> Alvo: {IP}", alvo.IPAddress);

                                    EnviarPacoteArpFalso(ipGateway, macLocal, ipAlvo, macAlvo);
                                    EnviarPacoteArpFalso(ipAlvo, macLocal, ipGateway, macGateway);
                                }
                            }
                        }
                    }

                    await Task.Delay(2000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha crítica no loop do motor de bloqueio.");
            }
            finally
            {
                _interfaceRedeAtiva?.Close();
            }
        }

        private void EnviarPacoteArpFalso(IPAddress ipOrigem, PhysicalAddress macOrigem, IPAddress ipDestino, PhysicalAddress macDestino)
        {
            if (_interfaceRedeAtiva == null) return;

            try
            {
                var arpPacket = new ArpPacket(ArpOperation.Response, macDestino, ipDestino, macOrigem, ipOrigem);

                var ethernetPacket = new EthernetPacket(macOrigem, macDestino, EthernetType.Arp)
                {
                    PayloadData = arpPacket.Bytes
                };

                _interfaceRedeAtiva.SendPacket(ethernetPacket);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Erro ao injetar frame ARP na rede: {Msg}", ex.Message);
            }
        }
    }
}