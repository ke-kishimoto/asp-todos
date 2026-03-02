using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorCrud.Services;

namespace MyRazorCrud.Pages.Todos;

public class EditModel : PageModel
{
    private readonly ITodoService _service;

    public EditModel(ITodoService service)
    {
        _service = service;
    }

    public class InputModel
    {
        public string Title { get; set; } = "";
        public bool Done { get; set; }
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    // GET /Todos/Edit?id=1
    // ★ 変更点：OnGet → OnGetAsync
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null) return NotFound();

        Input = new InputModel
        {
            Title = item.Title,
            Done = item.Done
        };

        return Page();
    }

    // POST /Todos/Edit?id=1
    // ★ 変更点：OnPost → OnPostAsync
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(Input.Title))
        {
            ModelState.AddModelError("Input.Title", "タイトルは必須です");
            return Page();
        }

        var ok = await _service.UpdateAsync(id, Input.Title, Input.Done);
        if (!ok) return NotFound();

        return RedirectToPage("/Todos/Details", new { id });
    }
}
