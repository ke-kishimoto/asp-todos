using MyRazorCrud.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
// ★ MVC追加：Razorビューを使うControllerを有効化
builder.Services.AddControllersWithViews();

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
