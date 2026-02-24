using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorCrud.Models;
using MyRazorCrud.Services;

namespace MyRazorCrud.Pages.Todos;

public class IndexModel : PageModel
{
    private readonly InMemoryTodoRepository _repo;

    public IReadOnlyList<TodoItem> Items { get; private set; } = Array.Empty<TodoItem>();

    public IndexModel(InMemoryTodoRepository repo)
    {
        _repo = repo;
    }

    public void OnGet()
    {
        Items = _repo.GetAll();
    }
}
