using Microsoft.AspNetCore.Mvc.RazorPages;
using MyTodo.Application.Queries.Todos;
using MyTodo.Web.Models;

namespace MyTodo.Web.Pages.Todos;

public class IndexModel : PageModel
{
    private readonly ITodoQueryService _queryService;

    public IReadOnlyList<TodoItemViewModel> Items { get; private set; } = Array.Empty<TodoItemViewModel>();

    public IndexModel(ITodoQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task OnGetAsync()
    {
        var items = await _queryService.GetAllAsync();
        Items = items.Select(i => new TodoItemViewModel(i)).ToList();
    }
}