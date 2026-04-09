# MyTodo コーディング SKILL ガイド

このファイルは `.github/copilot-instructions.md` から参照される、各層の詳細な実装パターン集です。

---

## 目次

1. [Domain 層](#1-domain-層)
2. [Application 層（Command）](#2-application-層command)
3. [Application 層（Query）](#3-application-層query)
4. [Infrastructure 層（Repository）](#4-infrastructure-層repository)
5. [Infrastructure 層（QueryService）](#5-infrastructure-層queryservice)
6. [Web 層（MVC Controller）](#6-web-層mvc-controller)
7. [Web 層（Blazor Server）](#7-web-層blazor-server)
8. [Web 層（Web API）](#8-web-層web-api)
9. [CSS（ITCSS + Every Layout）](#9-cssitcss--every-layout)
10. [単体テスト（xUnit）](#10-単体テストxunit)
11. [E2E テスト（gauge + Playwright）](#11-e2e-テストgauge--playwright)

---

## 1. Domain 層

**場所**: `MyTodo.Domain/`

### 設計原則

- 他プロジェクトへの依存を一切持たない
- プリミティブ型は `record` で値オブジェクトにラップして型安全性を高める
- ドメインロジックはドメインオブジェクトのメソッドに実装する

### エンティティ・値オブジェクトのパターン

```csharp
// 値オブジェクト（プリミティブのラッパー）
public record TodoId(int Value);
public record TodoTitle(string Value);
public record TodoIsCompleted(bool Value);
public record TodoCreatedAt(DateTime Value);

// エンティティ（値オブジェクトで構成）
public record TodoItem(TodoId Id, TodoTitle Title, TodoIsCompleted IsCompleted, TodoCreatedAt CreatedAt);

// コレクションにドメインロジックを持たせる
public record TodoItems(IEnumerable<TodoItem> Items)
{
    public TodoItems AllCompleted()
    {
        return new TodoItems(Items.Select(item => item with { IsCompleted = new TodoIsCompleted(true) }));
    }
}
```

### 命名規則

| 種別 | 規則 |
|---|---|
| 値オブジェクト | `{集約名}{属性名}` (例: `TodoId`, `TodoTitle`) |
| エンティティ | ドメイン概念の名詞 (例: `TodoItem`) |
| コレクション | エンティティ名の複数形 (例: `TodoItems`) |

---

## 2. Application 層（Command）

**場所**: `MyTodo.Application/Commands/`

### 設計原則

- `Command` は `record` で定義する（イミュータブル）
- `CommandHandler` はコンストラクタインジェクションで `IRepository` を受け取る
- バリデーションは `string.IsNullOrWhiteSpace` などで行い、ハンドラー内でトリムする

### コマンドとハンドラーのパターン

```csharp
// コマンド（同一ファイル内に定義）
public record CreateTodoCommand(string Title);

public class CreateTodoCommandHandler
{
    private readonly ITodoRepository _repo;

    public CreateTodoCommandHandler(ITodoRepository repo)
    {
        _repo = repo;
    }

    public async Task<TodoItem> HandleAsync(CreateTodoCommand command)
    {
        return await _repo.AddAsync(command.Title.Trim());
    }
}
```

### ファイル配置

```
Commands/
  Todos/
    CreateTodoCommand.cs   ← Command record + Handler class を同一ファイルに
    UpdateTodoCommand.cs
    DeleteTodoCommand.cs
  Items/
    CreateItemCommand.cs
```

---

## 3. Application 層（Query）

**場所**: `MyTodo.Application/Queries/`

### 設計原則

- QueryService のインターフェースのみをここに定義する（実装は Infrastructure 層）
- ReadModel は `record` で定義し、ドメインオブジェクトを直接返さない
- インターフェースと ReadModel は同一ディレクトリ内に配置する

### ReadModel と インターフェースのパターン

```csharp
// ReadModel（DTO）
public record TodoReadModel(int Id, string Title, bool Done, DateTime CreatedAt);

// QueryService インターフェース
public interface ITodoQueryService
{
    Task<IReadOnlyList<TodoReadModel>> GetAllAsync();
    Task<TodoReadModel?> GetByIdAsync(int id);
    Task<IReadOnlyList<TodoReadModel>> SearchAsync(string keyword);
}
```

### ファイル配置

```
Queries/
  Todos/
    ITodoQueryService.cs
    TodoReadModel.cs
  Items/
    IItemQueryService.cs
    ItemReadModel.cs
```

---

## 4. Infrastructure 層（Repository）

**場所**: `MyTodo.Infrastructure/Repositories/`

### 設計原則

- `Application.Repositories` のインターフェースを実装する
- EF Core の DbContext (`AppDbContext`) に直接依存する
- ドメインオブジェクトへの変換はリポジトリ内で行う

### リポジトリ実装パターン

```csharp
public class EfTodoRepository : ITodoRepository
{
    private readonly AppDbContext _db;

    public EfTodoRepository(AppDbContext db)
    {
        _db = db;
    }

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

## 5. Infrastructure 層（QueryService）

**場所**: `MyTodo.Infrastructure/Queries/`

### 設計原則

- `Application.Queries` のインターフェースを実装する
- EF Core の LINQ で ReadModel に直接射影し、ドメインオブジェクトを経由しない
- 取得のみ行い、状態変更は行わない

### QueryService 実装パターン

```csharp
public class TodoQueryService : ITodoQueryService
{
    private readonly AppDbContext _db;

    public TodoQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TodoReadModel>> GetAllAsync()
    {
        return await _db.Todos
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new TodoReadModel(x.Id, x.Title, x.Done, x.CreatedAt))
            .ToListAsync();
    }

    public async Task<TodoReadModel?> GetByIdAsync(int id)
    {
        var entity = await _db.Todos.FindAsync(id);
        return entity is null
            ? null
            : new TodoReadModel(entity.Id, entity.Title, entity.Done, entity.CreatedAt);
    }

    public async Task<IReadOnlyList<TodoReadModel>> SearchAsync(string keyword)
    {
        return await _db.Todos
            .Where(x => x.Title.Contains(keyword))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new TodoReadModel(x.Id, x.Title, x.Done, x.CreatedAt))
            .ToListAsync();
    }
}
```

---

## 6. Web 層（MVC Controller）

**場所**: `MyTodo.Web/Controllers/`

### 設計原則

- Controller はコンストラクタインジェクションで `QueryService` と各 `CommandHandler` を受け取る
- リポジトリを直接参照しない（必ず CommandHandler / QueryService 経由）
- View に渡すデータは `ViewModel` に変換する（ReadModel / ドメインオブジェクトをそのまま渡さない）
- Blazor ホスト View を返す Controller は `BlazorXxxController` と命名し View のみ返す

### Controller パターン

```csharp
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
        return View(items.Select(item => new TodoViewModel(item.Id, item.Title, item.Done, item.CreatedAt)).ToList());
    }

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

## 7. Web 層（Blazor Server）

**場所**: `MyTodo.Web/BlazorComponents/`

### 設計原則

- インタラクティブな操作（動的な行追加、リアルタイム更新など）に使用する
- **親コンポーネント**がデータと UI 状態の単一の真実の源（Single Source of Truth）
- 子コンポーネント間のデータ連携は `[Parameter]` と `EventCallback` で行う
- DI は `@inject` ディレクティブで宣言する

### コンポーネントツリー設計

```
TodoList.razor（親：状態管理・一覧表示・CRUD 起点）
  ├── TodoFormPanel.razor（子：作成/編集フォーム）
  └── TodoDeleteModal.razor（子：削除確認モーダル）
```

### 親コンポーネントのパターン

```razor
@inject ITodoQueryService QueryService
@inject CreateTodoCommandHandler CreateHandler

@* 一覧表示 *@
@foreach (var item in items)
{
    <div>@item.Title</div>
}

@* 子コンポーネントにパラメータと EventCallback を渡す *@
<TodoFormPanel Todo="editingTodo"
               OnSaved="HandleSaved" />

@code {
    private IReadOnlyList<TodoReadModel> items = [];
    private TodoReadModel? editingTodo;

    protected override async Task OnInitializedAsync()
        => items = await QueryService.GetAllAsync();

    private async Task HandleSaved()
    {
        items = await QueryService.GetAllAsync(); // 再取得
        editingTodo = null;
    }
}
```

### 子コンポーネントのパターン

```razor
@* TodoFormPanel.razor *@
@inject CreateTodoCommandHandler CreateHandler

<EditForm Model="model" OnValidSubmit="HandleSubmit">
    <InputText @bind-Value="model.Title" />
    <button type="submit">保存</button>
</EditForm>

@code {
    [Parameter] public TodoReadModel? Todo { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }

    private FormModel model = new();

    protected override void OnParametersSet()
    {
        if (Todo is not null) model = new FormModel { Title = Todo.Title };
    }

    private async Task HandleSubmit()
    {
        await CreateHandler.HandleAsync(new CreateTodoCommand(model.Title));
        await OnSaved.InvokeAsync();
    }

    private class FormModel { public string Title { get; set; } = ""; }
}
```

---

## 8. Web 層（Web API）

**場所**: `MyTodo.Web/Controllers/` (ApiController)

### 設計原則

- URL は `/api/{entity}` プレフィックスで統一する
- `[ApiController]` + `[Route("api/[controller]")]` を使用する
- レスポンスは ReadModel をそのまま返す（ViewModel への変換は不要）
- 存在しないリソースには `NotFound()` を返す

### API Controller パターン

```csharp
[ApiController]
[Route("api/todos")]
public class TodosApiController : ControllerBase
{
    private readonly ITodoQueryService _queryService;
    private readonly CreateTodoCommandHandler _createHandler;

    public TodosApiController(
        ITodoQueryService queryService,
        CreateTodoCommandHandler createHandler)
    {
        _queryService  = queryService;
        _createHandler = createHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _queryService.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _queryService.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTodoCommand command)
    {
        var result = await _createHandler.HandleAsync(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id.Value }, result);
    }
}
```

---

## 9. CSS（ITCSS + Every Layout）

**場所**: `MyTodo.Web/wwwroot/css/`

### レイヤー構成

| ファイル | 役割 | 追加するもの |
|---|---|---|
| `01-settings.css` | CSS 変数定義 | 新しいデザイントークン（色・スペーシング） |
| `02-tools.css` | 共通スタイル関数相当 | 再利用ヘルパークラス |
| `03-generic.css` | リセット・ノーマライズ | 基本的に変更しない |
| `04-elements.css` | 素の HTML 要素スタイル | タグセレクタのスタイル |
| `05-objects.css` | Every Layout パターン | 新しいレイアウトパターン |
| `06-components.css` | UI コンポーネント | **新規コンポーネントはここ** |
| `07-utilities.css` | 上書き用ユーティリティ | `!important` を使う調整クラス |

### CSS 変数（`01-settings.css`）

```css
:root {
  /* スペーシング（モジュラースケール） */
  --s0: 1rem;
  --s-1: calc(var(--s0) / var(--ratio));
  --s1: calc(var(--s0) * var(--ratio));
  --s2: calc(var(--s1) * var(--ratio));

  /* ボーダー */
  --border-thin: 1px;
  --border-radius: 4px;

  /* コンテンツ幅 */
  --measure: 1280px;

  /* カラー */
  --color-bg:        #ffffff;
  --color-text:      #212529;
  --color-primary:   #0d6efd;
}
```

### Every Layout パターン（`05-objects.css`）

```css
/* Stack: 縦方向のスペーシング */
.stack > * + * { margin-block-start: var(--space, var(--s0)); }

/* Cluster: 横並び・折り返し */
.cluster { display: flex; flex-wrap: wrap; gap: var(--space, var(--s0)); }

/* Center: 水平センタリング */
.center { max-inline-size: var(--measure); margin-inline: auto; }
```

### コンポーネント記述例（`06-components.css`）

```css
/* ── ボタン ──────────────────────────────────────── */
.btn {
  padding-block: var(--s-2);
  padding-inline: var(--s0);
  border-radius: var(--border-radius);
  font-size: var(--s0);
  /* ❌ 数値ハードコード禁止: padding: 4px 16px; */
}

.btn--primary {
  background-color: var(--color-primary);
  color: var(--color-bg);
}
```

### 禁止事項

```css
/* ❌ 数値のハードコーディング */
.bad { padding: 16px; font-size: 14px; }

/* ✅ CSS 変数を使用 */
.good { padding: var(--s0); font-size: var(--s-1); }
```

---

## 10. 単体テスト（xUnit）

**場所**: `TodoApp.Tests/`

### 設計原則

- **テスト対象**: 主に Domain 層（値オブジェクト・エンティティのビジネスロジック）
- テストクラスは `namespace TodoApp.Test.Domain` 配下に配置
- テストはアレンジ（Arrange）→ アクト（Act）→ アサート（Assert）の順に書く

### テストパターン

```csharp
namespace TodoApp.Test.Domain;

public class TodoItemTest
{
    [Fact]
    public void AllCompleted_未完了アイテムがある場合_全て完了になる()
    {
        // Arrange
        var items = new TodoItems(
        [
            new TodoItem(new TodoId(1), new TodoTitle("Task 1"), new TodoIsCompleted(false), new TodoCreatedAt(DateTime.UtcNow)),
            new TodoItem(new TodoId(2), new TodoTitle("Task 2"), new TodoIsCompleted(false), new TodoCreatedAt(DateTime.UtcNow))
        ]);

        // Act
        var completedItems = items.AllCompleted();

        // Assert
        Assert.All(completedItems.Items, item => Assert.True(item.IsCompleted.Value));
    }
}
```

### テストメソッド命名規則

```
[対象メソッド名]_[条件・状態]_[期待される結果]
```

例:
- `AllCompleted_未完了アイテムがある場合_全て完了になる`
- `Create_タイトルが空の場合_例外をスローする`
- または日本語の簡潔な説明でも可: `AllCompletedTest`

### 実行コマンド

```bash
dotnet test TodoApp.Tests/
```

---

## 11. E2E テスト（gauge + Playwright）

**場所**: `MyTodo.E2E/`

### ディレクトリ構成

```
MyTodo.E2E/
├── specs/todos/          ← シナリオ定義（Markdown 形式）
│   ├── todo-create.spec
│   ├── todo-list.spec
│   ├── todo-detail.spec
│   └── todo-api.spec
├── steps/                ← ステップ実装（C# + Playwright）
│   ├── WebStepImplementation.cs   ← ブラウザ操作ステップ
│   ├── WebApiStepImplementation.cs ← API 検証ステップ
│   └── DbStepImplementation.cs    ← DB 操作ステップ
├── fixtures/todos/       ← 期待値 CSV ファイル
│   └── expected/csv/
└── hooks/
    └── SetupAndTeardown.cs ← Suite/Scenario のセットアップ
```

### spec ファイルのパターン（Markdown 形式）

```markdown
# Todo Create

* テーブル "todos" のデータを全て削除する

## Todo が作成できる - CSV 確認
* URL "mvc/todos/create" を開く
* 要素 "input[name='title']" に "New Todo" と入力する
* 要素 "button[type='submit']" をクリックする
* URL "mvc/todos" に遷移している
* 要素 "tbody tr" が "1" 件表示されている
* テーブル "todos" の内容が <table:fixtures/todos/expected/csv/todo-created.csv> と一致している

## Todo が作成できる - テーブル確認
* URL "mvc/todos/create" を開く
* 要素 "input[name='title']" に "New Todo" と入力する
* 要素 "button[type='submit']" をクリックする
* URL "mvc/todos" に遷移している
* テーブル "todos" の内容が以下の通りである

|Id|Title   |Done |
|--|--------|-----|
|1 |New Todo|False|
```

### ステップ実装のパターン

```csharp
public class WebStepImplementation
{
    private static IPage Page =>
        ScenarioDataStore.Get<IPage>("pw:page") 
        ?? throw new InvalidOperationException("Page が初期化されていません");

    [Step("URL <url> を開く")]
    public async Task OpenUrl(string url)
    {
        await Page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    [Step("要素 <selector> に <text> と入力する")]
    public async Task EnterText(string selector, string text)
    {
        await Page.Locator(selector).FillAsync(text);
    }

    [Step("要素 <selector> をクリックする")]
    public async Task ClickElement(string selector)
    {
        await Page.Locator(selector).ClickAsync();
    }

    [Step("要素 <selector> が <count> 件表示されている")]
    public async Task ElementCountIsVisible(string selector, int count)
    {
        var elements = Page.Locator(selector);
        await elements.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        (await elements.CountAsync()).ShouldBe(count);
    }
}
```

### 実行コマンド

```bash
# 特定の spec を実行
gauge run specs/todos/todo-create.spec

# 全 spec を実行
gauge run specs/

# タグを指定して実行
gauge run --tags "create" specs/
```

### ステップ分類

| クラス | 役割 |
|---|---|
| `WebStepImplementation` | Playwright によるブラウザ操作・画面検証 |
| `WebApiStepImplementation` | HTTP クライアントによる API 検証 |
| `DbStepImplementation` | DB の直接操作（テストデータ準備・検証） |
