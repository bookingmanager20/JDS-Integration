using JDS.Integration.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Package.AAD.Security.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JDS.Integration.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TodoListController : Controller
    {
        // In-memory TodoList
        private static readonly Dictionary<int, Todo> TodoStore = new Dictionary<int, Todo>();

        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IGraphService _graphService;

        public TodoListController(IHttpContextAccessor contextAccessor, IGraphService graphService)
        {
            this._contextAccessor = contextAccessor;
            _graphService = graphService;

            // Pre-populate with sample data
            if (TodoStore.Count == 0)
            {
                TodoStore.Add(1, new Todo() { Id = 1, Owner = $"{this._contextAccessor.HttpContext.User.Identity.Name}", Title = "Pick up groceries" });
                TodoStore.Add(2, new Todo() { Id = 2, Owner = $"{this._contextAccessor.HttpContext.User.Identity.Name}", Title = "Finish invoice report" });
            }
        }

        // GET: api/values
        [HttpGet]
        [Authorize(Policy = "ReadScope")]
        public async Task<IEnumerable<Todo>> Get()
        {
            string owner = User.Identity.Name;
            var users = await _graphService.GetAllUsers();
            return TodoStore.Values.Where(x => x.Owner == owner);
        }

        // GET: api/values
        [HttpGet("{id}", Name = "Get")]
        [Authorize(Policy = "ReadScope")]
        public Todo Get(int id)
        {
            return TodoStore.Values.FirstOrDefault(t => t.Id == id);
        }

        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            TodoStore.Remove(id);
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post([FromBody] Todo todo)
        {
            int id = TodoStore.Values.OrderByDescending(x => x.Id).FirstOrDefault().Id + 1;
            Todo todonew = new Todo() { Id = id, Owner = HttpContext.User.Identity.Name, Title = todo.Title };
            TodoStore.Add(id, todonew);

            return Ok(todo);
        }

        // PATCH api/values
        [HttpPatch("{id}")]
        public IActionResult Patch(int id, [FromBody] Todo todo)
        {
            if (id != todo.Id)
            {
                return NotFound();
            }

            if (TodoStore.Values.FirstOrDefault(x => x.Id == id) == null)
            {
                return NotFound();
            }

            TodoStore.Remove(id);
            TodoStore.Add(id, todo);

            return Ok(todo);
        }
    }
}
