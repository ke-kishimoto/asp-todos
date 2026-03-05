using MyTodoApp.Services;
using MyTodo.Infrastructure.Repositories;
using MyTodo.Infrastructure;
    
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
builder.Services.AddScoped<ITodoRepository, EfTodoRepository>();
builder.Services.AddScoped<ITodoService, TodoService>();

// ★ 旧実装：InMemoryをそのまま残す（比較・参照用）
builder.Services.AddSingleton<InMemoryTodoRepository>();

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
