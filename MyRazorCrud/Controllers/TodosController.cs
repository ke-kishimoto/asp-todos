using Microsoft.AspNetCore.Mvc;
using MyRazorCrud.Models;
using MyRazorCrud.Services;

namespace MyRazorCrud.Controllers;

// -----------------------------------------------------------------------
// 【MVC版】Todos CRUD コントローラー
//
// ★ Razor Pages との主な違い：
//   - PageModel の代わりに Controller を継承
//   - OnGet/OnPost の代わりに アクションメソッド (Index/Create/Edit...) を使う
//   - ビューへのデータ渡しは View(model) または ViewData/ViewBag を使う
//   - フォーム入力は アクションメソッドの引数 として受け取る
//     （Razor Pages の [BindProperty] に相当）
//   - リダイレクトは RedirectToAction（Razor Pages は RedirectToPage）
//
// URL は /mvc/todos/... に集約（Razor Pages の /Todos/... と共存）
// -----------------------------------------------------------------------
[Route("mvc/todos")]
public class TodosController : Controller
{
    private readonly InMemoryTodoRepository _repo;

    // DIコンストラクタ（Razor Pages の PageModel と同じ）
    public TodosController(InMemoryTodoRepository repo)
    {
        _repo = repo;
    }

    // ---------------------------------------------------------------
    // GET /mvc/todos
    // ★ Razor Pages: OnGet() ＋ public IReadOnlyList<TodoItem> Items
    // ★ MVC       : View(model) にリストを直接渡す
    // ---------------------------------------------------------------
    [HttpGet("")]
    public IActionResult Index()
    {
        var items = _repo.GetAll();
        return View(items); // Views/Todos/Index.cshtml に渡る
    }

    // ---------------------------------------------------------------
    // GET /mvc/todos/create
    // ★ Razor Pages: OnGet() は空メソッド
    // ★ MVC       : 同様に View() を返すだけ
    // ---------------------------------------------------------------
    [HttpGet("create")]
    public IActionResult Create()
    {
        return View();
    }

    // ---------------------------------------------------------------
    // POST /mvc/todos/create
    // ★ Razor Pages: [BindProperty] public string Title で受け取る
    // ★ MVC       : アクション引数 string title で受け取る（モデルバインディング）
    // ---------------------------------------------------------------
    [HttpPost("create")]
    public IActionResult Create(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            // バリデーションエラー：入力値を ViewData で画面に戻す
            ModelState.AddModelError(nameof(title), "タイトルは必須です");
            ViewData["EnteredTitle"] = title;
            return View(); // 同じ画面を再表示
        }

        _repo.Add(title.Trim());
        // ★ Razor Pages: RedirectToPage("/Todos/Index")
        // ★ MVC       : RedirectToAction(nameof(Index))
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------
    // GET /mvc/todos/details/1
    // ★ Razor Pages: OnGet(int id) ＋ public TodoItem Item
    // ★ MVC       : View(item) にモデルを直接渡す
    // ---------------------------------------------------------------
    [HttpGet("details/{id:int}")]
    public IActionResult Details(int id)
    {
        var item = _repo.GetById(id);
        if (item is null) return NotFound();

        return View(item);
    }

    // ---------------------------------------------------------------
    // GET /mvc/todos/edit/1
    // ★ Razor Pages: OnGet(int id) → Input = new InputModel { ... }
    // ★ MVC       : View(item) に既存データを渡し、フォームの初期値を設定
    // ---------------------------------------------------------------
    [HttpGet("edit/{id:int}")]
    public IActionResult Edit(int id)
    {
        var item = _repo.GetById(id);
        if (item is null) return NotFound();

        return View(item);
    }

    // ---------------------------------------------------------------
    // POST /mvc/todos/edit/1
    // ★ Razor Pages: [BindProperty] public InputModel Input で受け取る
    // ★ MVC       : [Bind("Title,Done")] TodoItem をアクション引数で受け取る
    //              （許可フィールドを明示するセキュリティベストプラクティス）
    // ---------------------------------------------------------------
    [HttpPost("edit/{id:int}")]
    public IActionResult Edit(int id, [Bind("Title,Done")] TodoItem input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            ModelState.AddModelError(nameof(input.Title), "タイトルは必須です");
            // バリデーションエラー時は入力値（id付き）を画面に戻す
            input.Id = id;
            return View(input);
        }

        var ok = _repo.Update(id, input.Title.Trim(), input.Done);
        if (!ok) return NotFound();

        // ★ Razor Pages: RedirectToPage("/Todos/Details", new { id })
        // ★ MVC       : RedirectToAction(nameof(Details), new { id })
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------------------------------------------------------------
    // GET /mvc/todos/delete/1
    // ★ Razor Pages: OnGet(int id) → Item = item
    // ★ MVC       : View(item) にモデルを渡す
    // ---------------------------------------------------------------
    [HttpGet("delete/{id:int}")]
    public IActionResult Delete(int id)
    {
        var item = _repo.GetById(id);
        if (item is null) return NotFound();

        return View(item);
    }

    // ---------------------------------------------------------------
    // POST /mvc/todos/delete/1
    // ★ Razor Pages: OnPost(int id) という名前で POST を受け取る
    // ★ MVC       : GET と同名だと競合するため ActionName 属性で明示
    // ---------------------------------------------------------------
    [HttpPost("delete/{id:int}")]
    [ActionName("Delete")] // Getと同名にするためのエイリアス
    public IActionResult DeleteConfirmed(int id)
    {
        var ok = _repo.Delete(id);
        if (!ok) return NotFound();

        return RedirectToAction(nameof(Index));
    }
}
