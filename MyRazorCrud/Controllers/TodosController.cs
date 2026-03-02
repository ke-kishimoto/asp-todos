using Microsoft.AspNetCore.Mvc;
using MyRazorCrud.Models;
using MyRazorCrud.Services;

namespace MyRazorCrud.Controllers;

// -----------------------------------------------------------------------
// 【MVC版】Todos CRUD コントローラー
//
// ★ 変更点：InMemoryTodoRepository → ITodoService に切り替え
//   かつ 全アクションを非同期（async/await）に変更
// -----------------------------------------------------------------------
[Route("mvc/todos")]
public class TodosController : Controller
{
    private readonly ITodoService _service;

    public TodosController(ITodoService service)
    {
        _service = service;
    }

    // GET /mvc/todos
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var items = await _service.GetAllAsync();
        return View(items);
    }

    [HttpGet("search")]
    public async Task<IActionResult> search(string keyword)
    {
        var items = await _service.GetItemsAsync(keyword);
        return View(viewName: "index", model: items);
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

        await _service.CreateAsync(title);
        return RedirectToAction(nameof(Index));
    }

    // GET /mvc/todos/details/1
    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null) return NotFound();

        return View(item);
    }

    // GET /mvc/todos/edit/1
    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null) return NotFound();

        return View(item);
    }

    // POST /mvc/todos/edit/1
    [HttpPost("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, [Bind("Title,Done")] TodoItem input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            ModelState.AddModelError(nameof(input.Title), "タイトルは必須です");
            input.Id = id;
            return View(input);
        }

        var ok = await _service.UpdateAsync(id, input.Title, input.Done);
        if (!ok) return NotFound();

        return RedirectToAction(nameof(Details), new { id });
    }

    // GET /mvc/todos/delete/1
    [HttpGet("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null) return NotFound();

        return View(item);
    }

    // POST /mvc/todos/delete/1
    [HttpPost("delete/{id:int}")]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound();

        return RedirectToAction(nameof(Index));
    }
}
