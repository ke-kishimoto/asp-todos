# マイグレーション手順

EF Core を使ったスキーマ変更（テーブル追加・カラム変更）の手順をまとめます。

## 前提

| 項目 | 値 |
|---|---|
| マイグレーション定義プロジェクト | `MyTodo.Infrastructure` |
| スタートアッププロジェクト | `MyTodo.Web`（接続文字列・DI 設定を持つ） |
| 命名規則 | スネークケース（`UseSnakeCaseNamingConvention()`） |
| コンテキスト | `AppDbContext` |

コマンドはリポジトリルート（`dotnetSample.sln` があるディレクトリ）で実行します。

---

## 1. テーブルを追加する

### 手順

#### 1-1. モデル（Entity クラス）を追加する

`MyTodo.Infrastructure/Models/` に新しい Entity クラスを作成します。

```csharp
// MyTodo.Infrastructure/Models/SampleEntity.cs
namespace MyTodo.Infrastructure.Models;

public class SampleEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

#### 1-2. AppDbContext に DbSet を追加する

`MyTodo.Infrastructure/Data/AppDbContext.cs` に `DbSet` プロパティを追加します。

```csharp
public DbSet<SampleEntity> Samples { get; set; }
```

必要に応じて `OnModelCreating` にテーブル名・制約を設定します。

```csharp
modelBuilder.Entity<SampleEntity>(entity =>
{
    entity.ToTable("samples");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
});
```

#### 1-3. マイグレーションファイルを生成する

```powershell
dotnet ef migrations add <マイグレーション名> `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web
```

**命名例**: `AddSampleTable`、`AddOrdersTable`  
生成先: `MyTodo.Infrastructure/Migrations/`

#### 1-4. 生成内容を確認する

`Migrations/<タイムスタンプ>_<マイグレーション名>.cs` を開き、`Up()` メソッドに意図した `CreateTable` が含まれているか確認します。

#### 1-5. データベースに適用する

```powershell
dotnet ef database update `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web
```

---

## 2. カラムを追加・変更・削除する

### 2-1. カラムを追加する

#### Entity クラスにプロパティを追加する

```csharp
// 例: TodoItemEntity に Description カラムを追加
public string? Description { get; set; }
```

`OnModelCreating` に設定が必要な場合は追記します。

```csharp
entity.Property(e => e.Description).HasMaxLength(500);
```

#### マイグレーションを生成・適用する

```powershell
dotnet ef migrations add AddDescriptionToTodos `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web

dotnet ef database update `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web
```

---

### 2-2. カラムのデータ型・長さを変更する

Entity クラスのプロパティ型、または `OnModelCreating` の Fluent API 設定を変更し、マイグレーションを生成します。

```csharp
// 変更前
entity.Property(e => e.Title).HasMaxLength(200);

// 変更後
entity.Property(e => e.Title).HasMaxLength(500);
```

```powershell
dotnet ef migrations add ChangeTitleMaxLength `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web

dotnet ef database update `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web
```

> **注意**: NOT NULL カラムのデータ型変更は既存データの変換が必要な場合があります。  
> 生成された `Up()` メソッドを確認し、`migrationBuilder.Sql()` で補完データを INSERT する処理が必要かどうか確認してください。

---

### 2-3. カラムを削除する

Entity クラスからプロパティを削除し、`OnModelCreating` の設定も合わせて削除します。

```powershell
dotnet ef migrations add RemoveDescriptionFromTodos `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web

dotnet ef database update `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web
```

> **注意**: 運用 DB でカラムを削除する場合はデータ消失に注意してください。  
> 先にアプリ側でカラムの参照を全て除去してから、別のマイグレーションで削除するのが安全です。

---

## 3. マイグレーションを取り消す

### 直前のマイグレーションを取り消す（DB 適用前）

まだ `database update` を実行していない場合はファイルを削除できます。

```powershell
dotnet ef migrations remove `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web
```

### 特定のマイグレーションまでロールバックする（DB 適用済み）

```powershell
# <ターゲット> には戻したいマイグレーション名を指定（そのマイグレーションの状態まで戻る）
dotnet ef database update <ターゲット> `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web

# ファイルを削除
dotnet ef migrations remove `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web
```

**例**: `InitialCreate` の状態に戻す場合

```powershell
dotnet ef database update InitialCreate `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web
```

---

## 4. 現在の状態を確認する

```powershell
# 適用済み／未適用のマイグレーション一覧を表示
dotnet ef migrations list `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web
```

---

## 5. SQL スクリプトを生成する（本番適用時）

本番環境では `database update` を直接実行せず、SQL スクリプトを生成してレビュー後に適用します。

```powershell
# 全マイグレーションのスクリプトを生成
dotnet ef migrations script `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web `
  --output migrations.sql

# 特定の範囲を指定する場合（<from> から <to> まで）
dotnet ef migrations script <from> <to> `
  --project MyTodo.Infrastructure `
  --startup-project MyTodo.Web `
  --output migrations.sql
```

---

## 6. よくあるエラー

| エラー | 原因 | 対処 |
|---|---|---|
| `No migrations configuration type was found` | `--startup-project` の指定漏れ | `--startup-project MyTodo.Web` を付ける |
| `Unable to create an object of type 'AppDbContext'` | 接続文字列が取得できない | `appsettings.Development.json` の `DefaultConnection` を確認する |
| `There is already an object named '...' in the database` | 既に適用済みのマイグレーションを再適用しようとしている | `migrations list` で状態を確認し、未適用のものだけ適用する |
| `Column ... cannot be null` | NOT NULL カラムを既存データを持つテーブルに追加しようとしている | デフォルト値（`.HasDefaultValue()` / `.HasDefaultValueSql()`）を設定するか、nullable にする |
