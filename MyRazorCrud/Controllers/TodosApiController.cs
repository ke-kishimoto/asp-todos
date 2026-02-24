using Microsoft.AspNetCore.Mvc;
using MyRazorCrud.Models;
using MyRazorCrud.Services;

namespace MyRazorCrud.Controllers;

// -----------------------------------------------------------------------
// 【Web API版】Todos コントローラー
//
// ★ MVC (TodosController) との主な違い：
//   - [ApiController] 属性を付ける
//       → バリデーションエラー時に自動で 400 Bad Request を返す
//       → [FromBody] などのバインディングソース推論が有効になる
//   - View() を返さず、データ（オブジェクト）をそのまま返す
//       → ASP.NET Core が自動的に JSON にシリアライズしてレスポンス
//   - [Route("api/[controller]")] で /api/todos にマップされる
//       → [controller] はクラス名から "Controller" を除いた "Todos" に解決
// -----------------------------------------------------------------------
[ApiController]
// ★ [controller] を使うと "TodosApi" に解決されてしまうため、URLを明示的に指定
// クラス名: TodosApiController → [controller] = "TodosApi" → /api/TodosApi になってしまう
[Route("api/todos")]
public class TodosApiController : ControllerBase
{
    // ★ MVC/Razor Pages と同様に DI でリポジトリを受け取る
    private readonly InMemoryTodoRepository _repo;

    public TodosApiController(InMemoryTodoRepository repo)
    {
        _repo = repo;
    }

    // ---------------------------------------------------------------
    // GET /api/todos
    //
    // ★ MVC        : return View(items);  → HTMLを返す
    // ★ Razor Pages: return Page();       → HTMLを返す
    // ★ Web API    : return Ok(items);    → JSON を返す
    //
    // レスポンス例:
    // [
    //   { "id": 1, "title": "Learn Razor Pages", "done": false, "createdAt": "..." },
    //   { "id": 2, "title": "Build List page",   "done": false, "createdAt": "..." }
    // ]
    // ---------------------------------------------------------------
    [HttpGet]
    public ActionResult<IReadOnlyList<TodoItem>> GetAll()
    {
        var items = _repo.GetAll();

        // Ok() = HTTP 200 + JSON ボディ
        // ActionResult<T> により Swagger 等でレスポンス型が自動認識される
        return Ok(items);
    }
}
