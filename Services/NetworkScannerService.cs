using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GerenciadorRede.API.Models;
using Microsoft.Extensions.Logging;

namespace GerenciadorRede.API.Services
{
    public class NetworkScannerService : INetworkScannerService
    {
        private readonly ILogger<NetworkScannerService> _logger;

        public NetworkScannerService(ILogger<NetworkScannerService> logger)
        {
            _logger = logger;
        }

        public async Task<List<Dispositivo>> EscanearRedeAsync()
        {
            _logger.LogInformation("Iniciando escaneamento da rede...");
            var stopwatch = Stopwatch.StartNew();

            string subRedeBase = DescobrirSubRedeLocal();

            if (string.IsNullOrEmpty(subRedeBase))
            {
                _logger.LogWarning("Varredura cancelada: Nenhuma interface de rede IPv4 ativa foi detectada no Windows.");
                return new List<Dispositivo>();
            }

            _logger.LogDebug("Sub-rede identificada para varredura: {SubRede}x", subRedeBase);

            _logger.LogDebug("Acordando dispositivos na rede (disparando pings em lote de .1 a .254)...");
            await DispararPingEmMassaAsync(subRedeBase);

            var dispositivosEncontrados = await LerTabelaArpWindowsAsync();

            stopwatch.Stop();
            _logger.LogInformation("Varredura concluída com sucesso em {TempoTotal}ms. Dispositivos ativos encontrados: {Contagem}",
                stopwatch.ElapsedMilliseconds, dispositivosEncontrados.Count);

            return dispositivosEncontrados;


        }

        private string DescobrirSubRedeLocal()
        {
            try
            {
                foreach (var placa in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (placa.OperationalStatus == OperationalStatus.Up)
                    {
                        var propriedadesIp = placa.GetIPProperties();

                        foreach (var ip in propriedadesIp.UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                string ipLocal = ip.Address.ToString();
                                if (ipLocal.StartsWith("169.254")) continue;

                                int ultimoPontoIndex = ipLocal.LastIndexOf('.');
                                if (ultimoPontoIndex > 0)
                                {
                                    string prefixoDetectado = ipLocal.Substring(0, ultimoPontoIndex + 1);
                                    _logger.LogDebug("Placa de rede ativa detectada: {NomePlaca} | IP Local: {IP}", placa.Name, ipLocal);
                                    return prefixoDetectado;
                                }
                            }
                        }
                    }


                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                {
                    _logger.LogError(ex, "Falha crítica ao tentar mapear as interfaces de rede locais do Windows.");
                    return string.Empty;

                }



            }
        }

        private async Task DispararPingEmMassaAsync(string subRedeBase)
        {
            var tarefasPing = new List<Task>();
            for (int i = 1; i <= 254; i++)
            {
                string ipAlvo = $"{subRedeBase}{i}";
                tarefasPing.Add(DispararPingSimplesAsync(ipAlvo));

            }
            await Task.WhenAll(tarefasPing);
        }


        private async Task DispararPingSimplesAsync(string ip)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var resposta = await ping.SendPingAsync(ip, 100);
                    if (resposta.Status == IPStatus.Success)
                    {
                        _logger.LogDebug("Ping bem-sucedido para {IP}", ip);
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao enviar ping para {IP}. Este erro pode ser normal se o dispositivo estiver offline ou bloqueando pings.", ip);
            }


        }

        private async Task<List<Dispositivo>> LerTabelaArpWindowsAsync()
        {
            return await Task.Run(() =>
            {
                var dispositivosEncontrados = new List<Dispositivo>();
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c arp -a",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var processo = Process.Start(startInfo))
                {
                    if (processo == null)
                    {
                        _logger.LogError("Não foi possível iniciar o processo cmd.exe para ler a tabela ARP.");
                        return dispositivosEncontrados;
                    }

                    string textoBruto = processo.StandardOutput.ReadToEnd();
                    processo.WaitForExit();

                    var regexArp = new Regex(@"([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})\s+([0-9a-fA-F-]{17})\s+(\w+)");
                    var linhas = textoBruto.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var linha in linhas)
                    {
                        var match = regexArp.Match(linha);
                        if (match.Success)
                        {
                            string ip = match.Groups[1].Value;
                            string mac = match.Groups[2].Value.ToUpper().Replace("-", ":");

                            // endereços multicast/broadcast conhecidos ignoradoaster
                            if (mac.Equals("FF:FF:FF:FF:FF:FF", StringComparison.OrdinalIgnoreCase) ||
                                ip.StartsWith("224.") ||
                                ip.StartsWith("239."))
                            {
                                continue;
                            }

                            dispositivosEncontrados.Add(new Dispositivo
                            {
                                Id = Guid.NewGuid(),
                                IPAddress = ip,
                                MACAddress = mac,
                                Status = StatusDispositivo.Online,
                                UltimoSinalVisto = DateTime.UtcNow
                            });
                        }
                    }
                }

                return dispositivosEncontrados;


            });
        }



    }
}
