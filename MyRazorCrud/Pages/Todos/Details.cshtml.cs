using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorCrud.Models;
using MyRazorCrud.Services;

namespace MyRazorCrud.Pages.Todos;

public class DetailsModel : PageModel
{
    private readonly InMemoryTodoRepository _repo;

    public TodoItem Item { get; private set; } = default!;

    public DetailsModel(InMemoryTodoRepository repo)
    {
        _repo = repo;
    }

    // GET /Todos/Details?id=123
    public IActionResult OnGet(int id)
    {
        var item = _repo.GetById(id);
        if (item is null)
        {
            return NotFound();
        }

        Item = item;
        return Page();
    }
}
