# MyTodo アプリケーション

.NET 10 を使ったレイヤードアーキテクチャのサンプルアプリケーションです。  
Todo 管理機能を題材に、**Razor Pages / MVC / Blazor Server / Web API** の 4 つのアプローチを同一ソリューション内で実装・比較できます。

---

## ソリューション構成

```
dotnetSample.sln
├── MyTodo.Domain/          ドメイン層（ビジネスルール・エンティティ）
├── MyTodo.Infrastructure/  インフラ層（DB・EF Core・リポジトリ実装）
├── MyTodo.Application/     アプリケーション層（ユースケース・サービス）
├── MyTodo.Web/             プレゼンテーション層（UI・API・エントリーポイント）
└── TodoApp.Tests/          テストプロジェクト（xUnit）
```

---

## レイヤー構成と依存関係

```
┌─────────────────────────────────────────┐
│          MyTodo.Web (UI / API)          │
│  Razor Pages / MVC / Blazor / REST API  │
└──────────────────┬──────────────────────┘
                   │ 依存
┌──────────────────▼──────────────────────┐
│        MyTodo.Application               │
│  ITodoService / IItemService            │
│  (ビジネスロジック・ユースケース)       │
└──────────┬───────────────────┬──────────┘
           │ 依存              │ 依存
┌──────────▼──────────┐ ┌─────▼───────────────────┐
│   MyTodo.Domain     │ │  MyTodo.Infrastructure   │
│  TodoItem / Item    │ │  EF Core / SQL Server    │
│  (value objects)    │ │  Repository 実装          │
└─────────────────────┘ └──────────────────────────┘
```

> 依存の方向は常に **外側 → 内側** です（Domain が他を参照することはありません）。

---

## 各プロジェクトの役割

### MyTodo.Domain

| 要素 | 内容 |
|------|------|
| `TodoItem.cs` | Todo のドメインモデル（C# `record` で値オブジェクトを表現） |
| `Item.cs` | 商品アイテムのドメインモデル |

- 他プロジェクトへの参照を **一切持たない** 純粋なドメイン層です。
- `TodoId`, `TodoTitle` など、プリミティブ型をラップした値オブジェクトを使い、型安全性を高めています。

---

### MyTodo.Infrastructure

| 要素 | 内容 |
|------|------|
| `Data/AppDbContext.cs` | EF Core の DbContext（`Todos` / `Items` / `Products` テーブル） |
| `Models/` | DB テーブルに対応するエンティティクラス（`TodoItemEntity` など） |
| `Repositories/` | `ITodoRepository` / `IItemRepository` の EF Core 実装 |
| `Migrations/` | EF Core マイグレーションファイル |
| `DB/` | ストアドプロシージャ SQL ファイル |
| `InfrastructureServiceCollectionExtensions.cs` | DI 登録の拡張メソッド |

- **SQL Server** を使用します（接続文字列は `appsettings.json` で設定）。
- リポジトリパターンにより、上位層が SQL / EF Core の詳細を知らなくてよい設計です。

---

### MyTodo.Application

| 要素 | 内容 |
|------|------|
| `Services/ITodoService.cs` | Todo のユースケースインターフェース |
| `Services/TodoService.cs` | ユースケースの実装（タイトルのトリムなどビジネスロジック） |
| `Services/IItemService.cs` | アイテムのユースケースインターフェース |
| `Services/ItemService.cs` | アイテムユースケースの実装 |
| `Extentions/TodoItemExtension.cs` | Domain ↔ Infrastructure エンティティの変換拡張メソッド |
| `ApplicationServiceCollectionExtensions.cs` | DI 登録の拡張メソッド |

- Controller / Page は **インターフェース（`ITodoService`）にのみ依存** します。  
  実装を差し替えてもプレゼンテーション層のコードを変更する必要がありません。

---

### MyTodo.Web

ASP.NET Core のエントリーポイントで、4 種類の UI / API アプローチを含みます。

| パス | アプローチ | URL プレフィックス |
|------|-----------|-------------------|
| `Pages/Todos/` | **Razor Pages** | `/Todos/...` |
| `Controllers/TodosController.cs` | **MVC** (Razor View) | `/mvc/todos/...` |
| `BlazorComponents/Todos/` | **Blazor Server** | `/BlazorTodos` |
| `BlazorComponents/Orders/` | **Blazor Server** | `/BlazorOrders` |
| `Controllers/TodosApiController.cs` | **REST API** | `/api/todos` |

| 要素 | 内容 |
|------|------|
| `Program.cs` | DI 登録・ミドルウェア構成のエントリーポイント |
| `Models/` | ビュー向け ViewModel（`TodoItemViewModel`, `ItemViewModel`） |

#### `Program.cs` で登録されるサービス
```
AddInfrastructure()   → AppDbContext + EfTodoRepository + EfItemRepository
AddApplication()      → TodoService + ItemService
AddControllersWithViews() → MVC
AddRazorPages()       → Razor Pages
AddServerSideBlazor() → Blazor Server
```

---

### TodoApp.Tests

| 要素 | 内容 |
|------|------|
| `TodoItemTest.cs` | `TodoItems.AllCompleted()` のユニットテスト（xUnit） |

---

## 技術スタック

| 技術 | バージョン / 詳細 |
|------|------------------|
| .NET | 10.0 |
| ASP.NET Core | Razor Pages / MVC / Blazor Server |
| Entity Framework Core | 10.0.3（SQL Server プロバイダー） |
| SQL Server | 2022（Docker コンテナ） |
| テスト | xUnit / coverlet |

---

## 開発環境のセットアップ

### 1. SQL Server を起動する（Docker）

```bash
docker compose up -d
```

`docker-compose.yml` で SQL Server 2022 (Developer Edition) が `localhost:1433` で起動します。

### 2. データベースを作成する（マイグレーション）

```bash
cd MyTodo.Web
dotnet ef database update
```

### 3. アプリケーションを起動する

```bash
dotnet run --project MyTodo.Web
```

### 4. テストを実行する

```bash
dotnet test
```

---

## DI ライフタイムのまとめ

| クラス | ライフタイム | 理由 |
|--------|-------------|------|
| `AppDbContext` | Scoped | EF Core 標準。リクエストごとにトランザクションを管理 |
| `EfTodoRepository` | Scoped | DbContext と合わせる必要があるため |
| `TodoService` | Scoped | Repository と合わせる |

---

## 接続文字列

`MyTodo.Web/appsettings.json` に記載されています。

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=MyTodoAppDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
}
```

開発環境固有の設定は `appsettings.Development.json` で上書きできます。
