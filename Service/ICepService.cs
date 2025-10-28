using System.Collections.Generic;
using System.Threading.Tasks;
using Domain;

namespace Service
{
    public interface ICepService
    {
        Task<Cep> ConsultarCepAsync(string cep);
        Task<IEnumerable<Cep>> GetAllCepsAsync();
    }
}