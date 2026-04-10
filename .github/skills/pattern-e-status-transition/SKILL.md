# パターン E: ステータス遷移（ビジネスドキュメントのライフサイクル）

## 概要

業務伝票が「下書き → 承認待ち → 承認済み → 出荷済み → 請求済み」のように、**定義された状態機械に従って遷移**するパターンです。  
遷移ルールをドメイン層に持たせ、不正な遷移はドメイン層で拒否します。

**採用条件**:
- エンティティが複数のステータスを持ち、遷移に業務ルールがある
- ステータスによって画面の表示内容・操作可否が変わる
- 誰が・いつ・どのステータスに変えたかの記録が必要

**典型例**: 受注承認フロー、請求ステータス管理、入荷検収フロー

---

## ファイル構成

```
MyTodo.Application/
  Repositories/
    IOrderRepository.cs         ← ステータス更新メソッドを持つ
  Commands/Orders/
    ApproveOrderCommand.cs      ← アクション単位のコマンド
    ShipOrderCommand.cs
    InvoiceOrderCommand.cs
    RejectOrderCommand.cs

MyTodo.Web/
  Controllers/
    OrdersController.cs         ← POST /mvc/orders/{id}/approve 等
```

---

## 各層の実装

### 1. Domain 層 — ステータスと遷移ルール

```csharp
// MyTodo.Domain/OrderStatus.cs
public enum OrderStatus
{
    Draft = 0,          // 下書き
    PendingApproval,    // 承認待ち
    Approved,           // 承認済み
    Shipped,            // 出荷済み
    Invoiced,           // 請求済み
    Rejected,           // 却下
    Cancelled           // キャンセル
}

public static class OrderStatusTransition
{
    // 各ステータスから遷移可能なステータスの定義
    private static readonly Dictionary<OrderStatus, IReadOnlyList<OrderStatus>> AllowedTransitions = new()
    {
        [OrderStatus.Draft]           = [OrderStatus.PendingApproval, OrderStatus.Cancelled],
        [OrderStatus.PendingApproval] = [OrderStatus.Approved, OrderStatus.Rejected],
        [OrderStatus.Approved]        = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped]         = [OrderStatus.Invoiced],
        [OrderStatus.Invoiced]        = [],   // 終端ステータス
        [OrderStatus.Rejected]        = [OrderStatus.Draft],     // 差し戻し → 下書きに戻す
        [OrderStatus.Cancelled]       = [],   // 終端ステータス
    };

    // 遷移可能かチェック（不正な遷移は例外）
    public static void ValidateTransition(OrderStatus current, OrderStatus next)
    {
        if (!AllowedTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(next))
            throw new InvalidOperationException(
                $"ステータス '{current}' から '{next}' への遷移は許可されていません");
    }

    // ステータスが指定のアクション可能かどうか
    public static bool CanTransitionTo(OrderStatus current, OrderStatus next)
        => AllowedTransitions.TryGetValue(current, out var allowed) && allowed.Contains(next);
}
```

ドメインエンティティにも遷移メソッドを持たせます。

```csharp
// MyTodo.Domain/Order.cs（ステータス遷移メソッドを追加）
public record Order(OrderId Id, OrderNumber Number, CustomerId CustomerId,
    DateTime OrderDate, OrderStatus Status, IReadOnlyList<OrderLine> Lines)
{
    public Order Approve()
    {
        OrderStatusTransition.ValidateTransition(Status, OrderStatus.Approved);
        return this with { Status = OrderStatus.Approved };
    }

    public Order Submit()
    {
        OrderStatusTransition.ValidateTransition(Status, OrderStatus.PendingApproval);
        return this with { Status = OrderStatus.PendingApproval };
    }

    public Order Ship()
    {
        OrderStatusTransition.ValidateTransition(Status, OrderStatus.Shipped);
        return this with { Status = OrderStatus.Shipped };
    }

    public Order Invoice()
    {
        OrderStatusTransition.ValidateTransition(Status, OrderStatus.Invoiced);
        return this with { Status = OrderStatus.Invoiced };
    }

    public Order Reject()
    {
        OrderStatusTransition.ValidateTransition(Status, OrderStatus.Rejected);
        return this with { Status = OrderStatus.Rejected };
    }

    public Order Cancel()
    {
        OrderStatusTransition.ValidateTransition(Status, OrderStatus.Cancelled);
        return this with { Status = OrderStatus.Cancelled };
    }
}
```

---

### 2. Application 層 — アクション単位の Command & Handler

1 つのアクション（承認・出荷など）につき 1 つのコマンドを定義します。

```csharp
// MyTodo.Application/Commands/Orders/ApproveOrderCommand.cs
public record ApproveOrderCommand(int OrderId);

public class ApproveOrderCommandHandler
{
    private readonly IOrderRepository _repo;
    public ApproveOrderCommandHandler(IOrderRepository repo) => _repo = repo;

    public async Task HandleAsync(ApproveOrderCommand command)
    {
        var order = await _repo.GetByIdAsync(command.OrderId)
            ?? throw new KeyNotFoundException($"受注 ID {command.OrderId} が見つかりません");

        // ドメインオブジェクトのメソッドで遷移（不正遷移は Domain 層で例外）
        var updated = order.Approve();
        await _repo.UpdateStatusAsync(updated.Id.Value, updated.Status);
    }
}
```

```csharp
// MyTodo.Application/Commands/Orders/ShipOrderCommand.cs
public record ShipOrderCommand(int OrderId, DateTime ShippedAt);

public class ShipOrderCommandHandler
{
    private readonly IOrderRepository _repo;
    public ShipOrderCommandHandler(IOrderRepository repo) => _repo = repo;

    public async Task HandleAsync(ShipOrderCommand command)
    {
        var order = await _repo.GetByIdAsync(command.OrderId)
            ?? throw new KeyNotFoundException($"受注 ID {command.OrderId} が見つかりません");

        var updated = order.Ship();
        await _repo.UpdateStatusAsync(updated.Id.Value, updated.Status, command.ShippedAt);
    }
}
```

---

### 3. Application 層 — Repository インターフェース

```csharp
// MyTodo.Application/Repositories/IOrderRepository.cs
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<Order> AddAsync(int customerId, DateTime orderDate, IReadOnlyList<OrderLine> lines);
    Task<bool> UpdateAsync(int id, int customerId, DateTime orderDate, IReadOnlyList<OrderLine> lines);
    Task UpdateStatusAsync(int id, OrderStatus newStatus, DateTime? actionDate = null);
    Task<bool> DeleteAsync(int id);
}
```

---

### 4. Infrastructure 層 — UpdateStatus 実装

```csharp
// MyTodo.Infrastructure/Repositories/EfOrderRepository.cs（statusUpdate メソッドのみ抜粋）
public async Task UpdateStatusAsync(int id, OrderStatus newStatus, DateTime? actionDate = null)
{
    var entity = await _db.Orders.FindAsync(id);
    if (entity is null) return;

    entity.Status = newStatus;

    // アクション日時の記録（ステータスに応じて適切なカラムに保存）
    if (actionDate.HasValue)
    {
        entity.ShippedAt = newStatus == OrderStatus.Shipped ? actionDate : entity.ShippedAt;
        entity.InvoicedAt = newStatus == OrderStatus.Invoiced ? actionDate : entity.InvoicedAt;
    }

    await _db.SaveChangesAsync();
}
```

---

### 5. Web 層 — MVC Controller（アクション URL）

ステータス遷移はアクション単位の POST URL で表現します。

```csharp
// MyTodo.Web/Controllers/OrdersController.cs
[Route("mvc/orders")]
public class OrdersController : Controller
{
    private readonly IOrderQueryService _queryService;
    private readonly ApproveOrderCommandHandler _approveHandler;
    private readonly ShipOrderCommandHandler _shipHandler;
    // ... その他コンストラクタインジェクション

    // GET /mvc/orders/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _queryService.GetByIdAsync(id);
        if (order is null) return NotFound();
        return View(order);
    }

    // POST /mvc/orders/{id}/approve
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            await _approveHandler.HandleAsync(new ApproveOrderCommand(id));
            TempData["Success"] = "受注を承認しました";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;  // 不正な遷移エラーを画面に表示
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST /mvc/orders/{id}/ship
    [HttpPost("{id:int}/ship")]
    public async Task<IActionResult> Ship(int id)
    {
        try
        {
            await _shipHandler.HandleAsync(new ShipOrderCommand(id, DateTime.UtcNow));
            TempData["Success"] = "出荷済みにしました";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}
```

### アクション URL の規則

| アクション | HTTP | パス |
|---|---|---|
| 承認申請 | POST | `/mvc/orders/{id}/submit` |
| 承認 | POST | `/mvc/orders/{id}/approve` |
| 却下 | POST | `/mvc/orders/{id}/reject` |
| 出荷 | POST | `/mvc/orders/{id}/ship` |
| 請求 | POST | `/mvc/orders/{id}/invoice` |
| キャンセル | POST | `/mvc/orders/{id}/cancel` |

---

### 6. View — ステータス別ボタン制御

View 側では、現在のステータスに応じてアクションボタンを出し分けます。

```cshtml
@* Views/Orders/Details.cshtml *@
@model OrderReadModel

<h1>受注詳細 @Model.OrderNumber</h1>
<p>ステータス: <strong>@Model.StatusLabel</strong></p>

@* ステータス別アクションボタン *@
<div class="cluster">
    @if (Model.Status == "Draft")
    {
        <form asp-action="Submit" asp-route-id="@Model.Id" method="post">
            <button class="btn btn--primary" type="submit">承認申請</button>
        </form>
    }
    @if (Model.Status == "PendingApproval")
    {
        <form asp-action="Approve" asp-route-id="@Model.Id" method="post">
            <button class="btn btn--success" type="submit">承認</button>
        </form>
        <form asp-action="Reject" asp-route-id="@Model.Id" method="post">
            <button class="btn btn--danger" type="submit">却下</button>
        </form>
    }
    @if (Model.Status == "Approved")
    {
        <form asp-action="Ship" asp-route-id="@Model.Id" method="post">
            <button class="btn btn--primary" type="submit">出荷</button>
        </form>
    }
</div>
```

---

### 7. ReadModel へのステータスラベル付与

```csharp
// MyTodo.Application/Queries/Orders/OrderReadModel.cs
public record OrderReadModel(
    int Id,
    string OrderNumber,
    string Status,        // Enum の文字列表現
    string StatusLabel,   // 日本語ラベル
    bool CanApprove,      // 画面制御用フラグ
    bool CanShip,
    bool CanInvoice,
    // ... その他フィールド
    IReadOnlyList<OrderLineReadModel> Lines);
```

QueryService 側でステータスに合わせたフラグを計算して返します。

```csharp
// MyTodo.Infrastructure/Queries/OrderQueryService.cs（抜粋）
private static string GetStatusLabel(OrderStatus status) => status switch
{
    OrderStatus.Draft           => "下書き",
    OrderStatus.PendingApproval => "承認待ち",
    OrderStatus.Approved        => "承認済み",
    OrderStatus.Shipped         => "出荷済み",
    OrderStatus.Invoiced        => "請求済み",
    OrderStatus.Rejected        => "却下",
    OrderStatus.Cancelled       => "キャンセル",
    _ => status.ToString()
};
```

---

## 設計上の注意点

| 注意点 | 理由 |
|---|---|
| 遷移ルールは Domain 層に集約する | Controller・Handler に業務ルールが散らばるのを防ぐ |
| コマンドは汎用 `ChangeStatusCommand` にしない | アクション単位のコマンドにすることで意図が明確になる |
| 不正遷移例外は画面に表示する | ユーザーによる操作ミスに対してフィードバックを返す |
| 終端ステータスは遷移テーブルに空リストで明示する | コードを読むだけで終端が分かる |
