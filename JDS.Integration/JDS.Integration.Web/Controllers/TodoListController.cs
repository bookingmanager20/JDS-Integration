using JDS.Integration.Web.Models;
using JDS.Integration.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Threading.Tasks;

namespace JDS.Integration.Web.Controllers
{
    public class TodoListController : Controller
    {
        private ITodoListService _todoListService;

        public TodoListController(ITodoListService todoListService)
        {
            _todoListService = todoListService;
        }

        // GET: TodoList
        [AuthorizeForScopes(Scopes = new string[]{ "https://JustTestB2C.onmicrosoft.com/api/write", "https://JustTestB2C.onmicrosoft.com/api/read"})]
        public async Task<ActionResult> Index()
        {
            return View(await _todoListService.GetAsync());
        }

        // GET: TodoList/Details/5
        public async Task<ActionResult> Details(int id)
        {
            return View(await _todoListService.GetAsync(id));
        }

        // GET: TodoList/Create
        [AuthorizeForScopes(ScopeKeySection = "TodoList:TodoListWriteScope")]
        public ActionResult Create()
        {
            Todo todo = new Todo() { Owner = HttpContext.User.Identity.Name };
            return View(todo);
        }

        // POST: TodoList/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind("Title,Owner")] Todo todo)
        {
            await _todoListService.AddAsync(todo);
            return RedirectToAction("Index");
        }

        // GET: TodoList/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            Todo todo = await this._todoListService.GetAsync(id);

            if (todo == null)
            {
                return NotFound();
            }

            return View(todo);
        }

        // POST: TodoList/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, [Bind("Id,Title,Owner")] Todo todo)
        {
            await _todoListService.EditAsync(todo);
            return RedirectToAction("Index");
        }

        // GET: TodoList/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            Todo todo = await this._todoListService.GetAsync(id);

            if (todo == null)
            {
                return NotFound();
            }

            return View(todo);
        }

        // POST: TodoList/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, [Bind("Id,Title,Owner")] Todo todo)
        {
            await _todoListService.DeleteAsync(id);
            return RedirectToAction("Index");
        }
    }
}
