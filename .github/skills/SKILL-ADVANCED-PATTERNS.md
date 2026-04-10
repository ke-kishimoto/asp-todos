# MyTodo 高度な実装パターン

[SKILL.md](SKILL.md) から分割した、特定シナリオ向けの実装パターン集です。

---

## 目次

1. [インライン編集テーブルによる一画面 CRUD パターン（Blazor Server）](#1-インライン編集テーブルによる一画面-crud-パターンblazor-server)
2. [UnitOfWork パターンによるトランザクション制御](#2-unitofwork-パターンによるトランザクション制御)

---

## 1. インライン編集テーブルによる一画面 CRUD パターン（Blazor Server）

### 概要

一覧表の各行を直接編集し、画面上の **1 つの「更新」ボタン** で追加・更新・削除をまとめて保存するパターンです。  
MVC のように画面遷移せず、Blazor の状態管理を活かして差分を検出します。  
`CategoryList.razor` がこのパターンの実装例です。

**採用条件**:
- 複数行を一度に変更して一括保存したい
- 画面遷移なしで CRUD を完結させたい（管理画面・設定画面など）
- 行の追加・削除・編集が混在する可能性がある

---

### 行状態モデル（RowModel）

コンポーネント内に `private class RowModel` を定義し、各行の状態を管理します。

```csharp
private class CategoryRowModel
{
    public int Id { get; set; }           // 0 = 新規行
    public string Name { get; set; } = "";
    public string OriginalName { get; set; } = "";  // 変更検出用
    public DateTime CreatedAt { get; set; }
    public bool IsNew { get; set; }       // DBに未保存の行
    public bool IsDeleted { get; set; }   // 削除マーク
}
```

**状態の遷移**:

| `IsNew` | `IsDeleted` | 意味 |
|---|---|---|
| `true` | `false` | 新規追加行（キャンセルで行ごと除去） |
| `false` | `false` | 既存行（`Name != OriginalName` で変更検出） |
| `false` | `true` | 削除マーク済み行（取消で `IsDeleted = false` に戻す） |

---

### コンポーネント全体の構造

```razor
@inject ICategoryQueryService QueryService
@inject SaveCategoriesCommandHandler SaveHandler

<cluster-l space="var(--s-1)" align="center">
    <button class="btn btn--primary" type="button" @onclick="AddRow">＋ 行追加</button>
    <button class="btn btn--success" type="button"
            style="margin-inline-start:auto"
            @onclick="SaveAsync"
            disabled="@isSaving">
        @(isSaving ? "保存中..." : "更新")
    </button>
</cluster-l>

<table class="data-table data-table--hover">
    <tbody>
        @foreach (var row in rows)
        {
            var r = row;  @* ループ変数のキャプチャ（クロージャ対策） *@
            <tr class="@(r.IsDeleted ? "data-table__row--muted" : "")">
                <td>
                    @if (r.IsDeleted)
                    {
                        <s class="text-muted">@r.Name</s>
                    }
                    else
                    {
                        @* インライン入力：@bind ではなく value + @oninput で即時反映 *@
                        <input class="form-input"
                               type="text"
                               value="@r.Name"
                               @oninput="e => r.Name = e.Value?.ToString() ?? string.Empty" />
                    }
                </td>
                <td class="text-center">
                    @if (r.IsDeleted)
                    {
                        <button class="btn btn--sm btn--outline"
                                type="button"
                                @onclick="() => r.IsDeleted = false">取消</button>
                    }
                    else
                    {
                        <button class="btn btn--sm btn--outline-danger"
                                type="button"
                                @onclick="() => MarkDelete(r)">削除</button>
                    }
                </td>
            </tr>
        }
    </tbody>
</table>

@code {
    private List<CategoryRowModel>? rows;
    private bool isSaving = false;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        var items = await QueryService.GetAllAsync();
        rows = items.Select(x => new CategoryRowModel
        {
            Id = x.Id,
            Name = x.Name,
            OriginalName = x.Name,  // 変更検出の基準値
            CreatedAt = x.CreatedAt,
            IsNew = false,
            IsDeleted = false
        }).ToList();
    }

    // 新規行を末尾に追加（DB 保存はまだしない）
    private void AddRow()
    {
        rows ??= [];
        rows.Add(new CategoryRowModel { Id = 0, IsNew = true });
    }

    // 削除マーク（新規行は即除去、既存行は IsDeleted フラグを立てる）
    private void MarkDelete(CategoryRowModel row)
    {
        if (row.IsNew) rows!.Remove(row);
        else row.IsDeleted = true;
    }

    // 差分を集めて一括保存
    private async Task SaveAsync()
    {
        if (rows is null) return;

        var added   = rows.Where(r => r.IsNew && !r.IsDeleted && !string.IsNullOrWhiteSpace(r.Name))
                          .Select(r => r.Name).ToList();
        var updated = rows.Where(r => !r.IsNew && !r.IsDeleted && r.Name.Trim() != r.OriginalName)
                          .Select(r => new CategoryChange(r.Id, r.Name)).ToList();
        var deleted = rows.Where(r => !r.IsNew && r.IsDeleted)
                          .Select(r => r.Id).ToList();

        if (added.Count == 0 && updated.Count == 0 && deleted.Count == 0) return;

        isSaving = true;
        try
        {
            await SaveHandler.HandleAsync(new SaveCategoriesCommand(added, updated, deleted));
            await LoadAsync();  // 保存後に再取得してリセット
        }
        finally { isSaving = false; }
    }
}
```

---

### Command 設計

追加・更新・削除を **1 つの Command** で表現します。  
Blazor コンポーネントが差分を検出し、このコマンドに詰めて渡します。

```csharp
// 更新対象行の表現（id + 新しい値）
public record CategoryChange(int Id, string Name);

// 追加/更新/削除 の3リストを 1 コマンドにまとめる
public record SaveCategoriesCommand(
    IReadOnlyList<string> Added,
    IReadOnlyList<CategoryChange> Updated,
    IReadOnlyList<int> Deleted);
```

---

### MVC ホスト View の作成

Blazor コンポーネントを MVC の View 内に埋め込む場合は、  
`blazor.server.js` の読み込みを **`@section Scripts`** に必ず含めます（これがないと SignalR 接続が確立されず、ボタン等のインタラクションが動作しません）。

```cshtml
@* Views/Categories/Index.cshtml *@
@{
    ViewData["Title"] = "カテゴリ管理";
}

<h1>カテゴリ管理</h1>

<component type="typeof(MyTodo.Web.BlazorComponents.CategoryList)"
           render-mode="ServerPrerendered" />

@section Scripts {
    <script src="_framework/blazor.server.js"></script>
}
```

対応する Controller はデータを持たず View を返すだけにします。

```csharp
[Route("mvc/categories")]
public class CategoriesController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
```

---

## 2. UnitOfWork パターンによるトランザクション制御

### 概要

複数のリポジトリ操作（追加・更新・削除の混在）を **1 つのトランザクション** にまとめる場合に使います。  
通常のリポジトリは `SaveChangesAsync()` を自前で呼びますが、UnitOfWork を使う場合はリポジトリが `SaveChangesAsync()` を呼ばず、コミット権限を `IUnitOfWork` に委譲します。

**採用条件**:
- 追加・更新・削除が混在し、すべて成功か全ロールバックにしたい
- 複数のリポジトリをまたいだ操作をアトミックにしたい

---

### Application 層：インターフェース定義

```csharp
// MyTodo.Application/Repositories/IUnitOfWork.cs
namespace MyTodo.Application.Repositories;

public interface IUnitOfWork
{
    Task BeginTransactionAsync();
    Task CommitAsync();    // SaveChangesAsync + CommitAsync を内包
    Task RollbackAsync();
}
```

---

### Application 層：リポジトリインターフェース（SaveChanges を呼ばない）

UnitOfWork と組み合わせるリポジトリは、戻り値を `Task`（void 相当）にし、`SaveChangesAsync()` を呼びません。  
操作を EF Core の ChangeTracker に積むだけにします。

```csharp
// MyTodo.Application/Repositories/ICategoryRepository.cs
namespace MyTodo.Application.Repositories;

public interface ICategoryRepository
{
    Task AddAsync(string name);     // SaveChanges なし
    Task UpdateAsync(int id, string name);
    Task DeleteAsync(int id);
}
```

通常パターン（`IItemRepository` 等）との比較:

| | 通常リポジトリ | UnitOfWork 対応リポジトリ |
|---|---|---|
| 戻り値 | `Task<T>` / `Task<bool>` | `Task` |
| `SaveChangesAsync()` | リポジトリ内で呼ぶ | 呼ばない |
| トランザクション境界 | ハンドラーごと | `IUnitOfWork` が管理 |

---

### Application 層：CommandHandler での使い方

```csharp
public class SaveCategoriesCommandHandler
{
    private readonly ICategoryRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public SaveCategoriesCommandHandler(ICategoryRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(SaveCategoriesCommand command)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // ① 削除 → ② 更新 → ③ 追加 の順で ChangeTracker に積む
            foreach (var id in command.Deleted)
                await _repo.DeleteAsync(id);

            foreach (var change in command.Updated)
                await _repo.UpdateAsync(change.Id, change.Name.Trim());

            foreach (var name in command.Added)
                await _repo.AddAsync(name.Trim());

            // SaveChangesAsync + CommitAsync を一括実行
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;  // 呼び出し元に例外を伝播させる
        }
    }
}
```

---

### Infrastructure 層：EfUnitOfWork 実装

```csharp
// MyTodo.Infrastructure/Repositories/EfUnitOfWork.cs
public class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private IDbContextTransaction? _transaction;

    public EfUnitOfWork(AppDbContext db) { _db = db; }

    public async Task BeginTransactionAsync()
        => _transaction = await _db.Database.BeginTransactionAsync();

    public async Task CommitAsync()
    {
        await _db.SaveChangesAsync();          // すべての変更を DB に送出
        if (_transaction is not null)
        {
            await _transaction.CommitAsync();  // トランザクションをコミット
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
```

---

### Infrastructure 層：リポジトリ実装（SaveChanges を呼ばない）

```csharp
public class EfCategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public EfCategoryRepository(AppDbContext db) { _db = db; }

    // ChangeTracker に追加するだけ（SaveChanges は呼ばない）
    public Task AddAsync(string name)
    {
        _db.Categories.Add(new CategoryEntity { CategoryName = name, CreatedAt = DateTime.UtcNow });
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(int id, string name)
    {
        var entity = await _db.Categories.FindAsync(id);
        if (entity is null) return;
        entity.CategoryName = name;
        // ChangeTracker が Modified として追跡する（SaveChanges は呼ばない）
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _db.Categories.FindAsync(id);
        if (entity is null) return;
        _db.Categories.Remove(entity);
        // ChangeTracker が Deleted として追跡する（SaveChanges は呼ばない）
    }
}
```

---

### DI 登録

`InfrastructureServiceCollectionExtensions.cs` に追記します。

```csharp
// IUnitOfWork は Scoped（DbContext と同じ単位で生存）
services.AddScoped<IUnitOfWork, EfUnitOfWork>();
services.AddScoped<ICategoryRepository, EfCategoryRepository>();
```

`ApplicationServiceCollectionExtensions.cs` に CommandHandler を追記します。

```csharp
services.AddScoped<SaveCategoriesCommandHandler>();
```

---

### ファイル配置まとめ

```
MyTodo.Application/
  Repositories/
    IUnitOfWork.cs               ← インターフェース定義
    ICategoryRepository.cs       ← SaveChanges なしのリポジトリIF

  Commands/Categories/
    SaveCategoriesCommand.cs     ← Command + Handler（IUnitOfWork を注入）

MyTodo.Infrastructure/
  Repositories/
    EfUnitOfWork.cs              ← IUnitOfWork 実装
    EfCategoryRepository.cs      ← SaveChanges を呼ばない実装

MyTodo.Web/BlazorComponents/
  CategoryList.razor             ← 一画面 CRUD Blazor コンポーネント

MyTodo.Web/Controllers/
  CategoriesController.cs        ← View のみ返す MVC コントローラー

MyTodo.Web/Views/Categories/
  Index.cshtml                   ← blazor.server.js 込みの MVC View
```
