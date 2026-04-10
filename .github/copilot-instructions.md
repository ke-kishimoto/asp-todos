# GitHub Copilot Instructions

このドキュメントは、GitHub Copilot が本リポジトリのコードを生成・補完する際に従うべきルールと設計方針を定義します。

---

## プロジェクト概要

**MyTodo** は .NET 10 を使った Todo 管理アプリケーションです。  
クリーンアーキテクチャ + CQRS パターンを採用し、MVC / Blazor Server / Web API の 3 つのアプローチを同一ソリューション内に実装しています。

```
dotnetSample.sln
├── MyTodo.Domain/          ドメイン層（ビジネスルール・エンティティ・値オブジェクト）
├── MyTodo.Infrastructure/  インフラ層（EF Core・SQL Server・リポジトリ実装）
├── MyTodo.Application/     アプリケーション層（コマンドハンドラー・クエリサービス）
├── MyTodo.Web/             プレゼンテーション層（MVC / Blazor Server / Web API）
├── TodoApp.Tests/          単体テスト（xUnit）
└── MyTodo.E2E/             E2E テスト（gauge + Playwright）
```

---

## アーキテクチャ方針

### UnitOfWork

複数のリポジトリ操作（追加・更新・削除の混在）を **1 つのトランザクション** にまとめる場合は `IUnitOfWork` を使用します。

- `IUnitOfWork` インターフェース: `MyTodo.Application/Repositories/` に定義
- `EfUnitOfWork` 実装: `MyTodo.Infrastructure/Repositories/` に配置
- **UnitOfWork 対応リポジトリは `SaveChangesAsync()` を呼ばない**（変更を EF ChangeTracker に積むだけ）
- CommandHandler が `BeginTransactionAsync()` → 操作 → `CommitAsync()` / `RollbackAsync()` でトランザクション境界を制御
- 通常の 1 操作ずつ保存するリポジトリ（`EfTodoRepository` 等）は従来どおり自前で `SaveChangesAsync()` を呼ぶ

詳細は [.github/skills/SKILL-ADVANCED-PATTERNS.md](.github/skills/SKILL-ADVANCED-PATTERNS.md) を参照。

---

### クリーンアーキテクチャ

依存の方向は常に **外側 → 内側** です。Domain 層は他の層を参照しません。

```
MyTodo.Web
    ↓ 依存
MyTodo.Application
    ↓ 依存
MyTodo.Domain  ←  MyTodo.Infrastructure（Interfaceを通じて逆依存）
```

- **Domain 層**: 他プロジェクトへの参照を一切持たない
- **Application 層**: Domain のインターフェースのみに依存
- **Infrastructure 層**: Application 層で定義されたインターフェースを実装
- **Web 層**: Application 層のハンドラー・クエリサービスを DI 経由で使用

### CQRS

書き込み操作（Command）と読み取り操作（Query）は完全に分離します。

- **Command**: `MyTodo.Application/Commands/` 配下にハンドラーを実装
- **Query**: `MyTodo.Application/Queries/` にインターフェース、`MyTodo.Infrastructure/Queries/` に実装
- QueryService は ReadModel（DTO）を返し、ドメインオブジェクトは返さない

---

## 各層の実装規約

詳細なコーディングパターンは以下の SKILL ファイルを参照してください。

- [.github/skills/SKILL.md](.github/skills/SKILL.md) — 各層の基本実装パターン
- [.github/skills/SKILL-ADVANCED-PATTERNS.md](.github/skills/SKILL-ADVANCED-PATTERNS.md) — インライン編集テーブルによる一画面 CRUD / UnitOfWork パターン

---

## フロントエンド方針

| シナリオ | 採用技術 |
|---|---|
| 画面遷移・通常の CRUD 操作 | **MVC** (Controller + Razor View) |
| 動的な行追加・インタラクティブな操作 | **Blazor Server** |
| 一覧表内で複数行を一括 CRUD（インライン編集） | **Blazor Server**（インライン編集テーブルパターン） |
| 外部クライアント向けデータ提供 | **Web API** (JSON) |

### URL 設計

| 技術 | URL パターン |
|---|---|
| MVC | `/mvc/todos/*`, `/mvc/items/*` |
| Blazor | `/blazor/todos`, `/blazor/orders` |
| API | `/api/todos/*` |

---

## CSS 設計方針（ITCSS + Every Layout）

`wwwroot/css/` は ITCSS のレイヤー構造で管理します。

```
01-settings.css   → カスタムプロパティ（CSS変数）の定義
02-tools.css      → ミックスイン・ユーティリティ関数相当の共通スタイル
03-generic.css    → リセット・ノーマライズ
04-elements.css   → 素の HTML 要素スタイル（セレクタ指定なし）
05-objects.css    → Every Layout パターン（Stack / Cluster / Center 等）のレイアウト
06-components.css → 再利用可能な UI コンポーネント
07-utilities.css  → 上書き用ユーティリティクラス
```

- スペーシング・色・フォントサイズは必ず `01-settings.css` の CSS 変数（`--s0`, `--color-text` 等）を使用する
- 新しいコンポーネントは `06-components.css` に追記する
- レイアウトパターンは Every Layout の設計思想（Stack, Cluster, Center, Sidebar 等）に準拠する

---

## テスト方針

### 単体テスト（TodoApp.Tests）

- フレームワーク: **xUnit**
- テスト対象: 主に **Domain 層**（ビジネスロジック・値オブジェクトの振る舞い）
- テストクラスは `namespace TodoApp.Test.Domain` 配下に配置
- テストメソッド名は日本語または `[対象]_[条件]_[期待値]` の形式を推奨

### E2E テスト（MyTodo.E2E）

- フレームワーク: **gauge + Playwright**
- spec ファイルは `specs/todos/` 配下に Markdown 形式で記述（日本語）
- ステップ実装は `steps/` 配下の C# クラスに `[Step("...")]` 属性で定義
- テーブルデータの検証は CSV ファイル（`fixtures/`）またはインライン Markdown テーブルで行う
- 実行コマンド: `gauge run specs/todos/<spec-file>`

---

## DI / サービス登録規約

- Infrastructure 層のサービスは `AddInfrastructure()` 拡張メソッドにまとめる
- Application 層のサービスは `AddApplication()` 拡張メソッドにまとめる
- `Program.cs` では拡張メソッドを呼ぶだけにし、個別の `AddScoped` 等は書かない

---

## 認証

- 開発環境: `appsettings.Development.json` の `"UseFakeAuth": true` で `FakeAuthHandler` が有効化される
- 本番環境: Microsoft Entra ID（Azure AD）による OpenID Connect 認証

---

## 禁止事項

- Domain 層から Application / Infrastructure / Web 層への参照追加
- Controller・Blazor コンポーネントからのリポジトリ直接参照（必ずコマンドハンドラー・クエリサービス経由）
- `Program.cs` での個別サービス登録（拡張メソッドにまとめること）
- CSS での数値のハードコーディング（CSS 変数を使用すること）
