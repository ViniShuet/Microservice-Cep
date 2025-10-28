using System.Collections.Generic;
using System.Threading.Tasks;
using Domain;
using Microsoft.AspNetCore.Mvc;
using performance_cache.Models; 
using Service;

namespace performance_cache.Controllers
{
    [ApiController]
    [Route("api/cep")]
    public class CepController : ControllerBase
    {
        private readonly ICepService _cepService;

        public CepController(ICepService cepService)
        {
            _cepService = cepService;
        }

        [HttpPost]
        public async Task<IActionResult> AddCep([FromBody] CepRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Cep))
                return BadRequest(new { message = "O campo 'cep' é obrigatório." });

            var result = await _cepService.ConsultarCepAsync(request.Cep);
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cep>>> GetAllCeps()
        {
            var ceps = await _cepService.GetAllCepsAsync();
            return Ok(ceps);
        }
    }
}