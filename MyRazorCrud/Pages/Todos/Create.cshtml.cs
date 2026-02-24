using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorCrud.Services;

namespace MyRazorCrud.Pages.Todos;

public class CreateModel : PageModel
{
    private readonly InMemoryTodoRepository _repo;

    public CreateModel(InMemoryTodoRepository repo)
    {
        _repo = repo;
    }

    // フォーム入力を受け取る入れ物
    [BindProperty]
    public string Title { get; set; } = "";

    // GET /Todos/Create
    public void OnGet()
    {
    }

    // POST /Todos/Create
    public IActionResult OnPost()
    {
        // 超簡易バリデーション（まずはこれでOK）
        if (string.IsNullOrWhiteSpace(Title))
        {
            ModelState.AddModelError(nameof(Title), "タイトルは必須です");
            return Page(); // 同じ画面を再表示
        }

        _repo.Add(Title.Trim());
        return RedirectToPage("/Todos/Index"); // 登録後は一覧へ
    }
}
