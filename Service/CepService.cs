using System.Text.Json;
using System.Text.RegularExpressions;
using Domain;
using Microsoft.Extensions.Logging;
using Repository;

namespace Service
{
    public class CepService : ICepService
    {
        private readonly ICepRepository _cepRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CepService> _logger;

        public CepService(ICepRepository cepRepository, IHttpClientFactory httpClientFactory, ILogger<CepService> logger)
        {
            _cepRepository = cepRepository ?? throw new ArgumentNullException(nameof(cepRepository));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Cep> ConsultarCepAsync(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
                throw new ArgumentException("CEP inválido.", nameof(cep));

            var normalized = Regex.Replace(cep, @"\D", "");
            if (normalized.Length != 8)
                throw new ArgumentException("CEP deve conter 8 dígitos.", nameof(cep));

            var existing = await _cepRepository.GetCepByCodeAsync(normalized);
            if (existing != null)
                return existing;

            var client = _httpClientFactory.CreateClient();
            var url = $"https://viacep.com.br/ws/{normalized}/json/";

            var response = await client.GetStringAsync(url);
            var viaCepResponse = JsonSerializer.Deserialize<Cep>(response);

            if (viaCepResponse == null)
                throw new InvalidOperationException("Resposta inválida da API ViaCEP.");

            viaCepResponse.DataConsulta = DateTime.UtcNow;
            await _cepRepository.AddCepAsync(viaCepResponse);

            return viaCepResponse;
        }

        public async Task<IEnumerable<Cep>> GetAllCepsAsync()
        {
            return await _cepRepository.GetAllCepsAsync();
        }

        Task<Cep> ICepService.ConsultarCepAsync(string cep)
        {
            throw new NotImplementedException();
        }

        Task<IEnumerable<Cep>> ICepService.GetAllCepsAsync()
        {
            throw new NotImplementedException();
        }
    }
}