using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyTodo.Application.Commands.Todos;

namespace MyTodo.Web.Pages.Todos;

public class CreateModel : PageModel
{
    private readonly CreateTodoCommandHandler _createHandler;

    public CreateModel(CreateTodoCommandHandler createHandler)
    {
        _createHandler = createHandler;
    }

    [BindProperty]
    public string Title { get; set; } = "";

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ModelState.AddModelError(nameof(Title), "タイトルは必須です");
            return Page();
        }

        await _createHandler.HandleAsync(new CreateTodoCommand(Title));
        return RedirectToPage("/Todos/Index");
    }
}