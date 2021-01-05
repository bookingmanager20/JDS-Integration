using JDS.Integration.Web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JDS.Integration.Web.Services
{
    public interface ITodoListService
    {
        Task<IEnumerable<Todo>> GetAsync();

        Task<Todo> GetAsync(int id);

        Task DeleteAsync(int id);

        Task<Todo> AddAsync(Todo todo);

        Task<Todo> EditAsync(Todo todo);
    }
}
