using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyTodoApp.Services;

namespace MyTodoApp.Pages.Todos;

public class CreateModel : PageModel
{
    private readonly ITodoService _service;

    public CreateModel(ITodoService service)
    {
        _service = service;
    }

    [BindProperty]
    public string Title { get; set; } = "";

    // GET /Todos/Create
    public void OnGet()
    {
    }

    // POST /Todos/Create
    // ★ 変更点：OnPost → OnPostAsync
    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ModelState.AddModelError(nameof(Title), "タイトルは必須です");
            return Page();
        }

        // ★ 変更点：_repo.Add → await _service.CreateAsync
        await _service.CreateAsync(Title);
        return RedirectToPage("/Todos/Index");
    }
}
