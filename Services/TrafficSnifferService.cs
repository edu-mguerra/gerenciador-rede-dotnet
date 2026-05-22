using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GerenciadorRede.API.Data;
using GerenciadorRede.API.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PacketDotNet;
using SharpPcap;

namespace GerenciadorRede.API.Services
{
    public class TrafficSnifferService : BackgroundService
    {
        private readonly ILogger<TrafficSnifferService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private ILiveDevice? _device;
        private readonly ConcurrentDictionary<string, DateTime> _cacheAcessos = new();

        public TrafficSnifferService(ILogger<TrafficSnifferService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _device = CaptureDeviceList.Instance.FirstOrDefault(d => !d.Description.Contains("Loopback"));

            if (_device == null)
            {
                _logger.LogWarning("Sniffer: Nenhuma interface de rede encontrada.");
                return Task.CompletedTask;
            }

            _device.OnPacketArrival += Device_OnPacketArrival;
            _device.Open(DeviceModes.Promiscuous, 1000);
            _device.Filter = "ip and (tcp or udp)";
            _device.StartCapture();

            _logger.LogInformation("Motor de Sniffer ativado. Analisando trafego na porta {Descricao}", _device.Description);

            stoppingToken.Register(() =>
            {
                _device.StopCapture();
                _device.Close();
            });

            return Task.CompletedTask;
        }

        private void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            var rawPacket = e.GetPacket();
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            var ipv4Packet = packet.Extract<IPv4Packet>();

            if (ipv4Packet == null) return;

            string ipOrigem = ipv4Packet.SourceAddress.ToString();
            string ipDestino = ipv4Packet.DestinationAddress.ToString();

            if (ipDestino.StartsWith("192.168.") || ipDestino.StartsWith("239.") || ipDestino == "255.255.255.255") return;

            int portaDestino = 0;
            string protocolo = "TCP";

            var tcpPacket = packet.Extract<TcpPacket>();
            if (tcpPacket != null)
            {
                portaDestino = tcpPacket.DestinationPort;
            }
            else
            {
                var udpPacket = packet.Extract<UdpPacket>();
                if (udpPacket != null)
                {
                    portaDestino = udpPacket.DestinationPort;
                    protocolo = "UDP";
                }
            }

            if (portaDestino != 80 && portaDestino != 443 && portaDestino != 53) return;

            string cacheKey = $"{ipOrigem}-{ipDestino}";
            if (_cacheAcessos.TryGetValue(cacheKey, out DateTime ultimoAcesso) && (DateTime.UtcNow - ultimoAcesso).TotalSeconds < 60)
            {
                return;
            }

            _cacheAcessos[cacheKey] = DateTime.UtcNow;

            _ = SalvarHistoricoAsync(ipOrigem, ipDestino, portaDestino, protocolo);
        }

        private async Task SalvarHistoricoAsync(string ipOrigem, string ipDestino, int porta, string protocolo)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var dispositivo = dbContext.Dispositivos.FirstOrDefault(d => d.IPAddress == ipOrigem);
                if (dispositivo == null) return;

                var historico = new HistoricoRede
                {
                    DispositivoId = dispositivo.Id,
                    DataHora = DateTime.UtcNow,
                    TipoEvento = TipoEventoRede.Navegacao,
                    IPDestino = ipDestino,
                    PortaDestino = porta,
                    Protocolo = protocolo
                };

                await dbContext.HistoricosRede.AddAsync(historico);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar histórico de rede: {Mensagem}", ex.Message);
            }
        }
    }
}