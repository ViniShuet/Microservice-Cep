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
            {
                _logger.LogInformation("CEP {CepCode} encontrado no repositório.", normalized);
                return existing;
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://viacep.com.br/ws/{normalized}/json/";
            _logger.LogInformation("Consultando API ViaCEP: {Url}", url);

            var response = await client.GetStringAsync(url);
            var viaCepResponse = JsonSerializer.Deserialize<Cep>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (viaCepResponse == null)
            {
                _logger.LogError("Resposta nula da API ViaCEP para o CEP {CepCode}.", normalized);
                throw new InvalidOperationException("Resposta inválida da API ViaCEP.");
            }

            viaCepResponse.DataConsulta = DateTime.UtcNow;
            await _cepRepository.AddCepAsync(viaCepResponse);

            return viaCepResponse;
        }

        public async Task<IEnumerable<Cep>> GetAllCepsAsync()
        {
            _logger.LogInformation("Buscando todos os CEPs no repositório.");
            return await _cepRepository.GetAllCepsAsync();
        }
    }
}