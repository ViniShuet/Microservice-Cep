using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Domain;
using MySqlConnector;

namespace Repository
{
    internal class CepRepository : ICepRepository
    {
        private readonly string _connectionString;

        public CepRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        private MySqlConnection CreateConnection() => new MySqlConnection(_connectionString);

        /// <summary>
        /// Insere um CEP no banco e retorna o id gerado.
        /// </summary>
        public async Task<int> AddCepAsync(Cep cep)
        {
            if (cep == null)
                throw new ArgumentNullException(nameof(cep), "CEP inválido.");

            // Normaliza o código do CEP (remove caracteres não numéricos)
            if (!string.IsNullOrWhiteSpace(cep.Code))
                cep.Code = Regex.Replace(cep.Code, @"\D", "");

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO cep (code, street, neighborhood, city, state)
                VALUES (@Code, @Street, @Neighborhood, @City, @State);
                SELECT LAST_INSERT_ID();
            ";

            var id = await conn.ExecuteScalarAsync<int>(sql, cep);
            return id;
        }

        /// <summary>
        /// Retorna todos os CEPs salvos.
        /// </summary>
        public async Task<IEnumerable<Cep>> GetAllCepsAsync()
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT id AS Id, code AS Code, street AS Street, neighborhood AS Neighborhood, city AS City, state AS State
                FROM cep;
            ";

            var ceps = await conn.QueryAsync<Cep>(sql);
            return ceps;
        }

        /// <summary>
        /// Busca um CEP pelo código (aceita formatos com ou sem máscara).
        /// Retorna null se não encontrado.
        /// </summary>
        public async Task<Cep?> GetCepByCodeAsync(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
                throw new ArgumentException("CEP inválido.", nameof(cep));

            var normalized = Regex.Replace(cep, @"\D", "");

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT id AS Id, code AS Code, street AS Street, neighborhood AS Neighborhood, city AS City, state AS State
                FROM cep
                WHERE code = @Code
                LIMIT 1;
            ";

            var result = await conn.QueryFirstOrDefaultAsync<Cep>(sql, new { Code = normalized });
            return result;
        }
    }
}