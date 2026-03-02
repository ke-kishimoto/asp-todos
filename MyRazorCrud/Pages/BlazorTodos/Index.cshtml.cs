using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyRazorCrud.Pages.BlazorTodos;

// =========================================================
// Blazor ホストページの PageModel
// =========================================================
// ★ このクラスは Blazor の動作に直接関与しない
//    Blazor コンポーネント（TodoList.razor）が
//    サービスを @inject で注入し、自律的にデータを取得・更新する
//
// ★ MVC との対比：
//   MVC          : Controller.Index() でデータ取得 → View(items) で渡す
//   Blazor Server: PageModel は HTTP の入り口のみ担当
//                  データ取得は Blazor コンポーネントの OnInitializedAsync() で行う
// =========================================================
public class IndexModel : PageModel
{
    public void OnGet() { }
}
