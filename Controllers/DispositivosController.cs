using GerenciadorRede.API.Data;
using GerenciadorRede.API.DTOs;
using GerenciadorRede.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorRede.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DispositivosController : ControllerBase
    {

        private readonly AppDbContext _context;

        public DispositivosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ObterTodos()
        {
            try
            {
                var lista = await _context.Dispositivos.AsNoTracking().ToListAsync();
                return Ok(lista);

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao obter dispositivos: {ex.Message}");
            }
        }

        [HttpPost("status")]
        public async Task<IActionResult> AlterarStatus([FromBody] RequisicaoStatusDto model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.MACAddress))
            {
                return BadRequest("O MAC Address do dispositivo alvo é obrigatório.");
            }


                try
                {
                    var dispositivo = await _context.Dispositivos
                        .FirstOrDefaultAsync(d => d.MACAddress == model.MACAddress);

                    if (dispositivo == null)
                    {
                        return NotFound($"Dispositivo com o MAC {model.MACAddress} não foi localizado na base de dados.");
                    }

                    dispositivo.Status = model.Bloquear ? StatusDispositivo.Bloqueado : StatusDispositivo.Online;
                    dispositivo.UltimoSinalVisto = DateTime.UtcNow;

                    _context.Dispositivos.Update(dispositivo);
                    await _context.SaveChangesAsync();

                    string acao = model.Bloquear ? "BLOQUEADO e isolado" : "LIBERADO para acesso";
                    return Ok(new { mensagem = $"Dispositivo {dispositivo.IPAddress} foi {acao} com sucesso." });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Erro ao tentar atualizar registro no MySQL: {ex.Message}");
                }


            }
    }

    
}
