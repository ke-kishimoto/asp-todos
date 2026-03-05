using Microsoft.AspNetCore.Mvc.RazorPages;
using MyTodoApp.Models;
using MyTodoApp.Services;

namespace MyTodoApp.Pages.Todos;

public class IndexModel : PageModel
{
    // ★ 変更点：InMemoryTodoRepository → ITodoService に切り替え
    //   上位層（Page）はインターフェースにのみ依存する
    private readonly ITodoService _service;

    public IReadOnlyList<TodoItemViewModel> Items { get; private set; } = Array.Empty<TodoItemViewModel>();

    public IndexModel(ITodoService service)
    {
        _service = service;
    }

    // ★ 変更点：OnGet → OnGetAsync（DB操作は非同期で行う）
    public async Task OnGetAsync()
    {
        Items = (await _service.GetAllAsync()).Items.Select(i => new TodoItemViewModel(i)).ToList();
    }
}
