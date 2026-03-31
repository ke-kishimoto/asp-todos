using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyTodo.Application.Commands.Todos;
using MyTodo.Application.Queries.Todos;

namespace MyTodo.Web.Pages.Todos;

public class EditModel : PageModel
{
    private readonly ITodoQueryService _queryService;
    private readonly UpdateTodoCommandHandler _updateHandler;

    public EditModel(ITodoQueryService queryService, UpdateTodoCommandHandler updateHandler)
    {
        _queryService  = queryService;
        _updateHandler = updateHandler;
    }

    public class InputModel
    {
        public string Title { get; set; } = "";
        public bool Done { get; set; }
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _queryService.GetByIdAsync(id);
        if (item is null) return NotFound();

        Input = new InputModel
        {
            Title = item.Title,
            Done  = item.Done
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(Input.Title))
        {
            ModelState.AddModelError("Input.Title", "タイトルは必須です");
            return Page();
        }

        var ok = await _updateHandler.HandleAsync(new UpdateTodoCommand(id, Input.Title, Input.Done));
        if (!ok) return NotFound();

        return RedirectToPage("/Todos/Details", new { id });
    }
}