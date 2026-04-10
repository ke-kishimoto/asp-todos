# パターン B: Blazor リアルタイム CRUD

## 概要

Blazor Server の状態管理を活かし、一覧画面を起点としたリアルタイムな CRUD 操作を実現するパターンです。  
画面遷移なしでフォームパネルやモーダルを表示し、操作後に一覧を即時更新します。

**採用条件**:
- 一覧画面を起点に作成・編集・削除を行いたい（ページ遷移を最小化）
- フォームの表示・非表示など動的な UI 状態管理が必要
- リアルタイムな一覧更新が求められる

**実装例**: `TodoList.razor`, `MyTodo.Web/BlazorComponents/Todos/`

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
    EfTodoRepository.cs        ← 通常リポジトリ（SaveChanges 自前）
  Queries/
    TodoQueryService.cs

MyTodo.Web/
  BlazorComponents/
    Todos/
      TodoList.razor            ← 親：状態管理・一覧表示・CRUD 起点
      TodoFormPanel.razor       ← 子：作成/編集フォーム
      TodoDeleteModal.razor     ← 子：削除確認モーダル
  Controllers/
    BlazorTodosController.cs   ← View のみ返す（データを持たない）
  Views/BlazorTodos/
    Index.cshtml               ← blazor.server.js を含む MVC View
```

---

## コンポーネントツリー設計

```
TodoList.razor（親）
  状態: items（一覧）/ editingTodo（編集中）/ showForm / showDeleteConfirm
    ├── TodoFormPanel.razor（子）
    │     Props: Todo（編集対象 or null）
    │     Event: OnSaved → 親の一覧再取得
    └── TodoDeleteModal.razor（子）
          Props: Todo（削除対象）
          Event: OnConfirmed → 親の削除実行 + 一覧再取得
```

---

## 各層の実装

### 1. Application 層 — ReadModel & QueryService インターフェース

MVC パターンと共通です。[pattern-a-mvc/SKILL.md](../pattern-a-mvc/SKILL.md) の「2. Application 層」を参照してください。

---

### 2. Application 層 — Command & Handler

MVC パターンと共通です。[pattern-a-mvc/SKILL.md](../pattern-a-mvc/SKILL.md) の「3. Application 層」を参照してください。

---

### 3. Infrastructure 層

MVC パターンと共通です。[pattern-a-mvc/SKILL.md](../pattern-a-mvc/SKILL.md) の「4-5. Infrastructure 層」を参照してください。

---

### 4. Web 層 — Blazor 親コンポーネント

```razor
@* MyTodo.Web/BlazorComponents/Todos/TodoList.razor *@
@inject ITodoQueryService QueryService
@inject DeleteTodoCommandHandler DeleteHandler

<cluster-l space="var(--s-1)" align="center">
    <h2>Todo 一覧</h2>
    <button class="btn btn--primary" type="button" @onclick="ShowCreateForm">＋ 新規作成</button>
</cluster-l>

@* フォームパネル（作成・編集） *@
@if (showForm)
{
    <TodoFormPanel Todo="editingTodo" OnSaved="HandleSaved" OnCanceled="HideForm" />
}

@* 削除確認モーダル *@
@if (deletingTodo is not null)
{
    <TodoDeleteModal Todo="deletingTodo" OnConfirmed="HandleDeleted" OnCanceled="() => deletingTodo = null" />
}

@* 一覧テーブル *@
<table class="data-table data-table--hover">
    <thead>
        <tr>
            <th>タイトル</th>
            <th>状態</th>
            <th>作成日</th>
            <th></th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in items)
        {
            <tr>
                <td>@item.Title</td>
                <td>@(item.Done ? "完了" : "未完了")</td>
                <td>@item.CreatedAt.ToString("yyyy/MM/dd")</td>
                <td class="text-center">
                    <cluster-l space="var(--s-2)">
                        <button class="btn btn--sm btn--outline"
                                type="button"
                                @onclick="() => ShowEditForm(item)">編集</button>
                        <button class="btn btn--sm btn--outline-danger"
                                type="button"
                                @onclick="() => deletingTodo = item">削除</button>
                    </cluster-l>
                </td>
            </tr>
        }
    </tbody>
</table>

@code {
    private IReadOnlyList<TodoReadModel> items = [];
    private TodoReadModel? editingTodo;
    private TodoReadModel? deletingTodo;
    private bool showForm = false;

    protected override async Task OnInitializedAsync()
        => items = await QueryService.GetAllAsync();

    private void ShowCreateForm()
    {
        editingTodo = null;
        showForm = true;
    }

    private void ShowEditForm(TodoReadModel item)
    {
        editingTodo = item;
        showForm = true;
    }

    private void HideForm() => showForm = false;

    private async Task HandleSaved()
    {
        items = await QueryService.GetAllAsync();  // 一覧を再取得
        showForm = false;
        editingTodo = null;
    }

    private async Task HandleDeleted()
    {
        await DeleteHandler.HandleAsync(new DeleteTodoCommand(deletingTodo!.Id));
        items = await QueryService.GetAllAsync();
        deletingTodo = null;
    }
}
```

---

### 5. Web 層 — Blazor 子コンポーネント（フォームパネル）

```razor
@* MyTodo.Web/BlazorComponents/Todos/TodoFormPanel.razor *@
@inject CreateTodoCommandHandler CreateHandler
@inject UpdateTodoCommandHandler UpdateHandler

<div class="card">
    <EditForm Model="model" OnValidSubmit="HandleSubmit">
        <DataAnnotationsValidator />

        <div class="stack">
            <div class="form-group">
                <label for="title">タイトル</label>
                <InputText id="title" class="form-input" @bind-Value="model.Title" />
                <ValidationMessage For="() => model.Title" />
            </div>
            @if (Todo is not null)
            {
                <div class="form-group">
                    <label>
                        <InputCheckbox @bind-Value="model.Done" /> 完了
                    </label>
                </div>
            }
        </div>

        <cluster-l space="var(--s-1)" style="margin-block-start: var(--s0)">
            <button class="btn btn--primary" type="submit">保存</button>
            <button class="btn btn--outline" type="button" @onclick="OnCanceled">キャンセル</button>
        </cluster-l>
    </EditForm>
</div>

@code {
    [Parameter] public TodoReadModel? Todo { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }
    [Parameter] public EventCallback OnCanceled { get; set; }

    private FormModel model = new();

    protected override void OnParametersSet()
    {
        model = Todo is not null
            ? new FormModel { Title = Todo.Title, Done = Todo.Done }
            : new FormModel();
    }

    private async Task HandleSubmit()
    {
        if (Todo is null)
            await CreateHandler.HandleAsync(new CreateTodoCommand(model.Title));
        else
            await UpdateHandler.HandleAsync(new UpdateTodoCommand(Todo.Id, model.Title, model.Done));

        await OnSaved.InvokeAsync();
    }

    private class FormModel
    {
        [Required(ErrorMessage = "タイトルは必須です")]
        public string Title { get; set; } = "";
        public bool Done { get; set; }
    }
}
```

---

### 6. Web 層 — Blazor 子コンポーネント（削除確認モーダル）

```razor
@* MyTodo.Web/BlazorComponents/Todos/TodoDeleteModal.razor *@
<div class="modal-overlay">
    <div class="card" style="max-inline-size: 400px">
        <p>「<strong>@Todo?.Title</strong>」を削除しますか？</p>
        <cluster-l space="var(--s-1)" style="margin-block-start: var(--s0)">
            <button class="btn btn--danger" type="button" @onclick="OnConfirmed">削除する</button>
            <button class="btn btn--outline" type="button" @onclick="OnCanceled">キャンセル</button>
        </cluster-l>
    </div>
</div>

@code {
    [Parameter] public TodoReadModel? Todo { get; set; }
    [Parameter] public EventCallback OnConfirmed { get; set; }
    [Parameter] public EventCallback OnCanceled { get; set; }
}
```

---

### 7. Web 層 — MVC ホストコントローラー & View

Blazor コンポーネントを MVC View 内に埋め込みます。  
Controller はデータを持たず View を返すだけにしてください。

```csharp
// MyTodo.Web/Controllers/BlazorTodosController.cs
[Route("blazor/todos")]
public class BlazorTodosController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
```

```cshtml
@* MyTodo.Web/Views/BlazorTodos/Index.cshtml *@
@{
    ViewData["Title"] = "Todo 管理（Blazor）";
}

<component type="typeof(MyTodo.Web.BlazorComponents.Todos.TodoList)"
           render-mode="ServerPrerendered" />

@section Scripts {
    <script src="_framework/blazor.server.js"></script>
}
```

> **注意**: `blazor.server.js` の読み込みを `@section Scripts` に含めることが必須です。  
> これがないと SignalR 接続が確立されず、ボタン等のインタラクションが動作しません。

---

### 8. DI 登録

MVC パターンと共通です。[pattern-a-mvc/SKILL.md](../pattern-a-mvc/SKILL.md) の「8. DI 登録」を参照してください。
