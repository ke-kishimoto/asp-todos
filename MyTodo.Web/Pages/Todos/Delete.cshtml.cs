using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyTodo.Web.Models;
using MyTodo.Web.Services;

namespace MyTodo.Web.Pages.Todos;

public class DeleteModel : PageModel
{
    private readonly ITodoService _service;

    public DeleteModel(ITodoService service)
    {
        _service = service;
    }

    public TodoItemViewModel Item { get; private set; } = default!;

    // GET /Todos/Delete?id=1
    // ★ 変更点：OnGet → OnGetAsync
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null) return NotFound();

        Item = new TodoItemViewModel(item);
        return Page();
    }

    // POST /Todos/Delete?id=1
    // ★ 変更点：OnPost → OnPostAsync
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound();

        return RedirectToPage("/Todos/Index");
    }
}
