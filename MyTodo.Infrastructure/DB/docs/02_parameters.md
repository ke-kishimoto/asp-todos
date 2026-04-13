# 02. パラメータ

## 目次

1. [入力パラメータ（INPUT）](#1-入力パラメータinput)
2. [出力パラメータ（OUTPUT）](#2-出力パラメータoutput)
3. [デフォルト値（省略可能パラメータ）](#3-デフォルト値省略可能パラメータ)
4. [テーブル値パラメータ（TVP）](#4-テーブル値パラメータtvp)
5. [パラメータのデータ型一覧](#5-パラメータのデータ型一覧)
6. [NULL の扱い](#6-null-の扱い)

---

## 1. 入力パラメータ（INPUT）

最も基本的なパラメータ。呼び出し元から値を受け取る。

```sql
CREATE OR ALTER PROCEDURE dbo.usp_GetTodoById
    @TodoId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, Done, CreatedAt
    FROM   dbo.Todos
    WHERE  Id = @TodoId;
END
GO

-- 実行
EXEC dbo.usp_GetTodoById @TodoId = 1;
```

---

## 2. 出力パラメータ（OUTPUT）

プロシージャから呼び出し元へ値を返す。`RETURN` が整数専用なのに対し、任意の型を返せる。

```sql
CREATE OR ALTER PROCEDURE dbo.usp_InsertTodo
    @Title     NVARCHAR(200),
    @NewId     INT OUTPUT       -- ← OUTPUT キーワード
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Todos (Title, Done, CreatedAt)
    VALUES (@Title, 0, GETDATE());

    SET @NewId = SCOPE_IDENTITY();  -- 挿入した行の ID を返す
END
GO

-- 実行
DECLARE @InsertedId INT;

EXEC dbo.usp_InsertTodo
    @Title  = 'ドキュメント作成',
    @NewId  = @InsertedId OUTPUT;   -- ← OUTPUT を忘れずに

PRINT @InsertedId;  -- 挿入された ID が出力される
```

### SCOPE_IDENTITY() vs @@IDENTITY vs IDENT_CURRENT()

| 関数 | スコープ | トリガー考慮 | 推奨 |
|---|---|---|---|
| `SCOPE_IDENTITY()` | 現在のスコープのみ | 考慮する | ✅ 推奨 |
| `@@IDENTITY` | セッション全体 | 考慮しない | 非推奨 |
| `IDENT_CURRENT('テーブル名')` | テーブル単位（全セッション）| 考慮しない | 特殊用途 |

---

## 3. デフォルト値（省略可能パラメータ）

`= デフォルト値` と書くことでパラメータを省略可能にできる。

```sql
CREATE OR ALTER PROCEDURE dbo.usp_GetTodos
    @Done      BIT          = NULL,   -- NULL = 絞り込みなし
    @MaxRows   INT          = 100,
    @SortOrder NVARCHAR(4)  = 'ASC'
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@MaxRows)
           Id, Title, Done, CreatedAt
    FROM   dbo.Todos
    WHERE  (@Done IS NULL OR Done = @Done)
    ORDER BY
        CASE WHEN @SortOrder = 'ASC'  THEN CreatedAt END ASC,
        CASE WHEN @SortOrder = 'DESC' THEN CreatedAt END DESC;
END
GO

-- パラメータをすべて省略
EXEC dbo.usp_GetTodos;

-- 一部だけ指定（名前指定必須）
EXEC dbo.usp_GetTodos @Done = 0, @MaxRows = 50;
```

> **注意**: 省略可能パラメータを持つプロシージャを位置指定で呼ぶ場合、途中を省略できない。名前指定を使うこと。

---

## 4. テーブル値パラメータ（TVP）

複数行のデータをまとめてプロシージャに渡せる。  
アプリケーション側から一括 INSERT する際に特に有効。

### Step 1: ユーザー定義テーブル型を作成

```sql
CREATE TYPE dbo.TodoBulkInsertType AS TABLE
(
    Title     NVARCHAR(200) NOT NULL,
    Done      BIT           NOT NULL DEFAULT 0,
    CreatedAt DATETIME2     NOT NULL DEFAULT GETDATE()
);
GO
```

### Step 2: TVP を使うプロシージャを作成

```sql
CREATE OR ALTER PROCEDURE dbo.usp_BulkInsertTodos
    @Todos dbo.TodoBulkInsertType READONLY  -- READONLY は必須
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Todos (Title, Done, CreatedAt)
    SELECT Title, Done, CreatedAt
    FROM   @Todos;

    SELECT @@ROWCOUNT AS InsertedCount;
END
GO
```

### Step 3: 実行（T-SQL から）

```sql
DECLARE @NewTodos dbo.TodoBulkInsertType;

INSERT INTO @NewTodos (Title, Done)
VALUES ('タスク A', 0),
       ('タスク B', 0),
       ('タスク C', 1);

EXEC dbo.usp_BulkInsertTodos @Todos = @NewTodos;
```

### Step 4: C#（EF Core / ADO.NET）から呼び出す例

```csharp
using var dt = new DataTable();
dt.Columns.Add("Title",     typeof(string));
dt.Columns.Add("Done",      typeof(bool));
dt.Columns.Add("CreatedAt", typeof(DateTime));

dt.Rows.Add("タスク A", false, DateTime.UtcNow);
dt.Rows.Add("タスク B", false, DateTime.UtcNow);

using var cmd = new SqlCommand("dbo.usp_BulkInsertTodos", connection)
{
    CommandType = CommandType.StoredProcedure
};

var param = cmd.Parameters.AddWithValue("@Todos", dt);
param.SqlDbType = SqlDbType.Structured;
param.TypeName  = "dbo.TodoBulkInsertType";

await cmd.ExecuteNonQueryAsync();
```

---

## 5. パラメータのデータ型一覧

よく使うデータ型のまとめ。

| カテゴリ | 型名 | 説明 | 例 |
|---|---|---|---|
| 整数 | `INT` | 4 バイト整数（-21 億〜21 億） | `@Id INT` |
| 整数 | `BIGINT` | 8 バイト整数 | `@RowCount BIGINT` |
| 整数 | `SMALLINT` | 2 バイト（-32768〜32767） | `@Status SMALLINT` |
| 整数 | `TINYINT` | 1 バイト（0〜255） | `@Priority TINYINT` |
| 文字列 | `NVARCHAR(n)` | Unicode 可変長（日本語推奨） | `@Title NVARCHAR(200)` |
| 文字列 | `NVARCHAR(MAX)` | 最大 2GB Unicode 文字列 | `@Body NVARCHAR(MAX)` |
| 文字列 | `VARCHAR(n)` | 非 Unicode 可変長 | `@Code VARCHAR(20)` |
| 日時 | `DATE` | 日付のみ（2000-01-01） | `@StartDate DATE` |
| 日時 | `DATETIME2` | 高精度日時（推奨） | `@CreatedAt DATETIME2` |
| 日時 | `DATETIMEOFFSET` | タイムゾーン付き日時 | `@EventAt DATETIMEOFFSET` |
| 真偽 | `BIT` | 0 / 1 / NULL | `@Done BIT` |
| 数値 | `DECIMAL(p,s)` | 固定小数点 | `@Amount DECIMAL(18,2)` |
| 数値 | `FLOAT` | 浮動小数点（精度注意） | `@Ratio FLOAT` |
| GUID | `UNIQUEIDENTIFIER` | UUID / GUID | `@UserId UNIQUEIDENTIFIER` |
| XML | `XML` | XML データ | `@XmlData XML` |

---

## 6. NULL の扱い

```sql
CREATE OR ALTER PROCEDURE dbo.usp_UpdateTodoTitle
    @TodoId INT,
    @Title  NVARCHAR(200) = NULL   -- NULL なら更新しない
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Todos
    SET    Title = ISNULL(@Title, Title)   -- NULL なら元の値を維持
    WHERE  Id = @TodoId;
END
GO
```

### NULL 関連の便利関数

| 関数 | 説明 | 例 |
|---|---|---|
| `ISNULL(a, b)` | `a` が NULL なら `b` を返す | `ISNULL(@Title, Title)` |
| `COALESCE(a, b, ...)` | 左から最初の非 NULL を返す（ANSI標準）| `COALESCE(@Title, Title, N'(未入力)')` |
| `NULLIF(a, b)` | `a = b` なら NULL を返す | `NULLIF(@Status, 0)` |
