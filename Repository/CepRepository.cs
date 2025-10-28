using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Domain;
using MySqlConnector;

namespace Repository
{
    public class CepRepository : ICepRepository
    {
        private readonly MySqlConnection _connection;

        public CepRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "A string de conexão não pode ser nula ou vazia.");

            _connection = new MySqlConnection(connectionString);
        }

        public async Task<int> AddCepAsync(Cep cep)
        {
            if (cep == null)
                throw new ArgumentNullException(nameof(cep), "O objeto CEP não pode ser nulo.");

            await _connection.OpenAsync();
            try
            {
                const string sql = @"
                    INSERT INTO cep (cep, logradouro, complemento, bairro, localidade, uf, ibge, gia, ddd, siafi, dataconsulta)
                    VALUES (@Cep, @Logradouro, @Complemento, @Bairro, @Localidade, @Uf, @Ibge, @Gia, @Ddd, @Siafi, @DataConsulta);
                    SELECT LAST_INSERT_ID();
                ";

                var id = await _connection.ExecuteScalarAsync<int>(sql, cep);
                return id;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<IEnumerable<Cep>> GetAllCepsAsync()
        {
            await _connection.OpenAsync();
            try
            {
                const string sql = @"
                    SELECT id AS Id, cep AS Cep, logradouro AS Logradouro, complemento AS Complemento, bairro AS Bairro,
                           localidade AS Localidade, uf AS Uf, ibge AS Ibge, gia AS Gia, ddd AS Ddd, siafi AS Siafi, dataconsulta AS DataConsulta
                    FROM cep;
                ";

                var ceps = await _connection.QueryAsync<Cep>(sql);
                return ceps;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<Cep?> GetCepByCodeAsync(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
                throw new ArgumentException("O CEP não pode ser nulo ou vazio.", nameof(cep));

            var normalized = Regex.Replace(cep, @"\D", "");

            await _connection.OpenAsync();
            try
            {
                const string sql = @"
                    SELECT id AS Id, cep AS Cep, logradouro AS Logradouro, complemento AS Complemento, bairro AS Bairro,
                           localidade AS Localidade, uf AS Uf, ibge AS Ibge, gia AS Gia, ddd AS Ddd, siafi AS Siafi, dataconsulta AS DataConsulta
                    FROM cep
                    WHERE cep = @Cep
                    LIMIT 1;
                ";

                var result = await _connection.QueryFirstOrDefaultAsync<Cep>(sql, new { Cep = normalized });
                return result;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }
    }
}