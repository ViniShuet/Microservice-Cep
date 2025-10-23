using Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Service
{
    internal class CepService : ICepService
    {
        private readonly ICepRepository _cepRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CepService> _logger;
        private readonly string _viaCepBaseUrl;

        public CepService(
            ICepRepository cepRepository,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<CepService> logger)
        {
            _cepRepository = cepRepository ?? throw new ArgumentNullException(nameof(cepRepository));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viaCepBaseUrl = configuration?.GetValue<string>("ViaCep:BaseUrl") ?? "https://viacep.com.br/ws";
        }

        /// <summary>
        /// Consulta o serviço ViaCEP pelo CEP informado. Se encontrado, salva no banco (se ainda não existir)
        /// e retorna o registro salvo/consultado.
        /// </summary>
        public async Task<Cep> ConsultarCepAsync(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
                throw new ArgumentException("CEP inválido.", nameof(cep));

            // Normaliza (remove não-dígitos)
            var normalized = Regex.Replace(cep, @"\D", "");
            if (normalized.Length != 8)
                throw new ArgumentException("CEP deve conter 8 dígitos.", nameof(cep));

            // Verifica se já existe no banco
            try
            {
                var existing = await _cepRepository.GetCepByCodeAsync(normalized);
                if (existing != null)
                {
                    _logger.LogInformation("CEP {Cep} já existe no banco (Id={Id}). Retornando registro existente.", normalized, existing.Id);
                    return existing;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao verificar existência do CEP {Cep} no banco. Continuando para consulta externa.", normalized);
                // continuar para tentar consultar ViaCEP mesmo que a verificação no DB falhe
            }

            // Consulta ViaCEP
            var client = _httpClientFactory.CreateClient();
            var url = $"{_viaCepBaseUrl}/{normalized}/json/";
            _logger.LogDebug("Consultando ViaCEP: {Url}", url);

            ViaCepResponse? viaCepResponse;
            try
            {
                using var resp = await client.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                {
                    var content = await resp.Content.ReadAsStringAsync();
                    _logger.LogError("ViaCEP retornou status {Status} para {Cep}: {Content}", resp.StatusCode, normalized, content);
                    throw new HttpRequestException($"Erro ao consultar ViaCEP: {resp.StatusCode}");
                }

                var stream = await resp.Content.ReadAsStreamAsync();
                viaCepResponse = await JsonSerializer.DeserializeAsync<ViaCepResponse>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao consultar ViaCEP para {Cep}", normalized);
                throw;
            }

            if (viaCepResponse == null)
            {
                _logger.LogError("Resposta nula da ViaCEP para {Cep}", normalized);
                throw new InvalidOperationException("Resposta inválida da ViaCEP.");
            }

            if (viaCepResponse.Erro)
            {
                _logger.LogWarning("ViaCEP informou erro para o CEP {Cep}", normalized);
                throw new KeyNotFoundException($"CEP não encontrado na ViaCEP: {cep}");
            }

            var cepEntity = new Cep
            {
                Code = normalized,
                Street = viaCepResponse.Logradouro,
                Neighborhood = viaCepResponse.Bairro,
                City = viaCepResponse.Localidade,
                State = viaCepResponse.Uf
            };

            // Salva no banco
            try
            {
                var id = await _cepRepository.AddCepAsync(cepEntity);
                cepEntity.Id = id;
                _logger.LogInformation("CEP {Cep} salvo no banco com Id {Id}", normalized, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar CEP {Cep} no banco", normalized);
                throw;
            }

            return cepEntity;
        }

        /// <summary>
        /// Retorna todos os CEPs salvos no banco.
        /// </summary>
        public async Task<IEnumerable<Cep>> GetAllCepsAsync()
        {
            try
            {
                return await _cepRepository.GetAllCepsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os CEPs no banco");
                throw;
            }
        }

        // Classe de mapeamento para resposta da ViaCEP
        private class ViaCepResponse
        {
            public string? Cep { get; set; }
            public string? Logradouro { get; set; }
            public string? Complemento { get; set; }
            public string? Bairro { get; set; }
            public string? Localidade { get; set; }
            public string? Uf { get; set; }
            public bool Erro { get; set; }
        }
    }
}