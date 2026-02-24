using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorCrud.Models;
using MyRazorCrud.Services;

namespace MyRazorCrud.Pages.Todos;

public class DeleteModel : PageModel
{
    private readonly InMemoryTodoRepository _repo;

    public DeleteModel(InMemoryTodoRepository repo)
    {
        _repo = repo;
    }

    public TodoItem Item { get; private set; } = default!;

    // GET /Todos/Delete?id=1
    public IActionResult OnGet(int id)
    {
        var item = _repo.GetById(id);
        if (item is null) return NotFound();

        Item = item;
        return Page();
    }

    // POST /Todos/Delete?id=1
    public IActionResult OnPost(int id)
    {
        var ok = _repo.Delete(id);
        if (!ok) return NotFound();

        return RedirectToPage("/Todos/Index");
    }
}
