using Microsoft.EntityFrameworkCore;
using MyRazorCrud.Data;
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

// ★追加：インメモリRepoをDI登録
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
