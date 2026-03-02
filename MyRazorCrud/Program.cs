using Microsoft.EntityFrameworkCore;
using MyRazorCrud.Data;
using MyRazorCrud.Repositories;
using MyRazorCrud.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
// ★ MVC追加：Razorビューを使うControllerを有効化
builder.Services.AddControllersWithViews();

// ★ EF Core 追加：AppDbContext を DI 登録
//   - appsettings.json の "DefaultConnection" 接続文字列を使用
//   - UseSqlServer で SQL Server プロバイダーを指定
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.Run();
