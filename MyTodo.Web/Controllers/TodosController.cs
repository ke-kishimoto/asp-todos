using Microsoft.AspNetCore.Mvc;
using MyTodo.Application.Commands.Todos;
using MyTodo.Application.Queries.Todos;
using MyTodo.Web.Models;

namespace MyTodo.Web.Controllers;

[Route("mvc/todos")]
public class TodosController : Controller
{
    private readonly ITodoQueryService _queryService;
    private readonly CreateTodoCommandHandler _createHandler;
    private readonly UpdateTodoCommandHandler _updateHandler;
    private readonly DeleteTodoCommandHandler _deleteHandler;

    public TodosController(
        ITodoQueryService queryService,
        CreateTodoCommandHandler createHandler,
        UpdateTodoCommandHandler updateHandler,
        DeleteTodoCommandHandler deleteHandler)
    {
        _queryService  = queryService;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
    }

    // GET /mvc/todos
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var items = await _queryService.GetAllAsync();
        return View(items.Select(item => new TodoViewModel(item.Id, item.Title, item.Done, item.CreatedAt)).ToList());
    }

    [HttpGet("search")]
    public async Task<IActionResult> search(string keyword)
    {
        var items = await _queryService.SearchAsync(keyword);
        return View(viewName: "index", model: items.Select(item => new TodoViewModel(item.Id, item.Title, item.Done, item.CreatedAt)).ToList());
    }

    // GET /mvc/todos/create
    [HttpGet("create")]
    public IActionResult Create()
    {
        return View();
    }

    // POST /mvc/todos/create
    [HttpPost("create")]
    public async Task<IActionResult> Create(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError(nameof(title), "タイトルは必須です");
            ViewData["EnteredTitle"] = title;
            return View();
        }

        await _createHandler.HandleAsync(new CreateTodoCommand(title));
        return RedirectToAction(nameof(Index));
    }

    // GET /mvc/todos/details/1
    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _queryService.GetByIdAsync(id);
        if (item is null) return NotFound();

        return View(new TodoViewModel(item.Id, item.Title, item.Done, item.CreatedAt));
    }

    // GET /mvc/todos/edit/1
    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _queryService.GetByIdAsync(id);
        if (item is null) return NotFound();

        return View(new TodoInputEntity(item));
    }

    // POST /mvc/todos/edit/1
    [HttpPost("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, [Bind("Title,Done")] TodoInputEntity input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            ModelState.AddModelError(nameof(input.Title), "タイトルは必須です");
            input.Id = id;
            return View(input);
        }

        var ok = await _updateHandler.HandleAsync(new UpdateTodoCommand(id, input.Title, input.Done));
        if (!ok) return NotFound();

        return RedirectToAction(nameof(Details), new { id });
    }

    // GET /mvc/todos/delete/1
    [HttpGet("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _queryService.GetByIdAsync(id);
        if (item is null) return NotFound();

        return View(new TodoViewModel(item.Id, item.Title, item.Done, item.CreatedAt));
    }

    // POST /mvc/todos/delete/1
    [HttpPost("delete/{id:int}")]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ok = await _deleteHandler.HandleAsync(new DeleteTodoCommand(id));
        if (!ok) return NotFound();

        return RedirectToAction(nameof(Index));
    }

    public record TodoViewModel(int Id, string Title, bool Done, DateTime CreatedAt);

    public class TodoInputEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public bool Done { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public TodoInputEntity() { }

        public TodoInputEntity(TodoReadModel model)
        {
            Id = model.Id;
            Title = model.Title;
            Done = model.Done;
            CreatedAt = model.CreatedAt;
        }
    }
}
