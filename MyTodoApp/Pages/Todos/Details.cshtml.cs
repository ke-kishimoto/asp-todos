using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyTodoApp.Models;
using MyTodoApp.Services;

namespace MyTodoApp.Pages.Todos;

public class DetailsModel : PageModel
{
    private readonly ITodoService _service;

    public TodoItemEntity Item { get; private set; } = default!;

    public DetailsModel(ITodoService service)
    {
        _service = service;
    }

    // GET /Todos/Details?id=123
    // ★ 変更点：OnGet → OnGetAsync
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null) return NotFound();

        Item = new TodoItemEntity(item);
        return Page();
    }
}
