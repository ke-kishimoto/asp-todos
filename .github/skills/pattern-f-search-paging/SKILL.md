# パターン F: 多条件検索＋ページング

## 概要

業務一覧画面で必要な**複数条件の絞り込み・ページング・ソート**を実装するパターンです。  
条件は `SearchCondition` record にまとめ、QueryService の LINQ で動的フィルタを組み立て、`PagedResult<T>` で結果を返します。

**採用条件**:
- 一覧に絞り込み条件が複数ある（日付範囲・ステータス・顧客名など）
- 件数が多く全件取得でなくページングが必要
- ソート項目・順序をユーザーが変更できる

**典型例**: 受注一覧、売上レポート、顧客台帳

---

## ファイル構成

```
MyTodo.Application/
  Queries/
    PagedResult.cs              ← ページング結果の汎用ラッパー（共有）
  Queries/Orders/
    IOrderQueryService.cs       ← SearchAsync(condition) を持つ
    OrderSearchCondition.cs     ← 検索条件 record
    OrderReadModel.cs

MyTodo.Infrastructure/
  Queries/
    OrderQueryService.cs        ← IQueryable で動的フィルタ構築

MyTodo.Web/
  Controllers/
    OrdersController.cs         ← GET /mvc/orders?status=Approved&page=2
  Models/
    OrderSearchViewModel.cs     ← 検索フォーム + 結果をまとめた ViewModel
  BlazorComponents/Orders/
    OrderSearchPanel.razor      ← Blazor の場合：リアルタイム絞り込み
```

---

## 各層の実装

### 1. Application 層 — 汎用ページング結果

```csharp
// MyTodo.Application/Queries/PagedResult.cs
public record PagedResult<T>(
    IReadOnlyList<T> Items,    // 現ページのデータ
    int TotalCount,            // 総件数
    int Page,                  // 現在ページ（1 始まり）
    int PageSize)              // 1 ページあたりの件数
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
```

---

### 2. Application 層 — 検索条件 record

検索条件はすべて nullable にし、null の場合はフィルタを適用しません。

```csharp
// MyTodo.Application/Queries/Orders/OrderSearchCondition.cs
public record OrderSearchCondition(
    string? OrderNumber = null,        // 部分一致
    int? CustomerId = null,
    string? CustomerName = null,       // 部分一致
    OrderStatus? Status = null,
    DateTime? OrderDateFrom = null,    // 受注日（開始）
    DateTime? OrderDateTo = null,      // 受注日（終了）
    decimal? TotalAmountMin = null,
    decimal? TotalAmountMax = null,
    string SortBy = "OrderDate",       // ソート列名
    bool Descending = true,            // ソート方向
    int Page = 1,
    int PageSize = 20);
```

---

### 3. Application 層 — QueryService インターフェース

```csharp
// MyTodo.Application/Queries/Orders/IOrderQueryService.cs
public interface IOrderQueryService
{
    Task<PagedResult<OrderReadModel>> SearchAsync(OrderSearchCondition condition);
    Task<OrderReadModel?> GetByIdAsync(int id);
}
```

---

### 4. Infrastructure 層 — 動的フィルタ構築

`IQueryable` のチェーンで null の条件をスキップします。

```csharp
// MyTodo.Infrastructure/Queries/OrderQueryService.cs
public class OrderQueryService : IOrderQueryService
{
    private readonly AppDbContext _db;
    public OrderQueryService(AppDbContext db) => _db = db;

    public async Task<PagedResult<OrderReadModel>> SearchAsync(OrderSearchCondition cond)
    {
        // ベースクエリ
        IQueryable<OrderEntity> query = _db.Orders.Include(o => o.Customer);

        // ─── 動的フィルタ ─────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(cond.OrderNumber))
            query = query.Where(o => o.OrderNumber.Contains(cond.OrderNumber));

        if (cond.CustomerId.HasValue)
            query = query.Where(o => o.CustomerId == cond.CustomerId.Value);

        if (!string.IsNullOrWhiteSpace(cond.CustomerName))
            query = query.Where(o => o.Customer.Name.Contains(cond.CustomerName));

        if (cond.Status.HasValue)
            query = query.Where(o => o.Status == cond.Status.Value);

        if (cond.OrderDateFrom.HasValue)
            query = query.Where(o => o.OrderDate >= cond.OrderDateFrom.Value);

        if (cond.OrderDateTo.HasValue)
            query = query.Where(o => o.OrderDate <= cond.OrderDateTo.Value);

        if (cond.TotalAmountMin.HasValue)
            query = query.Where(o => o.Lines.Sum(l => l.Quantity * l.UnitPrice) >= cond.TotalAmountMin.Value);

        if (cond.TotalAmountMax.HasValue)
            query = query.Where(o => o.Lines.Sum(l => l.Quantity * l.UnitPrice) <= cond.TotalAmountMax.Value);

        // ─── 総件数（ページング前） ────────────────────────────────
        var totalCount = await query.CountAsync();

        // ─── ソート ──────────────────────────────────────────────
        query = (cond.SortBy, cond.Descending) switch
        {
            ("OrderDate",    true)  => query.OrderByDescending(o => o.OrderDate),
            ("OrderDate",    false) => query.OrderBy(o => o.OrderDate),
            ("TotalAmount",  true)  => query.OrderByDescending(o => o.Lines.Sum(l => l.Quantity * l.UnitPrice)),
            ("TotalAmount",  false) => query.OrderBy(o => o.Lines.Sum(l => l.Quantity * l.UnitPrice)),
            ("CustomerName", true)  => query.OrderByDescending(o => o.Customer.Name),
            ("CustomerName", false) => query.OrderBy(o => o.Customer.Name),
            _                       => query.OrderByDescending(o => o.OrderDate)  // デフォルト
        };

        // ─── ページング ──────────────────────────────────────────
        var items = await query
            .Skip((cond.Page - 1) * cond.PageSize)
            .Take(cond.PageSize)
            .Select(o => new OrderReadModel(
                o.Id,
                o.OrderNumber,
                o.Customer.Name,
                o.OrderDate,
                o.Status.ToString(),
                GetStatusLabel(o.Status),
                o.Lines.Sum(l => (decimal)l.Quantity * l.UnitPrice),
                []))   // 一覧では明細行は不要
            .ToListAsync();

        return new PagedResult<OrderReadModel>(items, totalCount, cond.Page, cond.PageSize);
    }
}
```

---

### 5. Web 層 — MVC Controller（GET パラメータ）

検索条件をクエリストリングで受け取り、ViewModel に組み立てます。

```csharp
// MyTodo.Web/Controllers/OrdersController.cs
[Route("mvc/orders")]
public class OrdersController : Controller
{
    private readonly IOrderQueryService _queryService;
    public OrdersController(IOrderQueryService queryService) => _queryService = queryService;

    // GET /mvc/orders?status=Approved&orderDateFrom=2026-01-01&page=2
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? orderNumber = null,
        string? customerName = null,
        string? status = null,
        DateTime? orderDateFrom = null,
        DateTime? orderDateTo = null,
        string sortBy = "OrderDate",
        bool desc = true,
        int page = 1)
    {
        OrderStatus? statusEnum = Enum.TryParse<OrderStatus>(status, out var s) ? s : null;

        var condition = new OrderSearchCondition(
            OrderNumber: orderNumber,
            CustomerName: customerName,
            Status: statusEnum,
            OrderDateFrom: orderDateFrom,
            OrderDateTo: orderDateTo,
            SortBy: sortBy,
            Descending: desc,
            Page: page);

        var result = await _queryService.SearchAsync(condition);

        return View(new OrderSearchViewModel(condition, result));
    }
}
```

```csharp
// MyTodo.Web/Models/OrderSearchViewModel.cs
public record OrderSearchViewModel(
    OrderSearchCondition Condition,
    PagedResult<OrderReadModel> Result);
```

---

### 6. View — 検索フォーム + ページング

```cshtml
@* Views/Orders/Index.cshtml *@
@model OrderSearchViewModel

@* 検索フォーム（GET で送信） *@
<form method="get" asp-action="Index">
    <div class="cluster">
        <input class="form-input" type="text" name="orderNumber"
               value="@Model.Condition.OrderNumber" placeholder="受注番号" />
        <input class="form-input" type="text" name="customerName"
               value="@Model.Condition.CustomerName" placeholder="顧客名" />
        <select class="form-input" name="status">
            <option value="">すべて</option>
            @foreach (var s in Enum.GetValues<OrderStatus>())
            {
                <option value="@s" selected="@(Model.Condition.Status == s)">@s</option>
            }
        </select>
        <input class="form-input" type="date" name="orderDateFrom"
               value="@Model.Condition.OrderDateFrom?.ToString("yyyy-MM-dd")" />
        <input class="form-input" type="date" name="orderDateTo"
               value="@Model.Condition.OrderDateTo?.ToString("yyyy-MM-dd")" />
        <button class="btn btn--primary" type="submit">検索</button>
    </div>
    @* 現在のページ・ソート条件を hidden で保持 *@
    <input type="hidden" name="sortBy" value="@Model.Condition.SortBy" />
    <input type="hidden" name="desc" value="@Model.Condition.Descending" />
</form>

@* 件数表示 *@
<p class="text-muted">
    @Model.Result.TotalCount 件中
    @((Model.Result.Page - 1) * Model.Result.PageSize + 1) ～
    @Math.Min(Model.Result.Page * Model.Result.PageSize, Model.Result.TotalCount) 件目
</p>

@* 一覧テーブル *@
<table class="data-table data-table--hover">
    <thead>
        <tr>
            @* ソートリンク *@
            <th><a asp-action="Index" asp-all-route-data="SortLink("OrderNumber")">受注番号</a></th>
            <th>顧客名</th>
            <th><a asp-action="Index" asp-all-route-data="SortLink("OrderDate")">受注日</a></th>
            <th>ステータス</th>
            <th class="text-end"><a asp-action="Index" asp-all-route-data="SortLink("TotalAmount")">合計金額</a></th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in Model.Result.Items)
        {
            <tr>
                <td><a asp-action="Details" asp-route-id="@item.Id">@item.OrderNumber</a></td>
                <td>@item.CustomerName</td>
                <td>@item.OrderDate.ToString("yyyy/MM/dd")</td>
                <td><span class="badge badge--@item.Status.ToLower()">@item.StatusLabel</span></td>
                <td class="text-end">@item.TotalAmount.ToString("N0")</td>
            </tr>
        }
    </tbody>
</table>

@* ページングナビゲーション *@
<nav class="cluster" aria-label="ページ">
    @if (Model.Result.HasPreviousPage)
    {
        <a class="btn btn--outline" asp-action="Index"
           asp-route-page="@(Model.Result.Page - 1)">前へ</a>
    }
    @for (int p = 1; p <= Model.Result.TotalPages; p++)
    {
        <a class="btn @(p == Model.Result.Page ? "btn--primary" : "btn--outline")"
           asp-action="Index" asp-route-page="@p">@p</a>
    }
    @if (Model.Result.HasNextPage)
    {
        <a class="btn btn--outline" asp-action="Index"
           asp-route-page="@(Model.Result.Page + 1)">次へ</a>
    }
</nav>
```

---

### 7. Web 層 — Blazor によるリアルタイム絞り込み（オプション）

入力のたびに再検索したい場合は Blazor で実装します。

```razor
@* MyTodo.Web/BlazorComponents/Orders/OrderSearchPanel.razor *@
@inject IOrderQueryService QueryService

@* 検索フォーム *@
<div class="cluster">
    <input class="form-input" type="text" placeholder="顧客名"
           @bind="condition.CustomerName" @bind:event="oninput"
           @oninput="OnConditionChanged" />
    <select class="form-input" @onchange="e => { condition = condition with { Status = ParseStatus(e.Value) }; _ = SearchAsync(); }">
        <option value="">すべて</option>
        @foreach (var s in Enum.GetValues<OrderStatus>())
        {
            <option value="@s">@s</option>
        }
    </select>
</div>

@* 件数・一覧表示（省略） *@

@code {
    private OrderSearchCondition condition = new();
    private PagedResult<OrderReadModel>? result;

    protected override async Task OnInitializedAsync() => await SearchAsync();

    private async Task OnConditionChanged()
    {
        condition = condition with { Page = 1 };  // 条件変更でページ先頭に戻す
        await SearchAsync();
    }

    private async Task SearchAsync()
    {
        result = await QueryService.SearchAsync(condition);
    }

    private static OrderStatus? ParseStatus(object? val)
        => Enum.TryParse<OrderStatus>(val?.ToString(), out var s) ? s : null;
}
```

---

## 設計上の注意点

| 注意点 | 理由 |
|---|---|
| `SearchCondition` は record かつ完全 null 許容 | 条件の組み合わせが自由になり、デフォルト値で「全件」になる |
| 総件数は `CountAsync()` を Skip/Take の前に取得 | ページ数の計算に必要 |
| ソートは `switch` 式で列挙 | `dynamic` ソートより型安全で EF Core の変換が保証される |
| ページングは 1 始まりで統一 | View の表示 (`$"{ page }"`) と一致させる |
| Blazor (`oninput`) でリアルタイム検索する場合は debounce を検討 | キー入力ごとに DB クエリが走るのを防ぐ |
