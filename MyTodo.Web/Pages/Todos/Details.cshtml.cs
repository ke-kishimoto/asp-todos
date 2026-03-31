using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyTodo.Application.Queries.Todos;
using MyTodo.Web.Models;

namespace MyTodo.Web.Pages.Todos;

public class DetailsModel : PageModel
{
    private readonly ITodoQueryService _queryService;

    public TodoItemViewModel Item { get; private set; } = default!;

    public DetailsModel(ITodoQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _queryService.GetByIdAsync(id);
        if (item is null) return NotFound();

        Item = new TodoItemViewModel(item);
        return Page();
    }
}