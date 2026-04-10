# パターン A: MVC フルサイクル CRUD

## 概要

HTTP リクエストと画面遷移を基本とする、伝統的な MVC パターンの CRUD 実装です。  
フォーム送信 → リダイレクト → 一覧表示 のライフサイクルで動作します。

**採用条件**:
- 画面遷移で自然に CRUD 操作が表現できる（フォームページ → 一覧ページ）
- SEO・アクセシビリティを重視する
- Blazor の状態管理が不要なシンプルな CRUD

**実装例**: `TodosController`, `MyTodo.Web/Views/Todos/`

---

## ファイル構成

```
MyTodo.Application/
  Queries/Todos/
    ITodoQueryService.cs       ← QueryService インターフェース + ReadModel
    TodoReadModel.cs
  Commands/Todos/
    CreateTodoCommand.cs       ← Command record + Handler を同一ファイルに
    UpdateTodoCommand.cs
    DeleteTodoCommand.cs

MyTodo.Infrastructure/
  Repositories/
    EfTodoRepository.cs        ← SaveChanges を自前で呼ぶ通常リポジトリ
  Queries/
    TodoQueryService.cs        ← EF Core LINQ で ReadModel に直接射影

MyTodo.Web/
  Controllers/
    TodosController.cs         ← [Route("mvc/todos")] の MVC Controller
  Models/
    TodoViewModel.cs           ← View 用の ViewModel
  Views/Todos/
    Index.cshtml
    Create.cshtml
    Edit.cshtml
    Delete.cshtml
    Details.cshtml
```

---

## 各層の実装

### 1. Domain 層（変更なし）

```csharp
// MyTodo.Domain/TodoItem.cs
public record TodoId(int Value);
public record TodoTitle(string Value);
public record TodoIsCompleted(bool Value);
public record TodoCreatedAt(DateTime Value);

public record TodoItem(TodoId Id, TodoTitle Title, TodoIsCompleted IsCompleted, TodoCreatedAt CreatedAt);
```

---

### 2. Application 層 — ReadModel & QueryService インターフェース

```csharp
// MyTodo.Application/Queries/Todos/TodoReadModel.cs
public record TodoReadModel(int Id, string Title, bool Done, DateTime CreatedAt);
```

```csharp
// MyTodo.Application/Queries/Todos/ITodoQueryService.cs
public interface ITodoQueryService
{
    Task<IReadOnlyList<TodoReadModel>> GetAllAsync();
    Task<TodoReadModel?> GetByIdAsync(int id);
}
```

---

### 3. Application 層 — Command & Handler

Command record と Handler class は同一ファイルに定義します。

```csharp
// MyTodo.Application/Commands/Todos/CreateTodoCommand.cs
public record CreateTodoCommand(string Title);

public class CreateTodoCommandHandler
{
    private readonly ITodoRepository _repo;
    public CreateTodoCommandHandler(ITodoRepository repo) => _repo = repo;

    public async Task<TodoItem> HandleAsync(CreateTodoCommand command)
        => await _repo.AddAsync(command.Title.Trim());
}
```

```csharp
// MyTodo.Application/Commands/Todos/UpdateTodoCommand.cs
public record UpdateTodoCommand(int Id, string Title, bool Done);

public class UpdateTodoCommandHandler
{
    private readonly ITodoRepository _repo;
    public UpdateTodoCommandHandler(ITodoRepository repo) => _repo = repo;

    public async Task<bool> HandleAsync(UpdateTodoCommand command)
        => await _repo.UpdateAsync(command.Id, command.Title.Trim(), command.Done);
}
```

```csharp
// MyTodo.Application/Commands/Todos/DeleteTodoCommand.cs
public record DeleteTodoCommand(int Id);

public class DeleteTodoCommandHandler
{
    private readonly ITodoRepository _repo;
    public DeleteTodoCommandHandler(ITodoRepository repo) => _repo = repo;

    public async Task<bool> HandleAsync(DeleteTodoCommand command)
        => await _repo.DeleteAsync(command.Id);
}
```

---

### 4. Infrastructure 層 — Repository（通常パターン）

通常リポジトリは各メソッド内で `SaveChangesAsync()` を自前で呼びます。  
（UnitOfWork パターンとは異なります）

```csharp
// MyTodo.Infrastructure/Repositories/EfTodoRepository.cs
public class EfTodoRepository : ITodoRepository
{
    private readonly AppDbContext _db;
    public EfTodoRepository(AppDbContext db) => _db = db;

    public async Task<TodoItem> AddAsync(string title)
    {
        var entity = new TodoItemEntity { Title = title, Done = false, CreatedAt = DateTime.UtcNow };
        _db.Todos.Add(entity);
        await _db.SaveChangesAsync();
        return new TodoItem(new TodoId(entity.Id), new TodoTitle(entity.Title),
            new TodoIsCompleted(entity.Done), new TodoCreatedAt(entity.CreatedAt));
    }

    public async Task<bool> UpdateAsync(int id, string title, bool done)
    {
        var entity = await _db.Todos.FindAsync(id);
        if (entity is null) return false;
        entity.Title = title;
        entity.Done = done;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.Todos.FindAsync(id);
        if (entity is null) return false;
        _db.Todos.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
}
```

---

### 5. Infrastructure 層 — QueryService

```csharp
// MyTodo.Infrastructure/Queries/TodoQueryService.cs
public class TodoQueryService : ITodoQueryService
{
    private readonly AppDbContext _db;
    public TodoQueryService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TodoReadModel>> GetAllAsync()
        => await _db.Todos
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new TodoReadModel(x.Id, x.Title, x.Done, x.CreatedAt))
            .ToListAsync();

    public async Task<TodoReadModel?> GetByIdAsync(int id)
    {
        var entity = await _db.Todos.FindAsync(id);
        return entity is null ? null
            : new TodoReadModel(entity.Id, entity.Title, entity.Done, entity.CreatedAt);
    }
}
```

---

### 6. Web 層 — MVC Controller

```csharp
// MyTodo.Web/Controllers/TodosController.cs
[Route("mvc/todos")]
public class TodosController : Controller
{
    private readonly ITodoQueryService _queryService;
    private readonly CreateTodoCommandHandler _createHandler;
    private readonly UpdateTodoCommandHandler _updateHandler;
    private readonly DeleteTodoCommandHandler _deleteHandler;

    public TodosController(
        ITodoQueryService queryService,
        CreateTodoCommandHandler createHandler,
        UpdateTodoCommandHandler updateHandler,
        DeleteTodoCommandHandler deleteHandler)
    {
        _queryService  = queryService;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
    }

    // GET /mvc/todos
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var items = await _queryService.GetAllAsync();
        return View(items.Select(x => new TodoViewModel(x.Id, x.Title, x.Done, x.CreatedAt)).ToList());
    }

    // GET /mvc/todos/create
    [HttpGet("create")]
    public IActionResult Create() => View();

    // POST /mvc/todos/create
    [HttpPost("create")]
    public async Task<IActionResult> Create(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError(nameof(title), "タイトルは必須です");
            return View();
        }
        await _createHandler.HandleAsync(new CreateTodoCommand(title));
        return RedirectToAction(nameof(Index));
    }

    // GET /mvc/todos/edit/{id}
    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _queryService.GetByIdAsync(id);
        if (item is null) return NotFound();
        return View(new TodoViewModel(item.Id, item.Title, item.Done, item.CreatedAt));
    }

    // POST /mvc/todos/edit/{id}
    [HttpPost("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, string title, bool done)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError(nameof(title), "タイトルは必須です");
            return View();
        }
        await _updateHandler.HandleAsync(new UpdateTodoCommand(id, title, done));
        return RedirectToAction(nameof(Index));
    }

    // GET /mvc/todos/delete/{id}
    [HttpGet("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _queryService.GetByIdAsync(id);
        if (item is null) return NotFound();
        return View(new TodoViewModel(item.Id, item.Title, item.Done, item.CreatedAt));
    }

    // POST /mvc/todos/delete/{id}
    [HttpPost("delete/{id:int}")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _deleteHandler.HandleAsync(new DeleteTodoCommand(id));
        return RedirectToAction(nameof(Index));
    }
}
```

### URL ルーティング規則

| アクション | HTTP メソッド | パス |
|---|---|---|
| 一覧 | GET | `/mvc/{entity}` |
| 詳細 | GET | `/mvc/{entity}/details/{id}` |
| 作成フォーム | GET | `/mvc/{entity}/create` |
| 作成実行 | POST | `/mvc/{entity}/create` |
| 編集フォーム | GET | `/mvc/{entity}/edit/{id}` |
| 編集実行 | POST | `/mvc/{entity}/edit/{id}` |
| 削除確認 | GET | `/mvc/{entity}/delete/{id}` |
| 削除実行 | POST | `/mvc/{entity}/delete/{id}` |

---

### 7. Web 層 — ViewModel

```csharp
// MyTodo.Web/Models/TodoViewModel.cs
public record TodoViewModel(int Id, string Title, bool Done, DateTime CreatedAt);
```

---

### 8. DI 登録

`InfrastructureServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<ITodoRepository, EfTodoRepository>();
services.AddScoped<ITodoQueryService, TodoQueryService>();
```

`ApplicationServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<CreateTodoCommandHandler>();
services.AddScoped<UpdateTodoCommandHandler>();
services.AddScoped<DeleteTodoCommandHandler>();
```
