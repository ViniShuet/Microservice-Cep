using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Domain;
using Microsoft.Extensions.Logging; // Adicionado para usar o logger

namespace Repository
{
    public class CepRepository : ICepRepository
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CepRepository> _logger; // Logger adicionado

        public CepRepository(HttpClient httpClient, ILogger<CepRepository> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> AddCepAsync(Cep cep)
        {
            throw new NotImplementedException("Este método não é necessário para a integração com a API ViaCEP.");
        }

        public async Task<IEnumerable<Cep>> GetAllCepsAsync()
        {
            throw new NotImplementedException("Este método não é necessário para a integração com a API ViaCEP.");
        }

        public async Task<Cep?> GetCepByCodeAsync(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
            {
                _logger.LogWarning("CEP fornecido é nulo ou vazio.");
                throw new ArgumentException("O CEP não pode ser nulo ou vazio.", nameof(cep));
            }

            // Normaliza o CEP (remove caracteres não numéricos)
            var normalized = Regex.Replace(cep, @"\D", "");
            if (normalized.Length != 8)
            {
                _logger.LogWarning("CEP fornecido ({Cep}) não contém exatamente 8 dígitos.", cep);
                throw new ArgumentException("O CEP deve conter exatamente 8 dígitos.", nameof(cep));
            }

            // Monta a URL da API ViaCEP
            var url = $"https://viacep.com.br/ws/{normalized}/json/";
            _logger.LogInformation("Consultando API ViaCEP com a URL: {Url}", url);

            try
            {
                // Faz a requisição para a API ViaCEP
                var response = await _httpClient.GetAsync(url);
                _logger.LogInformation("Resposta da API ViaCEP recebida com status: {StatusCode}", response.StatusCode);

                response.EnsureSuccessStatusCode();

                // Lê e desserializa a resposta JSON
                var jsonResponse = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Resposta JSON da API ViaCEP: {JsonResponse}", jsonResponse);

                var viaCepResponse = JsonSerializer.Deserialize<Cep>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (viaCepResponse == null)
                {
                    _logger.LogError("A resposta da API ViaCEP é nula.");
                    throw new InvalidOperationException("A resposta da API ViaCEP é inválida.");
                }

                // Valida os campos obrigatórios
                if (string.IsNullOrWhiteSpace(viaCepResponse.CepCode) ||
                    string.IsNullOrWhiteSpace(viaCepResponse.Logradouro) ||
                    string.IsNullOrWhiteSpace(viaCepResponse.Bairro) ||
                    string.IsNullOrWhiteSpace(viaCepResponse.Localidade) ||
                    string.IsNullOrWhiteSpace(viaCepResponse.Uf))
                {
                    _logger.LogError("A resposta da API ViaCEP não contém todos os campos obrigatórios. Resposta: {ViaCepResponse}", viaCepResponse);
                    throw new InvalidOperationException("A resposta da API ViaCEP não contém todos os campos obrigatórios.");
                }

                // Define a data da consulta
                viaCepResponse.DataConsulta = DateTime.UtcNow;
                _logger.LogInformation("Consulta ao CEP {CepCode} realizada com sucesso.", viaCepResponse.CepCode);

                return viaCepResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro ao conectar à API ViaCEP.");
                throw new InvalidOperationException($"Erro ao conectar à API ViaCEP: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erro ao desserializar a resposta da API ViaCEP.");
                throw new InvalidOperationException($"Erro ao desserializar a resposta da API ViaCEP: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao consultar a API ViaCEP.");
                throw new InvalidOperationException($"Erro inesperado ao consultar a API ViaCEP: {ex.Message}", ex);
            }
        }
    }
}