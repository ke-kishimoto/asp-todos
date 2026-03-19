using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using MyTodo.Application;
using MyTodo.Infrastructure;
using MyTodo.Web.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
// ★ MVC追加：Razorビューを使うControllerを有効化
builder.Services.AddControllersWithViews();

// ★ EF Core 追加：AppDbContext を DI 登録
//   - appsettings.json の "DefaultConnection" 接続文字列を使用
//   - UseSqlServer で SQL Server プロバイダーを指定
//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Infrastructure 層のサービスをまとめて登録する拡張メソッド
builder.Services.AddInfrastructure(builder.Configuration);

// -----------------------------------------------------------------------
// DI 登録：レイヤー構成
//
//   [Controller/Page]
//        ↓ ITodoService に依存
//   [TodoService]      ← Scoped：リクエストごとに生成
//        ↓ ITodoRepository に依存
//   [EfTodoRepository] ← Scoped：リクエストごとに生成（DbContextと合わせる）
//        ↓
//   [AppDbContext]     ← Scoped：AddDbContext が自動登録
//
// ★ AddScoped vs AddSingleton vs AddTransient：
//   - Scoped    : リクエストごとに1インスタンス（DB操作に最適）
//   - Singleton : アプリ全体で1インスタンス（InMemoryRepository はこれ）
//   - Transient : 注入のたびに新しいインスタンス
// -----------------------------------------------------------------------
// builder.Services.AddScoped<ITodoRepository, EfTodoRepository>();

builder.Services.AddApplication(builder.Configuration);

// -----------------------------------------------------------------------
// ★ Blazor Server 追加
//
//   AddServerSideBlazor() : Blazor Server に必要なサービスを DI に登録
//     - SignalR ベースのリアルタイム通信（回線 = Circuit）を管理
//     - Razor コンポーネントのレンダリングエンジンを登録
//
//   MVC/Razor Pages との違い：
//     - MVC/Razor Pages : HTTP リクエスト/レスポンスのライフサイクル
//     - Blazor Server   : WebSocket で "回線" を張り、差分DOM更新で動作
// -----------------------------------------------------------------------
builder.Services.AddServerSideBlazor();

// -----------------------------------------------------------------------
// 認証設定：環境に応じて FakeAuth または Microsoft Entra ID を切り替える
//
// ★ appsettings.Development.json で "UseFakeAuth": true の場合
//   → FakeAuthHandler を登録。実際の Entra ID 名義がなくても動作する。
//
// ★ 本番環境（または UseFakeAuth=false）の場合
//   → Microsoft.Identity.Web で Entra ID による OpenID Connect 認証を有効化する
//   → appsettings.json の "AzureAd" セクションが参照される
//
// ★ OpenID Connect (OIDC) 認証フロー（本番）：
//   1. 未認証ユーザーが [Authorize] ページにアクセス
//   2. ASP.NET Core が Entra ID のログインページにリダイレクト
//   3. ユーザーが Microsoft アカウントでログイン
//   4. Entra ID が CallbackPath (/signin-oidc) に ID トークンを返却
//   5. ASP.NET Core がトークンを検証し、Cookie にセッションを保存
//   6. 元のページにリダイレクト
// -----------------------------------------------------------------------
var useFakeAuth = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("UseFakeAuth");

if (useFakeAuth)
{
    // 開発環境：FakeAuthHandler さえ 認証済みにするカスタムスキームを登録
    // AddAuthentication() の引数にスキーム名を指定することで
    //   DefaultAuthenticateScheme: リクエストの認証に使うスキーム
    //   DefaultChallengeScheme   : 未認証時に呼び出すスキーム（ログイン画面へのリダイレクト等）
    builder.Services.AddAuthentication("FakeAuth")
        .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("FakeAuth", null);
}
else
{
    // 本番環境：Microsoft.Identity.Web で Entra ID 認証を有効化
    // AddMicrosoftIdentityWebApp は以下をまとめて設定するメソッド：
    //   - AddAuthentication() : Cookie + OpenID Connect スキームの登録
    //   - AddCookie()         : ログイン後のセッションを Cookie で管理
    //   - AddOpenIdConnect()  : Entra ID への OIDC プロトコル設定
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
}

// AddAuthorization : [Authorize] 属性やポリシーベースの認可設定を DI に登録
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// -----------------------------------------------------------------------
// 認証・認可ミドルウェア
//
// ★ 必ずこの順序で登録すること！
//   UseAuthentication → UseAuthorization の順序が額面順
//   逆にしたりどちらかを忘れると認可が永遠失敗する
//
//   UseAuthentication : HTTP リクエストから認証情報（Cookie/Token）を読み取り
//                       HttpContext.User に ClaimsPrincipal をセットする
//   UseAuthorization  : HttpContext.User を元に [Authorize] を評価し
//                       認可がなければ 401/403 を返す
// -----------------------------------------------------------------------
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
// ★ MVC追加：コントローラーの従来ルーティングを登録
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// ★ Web API追加：[ApiController] の属性ルーティングを有効化
//   MapControllerRoute は従来ルーティング用のため、APIコントローラーには MapControllers() が必要
app.MapControllers();

// -----------------------------------------------------------------------
// ★ Blazor Server 追加
//
//   MapBlazorHub() : Blazor 回線（Circuit）を確立する SignalR ハブ
//                    /_blazor エンドポイントを登録
//
//   ※ MapRazorComponents<App>() は .NET 8 の新形式（Blazor Web App）
//     旧形式（AddServerSideBlazor + MapBlazorHub）は
//     既存の Razor Pages / MVC アプリへの組み込みに適している
// -----------------------------------------------------------------------
app.MapBlazorHub();

app.Run();
