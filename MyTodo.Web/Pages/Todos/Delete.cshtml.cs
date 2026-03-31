using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyTodo.Application.Commands.Todos;
using MyTodo.Application.Queries.Todos;
using MyTodo.Web.Models;

namespace MyTodo.Web.Pages.Todos;

public class DeleteModel : PageModel
{
    private readonly ITodoQueryService _queryService;
    private readonly DeleteTodoCommandHandler _deleteHandler;

    public DeleteModel(ITodoQueryService queryService, DeleteTodoCommandHandler deleteHandler)
    {
        _queryService  = queryService;
        _deleteHandler = deleteHandler;
    }

    public TodoItemViewModel Item { get; private set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _queryService.GetByIdAsync(id);
        if (item is null) return NotFound();

        Item = new TodoItemViewModel(item);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var ok = await _deleteHandler.HandleAsync(new DeleteTodoCommand(id));
        if (!ok) return NotFound();

        return RedirectToPage("/Todos/Index");
    }
}