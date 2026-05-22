using System;
using System.Linq;
using System.Threading.Tasks;
using GerenciadorRede.API.Data;
using GerenciadorRede.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorRede.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HistoricoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HistoricoController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ObterTodos([FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 50)
        {
            var totalItens = await _context.HistoricosRede.CountAsync();

            var itens = await _context.HistoricosRede
                .OrderByDescending(h => h.DataHora)
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToListAsync();

            return Ok(new { totalItens, pagina, tamanhoPagina, dados = itens });
        }

        [HttpGet("dispositivo/{dispositivoId}")]
        public async Task<IActionResult> ObterPorDispositivo(Guid dispositivoId, [FromQuery] TipoEventoRede? tipoFiltro = null)
        {
            var query = _context.HistoricosRede
                .Where(h => h.DispositivoId == dispositivoId);

            if (tipoFiltro.HasValue)
            {
                query = query.Where(h => h.TipoEvento == tipoFiltro.Value);
            }

            var logs = await query
                .OrderByDescending(h => h.DataHora)
                .Take(100)
                .ToListAsync();

            return Ok(logs);
        }

        [HttpGet("estatisticas/navegacao")]
        public async Task<IActionResult> ObterDestinosMaisAcessados()
        {
            var destinos = await _context.HistoricosRede
                .Where(h => h.TipoEvento == TipoEventoRede.Navegacao && h.IPDestino != null)
                .GroupBy(h => h.IPDestino)
                .Select(g => new
                {
                    IPDestino = g.Key,
                    TotalAcessos = g.Count(),
                    UltimoAcesso = g.Max(h => h.DataHora)
                })
                .OrderByDescending(x => x.TotalAcessos)
                .Take(10)
                .ToListAsync();

            return Ok(destinos);
        }

        [HttpDelete("limpar")]
        public async Task<IActionResult> LimparHistoricoAntigo([FromQuery] int diasParaManter = 7)
        {
            var dataLimite = DateTime.UtcNow.AddDays(-diasParaManter);

            var registrosParaExcluir = await _context.HistoricosRede
                .Where(h => h.DataHora < dataLimite)
                .ToListAsync();

            if (registrosParaExcluir.Any())
            {
                _context.HistoricosRede.RemoveRange(registrosParaExcluir);
                await _context.SaveChangesAsync();
            }

            return Ok(new { mensagem = $"{registrosParaExcluir.Count} registros antigos foram limpos." });
        }
    }
} 