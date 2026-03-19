using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTodo.Application.Services;
using MyTodo.Web.Models;

namespace MyTodo.Web.Controllers;

// -----------------------------------------------------------------------
// [Authorize] 属性 : このコントローラー全体を「認証必須」にする
//
// ★ 動作の仕組み：
//   - 未認証ユーザーがアクセス → 登録したChallengeスキームの処理を実行
//     - 開発 (FakeAuth) : FakeAuthHandler が常に認証済みを返すのでそのまま通過
//     - 本番 (Entra ID) : Entra ID のログインページへリダイレクト
//   - 認証済みユーザー → [Authorize] を通過してアクションが実行される
//
// ★ [AllowAnonymous] で個別アクションを公開に戻すこともできる
//   例) [AllowAnonymous] public IActionResult PublicInfo() { ... }
//
// ★ Todo 機能（TodosController）には [Authorize] が付いていないため
//   認証なしでアクセス可能 → 要件どおり「既存機能は認証不要」を実現
// -----------------------------------------------------------------------
[Authorize]
[Route("mvc/items")]
public class ItemsController : Controller
{
    private readonly IItemService _service;

    // ★ DI (依存性注入)：IItemService のインターフェースに依存
    //   → テスト時はモックに差し替え可能
    public ItemsController(IItemService service)
    {
        _service = service;
    }

    // -----------------------------------------------------------------------
    // GET /mvc/items
    // アイテム一覧を表示する
    //
    // ★ ログイン済みユーザーの情報は User プロパティから取得できる
    //   例) User.Identity?.Name          → ユーザー名
    //       User.FindFirst(ClaimTypes.Email)?.Value → メールアドレス
    // -----------------------------------------------------------------------
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var items = await _service.GetAllAsync();

        // Domain モデル → ViewModel に変換してビューに渡す
        var viewModels = items.Select(item => new ItemViewModel(item)).ToList();

        // ★ ログイン中のユーザー名をViewDataでビューに渡す例
        ViewData["UserName"] = User.Identity?.Name ?? "（不明）";

        return View(viewModels);
    }

    // -----------------------------------------------------------------------
    // GET /mvc/items/create
    // 登録フォームを表示する
    // -----------------------------------------------------------------------
    [HttpGet("create")]
    public IActionResult Create()
    {
        // 空の ViewModel を渡すことでフォームの初期値を確定させる
        return View(new ItemCreateViewModel());
    }

    // -----------------------------------------------------------------------
    // POST /mvc/items/create
    // フォームの送信を処理し、アイテムを登録する
    //
    // ★ ModelState.IsValid とは：
    //   ViewModel の [Required] や [Range] などのバリデーション属性を
    //   ASP.NET Core が自動的に評価した結果
    //   すべての条件を満たしていれば true、1つでも違反があれば false
    // -----------------------------------------------------------------------
    [HttpPost("create")]
    public async Task<IActionResult> Create(ItemCreateViewModel model)
    {
        // バリデーション失敗時はフォームを再表示
        // ModelState にエラー情報が入っているのでビュー側で <span asp-validation-for> に表示される
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _service.CreateAsync(model.ItemCode, model.ItemName, model.Price);

        // 登録成功 → PRG パターン (Post-Redirect-Get) でリダイレクト
        // → ブラウザの「更新」ボタンによる二重送信を防ぐ
        return RedirectToAction(nameof(Index));
    }
}
