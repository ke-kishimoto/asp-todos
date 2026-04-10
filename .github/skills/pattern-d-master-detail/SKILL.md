# パターン D: 親子一体 CRUD（マスター明細）

## 概要

受注ヘッダー＋受注明細行のように、**親エンティティと複数の子エンティティを 1 画面・1 トランザクションで作成・編集**するパターンです。  
集約ルート（Aggregate Root）としてドメインを設計し、親子をアトミックに保存します。

**採用条件**:
- 1 つの業務伝票が「ヘッダー」と「明細行（可変数）」で構成される
- 明細行の追加・削除・変更を親と同時に保存したい
- 明細の小計・合計などの集約演算をドメイン層に持たせたい

**典型例**: 受注伝票、請求書、発注書、見積書

---

## ファイル構成

```
MyTodo.Application/
  Repositories/
    IOrderRepository.cs         ← 集約ルートのリポジトリ IF（親子をまとめて操作）
  Queries/Orders/
    IOrderQueryService.cs
    OrderReadModel.cs           ← OrderLineReadModel を子リストとして含む
  Commands/Orders/
    CreateOrderCommand.cs       ← 親 + 子リストを 1 コマンドにまとめる
    UpdateOrderCommand.cs
    DeleteOrderCommand.cs

MyTodo.Infrastructure/
  Repositories/
    EfOrderRepository.cs        ← EF Core のナビゲーションで親子を一括保存
  Queries/
    OrderQueryService.cs

MyTodo.Web/
  BlazorComponents/Orders/
    OrderForm.razor             ← 親フォーム + 明細行テーブルを 1 コンポーネントに
  Controllers/
    OrdersController.cs         ← MVC または Blazor ホスト
  Views/Orders/
    Create.cshtml / Edit.cshtml
```

---

## 各層の実装

### 1. Domain 層

集約ルートと子エンティティを同じ集約内で管理します。

```csharp
// 値オブジェクト
public record OrderId(int Value);
public record OrderNumber(string Value);
public record CustomerId(int Value);
public record ProductId(int Value);

// 子エンティティ（明細行）
public record OrderLine(
    int LineNo,
    ProductId ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice)
{
    public decimal SubTotal => Quantity * UnitPrice;
}

// 集約ルート（親エンティティ）
public record Order(
    OrderId Id,
    OrderNumber Number,
    CustomerId CustomerId,
    DateTime OrderDate,
    IReadOnlyList<OrderLine> Lines)
{
    // 集約演算：合計金額
    public decimal TotalAmount => Lines.Sum(l => l.SubTotal);

    // 明細の追加（ドメインロジック）
    public Order AddLine(OrderLine line)
        => this with { Lines = [.. Lines, line] };

    // 明細の削除
    public Order RemoveLine(int lineNo)
        => this with { Lines = Lines.Where(l => l.LineNo != lineNo).ToList() };
}
```

---

### 2. Application 層 — ReadModel

子リストをネストした ReadModel を定義します。

```csharp
// MyTodo.Application/Queries/Orders/OrderReadModel.cs
public record OrderLineReadModel(
    int LineNo,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal SubTotal);

public record OrderReadModel(
    int Id,
    string OrderNumber,
    int CustomerId,
    string CustomerName,
    DateTime OrderDate,
    decimal TotalAmount,
    IReadOnlyList<OrderLineReadModel> Lines);
```

```csharp
// MyTodo.Application/Queries/Orders/IOrderQueryService.cs
public interface IOrderQueryService
{
    Task<IReadOnlyList<OrderReadModel>> GetAllAsync();
    Task<OrderReadModel?> GetByIdAsync(int id);
}
```

---

### 3. Application 層 — Command & Handler

親と子を 1 つの Command にまとめます。

```csharp
// MyTodo.Application/Commands/Orders/CreateOrderCommand.cs

// 明細行の入力データ
public record OrderLineInput(int ProductId, string ProductName, int Quantity, decimal UnitPrice);

// 親 + 子リストを 1 コマンドに
public record CreateOrderCommand(
    int CustomerId,
    DateTime OrderDate,
    IReadOnlyList<OrderLineInput> Lines);

public class CreateOrderCommandHandler
{
    private readonly IOrderRepository _repo;

    public CreateOrderCommandHandler(IOrderRepository repo) => _repo = repo;

    public async Task<Order> HandleAsync(CreateOrderCommand command)
    {
        // 保存前バリデーション（明細が 0 件は不可）
        if (command.Lines.Count == 0)
            throw new InvalidOperationException("明細行が 1 件以上必要です");

        var lines = command.Lines
            .Select((l, i) => new OrderLine(i + 1, new ProductId(l.ProductId), l.ProductName, l.Quantity, l.UnitPrice))
            .ToList();

        return await _repo.AddAsync(command.CustomerId, command.OrderDate, lines);
    }
}
```

```csharp
// MyTodo.Application/Commands/Orders/UpdateOrderCommand.cs
public record UpdateOrderLineInput(int LineNo, int ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record UpdateOrderCommand(
    int Id,
    int CustomerId,
    DateTime OrderDate,
    IReadOnlyList<UpdateOrderLineInput> Lines);

public class UpdateOrderCommandHandler
{
    private readonly IOrderRepository _repo;
    public UpdateOrderCommandHandler(IOrderRepository repo) => _repo = repo;

    public async Task<bool> HandleAsync(UpdateOrderCommand command)
    {
        if (command.Lines.Count == 0)
            throw new InvalidOperationException("明細行が 1 件以上必要です");

        return await _repo.UpdateAsync(command.Id, command.CustomerId, command.OrderDate,
            command.Lines.Select((l, i) =>
                new OrderLine(i + 1, new ProductId(l.ProductId), l.ProductName, l.Quantity, l.UnitPrice)).ToList());
    }
}
```

---

### 4. Application 層 — Repository インターフェース

集約ルートごと保存するため、親子を同時に受け取るインターフェースにします。

```csharp
// MyTodo.Application/Repositories/IOrderRepository.cs
public interface IOrderRepository
{
    Task<Order> AddAsync(int customerId, DateTime orderDate, IReadOnlyList<OrderLine> lines);
    Task<bool> UpdateAsync(int id, int customerId, DateTime orderDate, IReadOnlyList<OrderLine> lines);
    Task<bool> DeleteAsync(int id);
}
```

---

### 5. Infrastructure 層 — Repository 実装

EF Core のナビゲーションプロパティを使って親子を一括保存します。

```csharp
// MyTodo.Infrastructure/Repositories/EfOrderRepository.cs
public class EfOrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    public EfOrderRepository(AppDbContext db) => _db = db;

    public async Task<Order> AddAsync(int customerId, DateTime orderDate, IReadOnlyList<OrderLine> lines)
    {
        var entity = new OrderEntity
        {
            CustomerId = customerId,
            OrderDate = orderDate,
            OrderNumber = GenerateOrderNumber(),
            Lines = lines.Select(l => new OrderLineEntity
            {
                LineNo = l.LineNo,
                ProductId = l.ProductId.Value,
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };
        _db.Orders.Add(entity);
        await _db.SaveChangesAsync();  // 親子まとめて 1 回の SaveChanges
        return MapToOrder(entity);
    }

    public async Task<bool> UpdateAsync(int id, int customerId, DateTime orderDate, IReadOnlyList<OrderLine> lines)
    {
        // Include で子を一括読み込み
        var entity = await _db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id);
        if (entity is null) return false;

        entity.CustomerId = customerId;
        entity.OrderDate = orderDate;

        // 既存明細を消して再追加（差分管理より単純で誤りが少ない）
        entity.Lines.Clear();
        foreach (var l in lines)
        {
            entity.Lines.Add(new OrderLineEntity
            {
                LineNo = l.LineNo,
                ProductId = l.ProductId.Value,
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.Orders.FindAsync(id);
        if (entity is null) return false;
        _db.Orders.Remove(entity);  // Cascade Delete で明細も削除される
        await _db.SaveChangesAsync();
        return true;
    }

    private static string GenerateOrderNumber()
        => $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

    private static Order MapToOrder(OrderEntity e)
        => new Order(
            new OrderId(e.Id),
            new OrderNumber(e.OrderNumber),
            new CustomerId(e.CustomerId),
            e.OrderDate,
            e.Lines.Select(l => new OrderLine(l.LineNo, new ProductId(l.ProductId),
                l.ProductName, l.Quantity, l.UnitPrice)).ToList());
}
```

EF Core の Cascade Delete を有効にするには `OnModelCreating` で設定します。

```csharp
// InfrastructureDbContext（OnModelCreating）
modelBuilder.Entity<OrderEntity>()
    .HasMany(o => o.Lines)
    .WithOne(l => l.Order)
    .HasForeignKey(l => l.OrderId)
    .OnDelete(DeleteBehavior.Cascade);
```

---

### 6. Web 層 — Blazor コンポーネント

親フォームと明細行テーブルを 1 つのコンポーネントで管理します。

```razor
@* MyTodo.Web/BlazorComponents/Orders/OrderForm.razor *@
@inject IOrderQueryService QueryService
@inject CreateOrderCommandHandler CreateHandler
@inject UpdateOrderCommandHandler UpdateHandler
@inject NavigationManager Nav

<EditForm Model="model" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />

    @* ─── ヘッダー部 ─────────────────── *@
    <div class="stack">
        <div class="form-group">
            <label>顧客 ID</label>
            <InputNumber class="form-input" @bind-Value="model.CustomerId" />
        </div>
        <div class="form-group">
            <label>受注日</label>
            <InputDate class="form-input" @bind-Value="model.OrderDate" />
        </div>
    </div>

    @* ─── 明細行テーブル ─────────────── *@
    <table class="data-table" style="margin-block-start: var(--s0)">
        <thead>
            <tr>
                <th>商品名</th>
                <th style="width:80px">数量</th>
                <th style="width:100px">単価</th>
                <th style="width:100px">小計</th>
                <th style="width:60px"></th>
            </tr>
        </thead>
        <tbody>
            @foreach (var line in model.Lines)
            {
                var l = line;  @* クロージャ対策 *@
                <tr>
                    <td>
                        <input class="form-input" type="text"
                               value="@l.ProductName"
                               @oninput="e => l.ProductName = e.Value?.ToString() ?? string.Empty" />
                    </td>
                    <td>
                        <input class="form-input" type="number" min="1"
                               value="@l.Quantity"
                               @oninput="e => { if (int.TryParse(e.Value?.ToString(), out var v)) l.Quantity = v; }" />
                    </td>
                    <td>
                        <input class="form-input" type="number" min="0" step="0.01"
                               value="@l.UnitPrice"
                               @oninput="e => { if (decimal.TryParse(e.Value?.ToString(), out var v)) l.UnitPrice = v; }" />
                    </td>
                    <td class="text-end">@((l.Quantity * l.UnitPrice).ToString("N0"))</td>
                    <td>
                        <button class="btn btn--sm btn--outline-danger" type="button"
                                @onclick="() => model.Lines.Remove(l)">削除</button>
                    </td>
                </tr>
            }
        </tbody>
        <tfoot>
            <tr>
                <td colspan="3" class="text-end"><strong>合計</strong></td>
                <td class="text-end"><strong>@model.Lines.Sum(l => l.Quantity * l.UnitPrice).ToString("N0")</strong></td>
                <td></td>
            </tr>
        </tfoot>
    </table>

    <button class="btn btn--outline" type="button"
            style="margin-block-start: var(--s-1)"
            @onclick="AddLine">＋ 明細追加</button>

    <cluster-l space="var(--s-1)" style="margin-block-start: var(--s1)">
        <button class="btn btn--primary" type="submit">保存</button>
        <button class="btn btn--outline" type="button" @onclick="Cancel">キャンセル</button>
    </cluster-l>
</EditForm>

@code {
    [Parameter] public int? OrderId { get; set; }  // null = 新規作成

    private OrderFormModel model = new();

    protected override async Task OnParametersSetAsync()
    {
        if (OrderId.HasValue)
        {
            var order = await QueryService.GetByIdAsync(OrderId.Value);
            if (order is not null)
            {
                model = new OrderFormModel
                {
                    CustomerId = order.CustomerId,
                    OrderDate = order.OrderDate,
                    Lines = order.Lines.Select(l => new LineModel
                    {
                        ProductId = l.ProductId,
                        ProductName = l.ProductName,
                        Quantity = l.Quantity,
                        UnitPrice = l.UnitPrice
                    }).ToList()
                };
            }
        }
    }

    private void AddLine()
        => model.Lines.Add(new LineModel { Quantity = 1 });

    private async Task HandleSubmit()
    {
        if (OrderId.HasValue)
        {
            await UpdateHandler.HandleAsync(new UpdateOrderCommand(
                OrderId.Value, model.CustomerId, model.OrderDate,
                model.Lines.Select(l => new UpdateOrderLineInput(0, l.ProductId, l.ProductName, l.Quantity, l.UnitPrice)).ToList()));
        }
        else
        {
            await CreateHandler.HandleAsync(new CreateOrderCommand(
                model.CustomerId, model.OrderDate,
                model.Lines.Select(l => new OrderLineInput(l.ProductId, l.ProductName, l.Quantity, l.UnitPrice)).ToList()));
        }
        Nav.NavigateTo("/mvc/orders");
    }

    private void Cancel() => Nav.NavigateTo("/mvc/orders");

    private class OrderFormModel
    {
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Today;
        public List<LineModel> Lines { get; set; } = [];
    }

    private class LineModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }
}
```

---

### 7. DI 登録

`InfrastructureServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IOrderRepository, EfOrderRepository>();
services.AddScoped<IOrderQueryService, OrderQueryService>();
```

`ApplicationServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<CreateOrderCommandHandler>();
services.AddScoped<UpdateOrderCommandHandler>();
services.AddScoped<DeleteOrderCommandHandler>();
```

---

## 設計上の注意点

| 注意点 | 理由 |
|---|---|
| 更新時は明細を削除→再挿入 | 差分マージより実装が単純で不整合が起きにくい |
| Cascade Delete を必ず設定する | 親削除時に明細が孤立レコードにならないようにする |
| 明細 0 件は Application 層で弾く | DB に空の伝票が残ることを防ぐ |
| `SaveChangesAsync` は 1 回だけ | 親子の整合性を保つため、まとめて 1 トランザクションで保存 |
