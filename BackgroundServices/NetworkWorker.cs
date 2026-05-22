using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GerenciadorRede.API.Data;
using GerenciadorRede.API.Models;
using GerenciadorRede.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GerenciadorRede.API.BackgroundServices
{
    public class NetworkWorker : BackgroundService
    {
        private readonly ILogger<NetworkWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _intervaloVarredura = TimeSpan.FromMinutes(5);

        public NetworkWorker(ILogger<NetworkWorker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servico de Background do Monitor de Rede inicializado e conectado ao MySQL.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Executando ciclo agendado de varredura...");

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var scannerService = scope.ServiceProvider.GetRequiredService<INetworkScannerService>();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        var dispositivosOnline = await scannerService.EscanearRedeAsync();

                        if (dispositivosOnline.Count > 0)
                        {
                            var dispositivosFiltrados = dispositivosOnline
                                .GroupBy(d => d.MACAddress)
                                .Select(g => g.First())
                                .ToList();

                            _logger.LogInformation("Sincronizando {Total} dispositivos encontrados com o banco de dados...", dispositivosFiltrados.Count);

                            foreach (var dispEncontrado in dispositivosFiltrados)
                            {
                                if (string.IsNullOrWhiteSpace(dispEncontrado.MACAddress)) continue;

                                var dispositivoBanco = await dbContext.Dispositivos
                                    .FirstOrDefaultAsync(d => d.MACAddress == dispEncontrado.MACAddress, stoppingToken);

                                if (dispositivoBanco == null)
                                {
                                    _logger.LogInformation("[Novo Dispositivo] Detectado pela primeira vez na rede: MAC {MAC} no IP {IP}",
                                        dispEncontrado.MACAddress, dispEncontrado.IPAddress);

                                    await dbContext.Dispositivos.AddAsync(dispEncontrado, stoppingToken);
                                }
                                else
                                {
                                    dispositivoBanco.IPAddress = dispEncontrado.IPAddress;
                                    dispositivoBanco.Status = StatusDispositivo.Online;
                                    dispositivoBanco.UltimoSinalVisto = DateTime.UtcNow;

                                    dbContext.Dispositivos.Update(dispositivoBanco);
                                }
                            }

                            await dbContext.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Banco de dados sincronizado e atualizado com sucesso.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ocorreu um erro inesperado durante o ciclo do Worker de Rede.");
                }

                int tempoRestanteSegundos = (int)_intervaloVarredura.TotalSeconds;

                _logger.LogInformation("Iniciando contagem regressiva para o proximo ciclo...");

                while (tempoRestanteSegundos > 0 && !stoppingToken.IsCancellationRequested)
                {
                    var tempoFormatado = TimeSpan.FromSeconds(tempoRestanteSegundos).ToString(@"mm\:ss");
                    Console.Write($"\r[Cronometro] Proxima varredura em: {tempoFormatado} | Aguardando... ");

                    await Task.Delay(1000, stoppingToken);
                    tempoRestanteSegundos--;
                }

                Console.Write("\r" + new string(' ', 60) + "\r");
            }

            _logger.LogWarning("Servico de Background do Monitor de Rede esta sendo finalizado.");
        }
    }
}