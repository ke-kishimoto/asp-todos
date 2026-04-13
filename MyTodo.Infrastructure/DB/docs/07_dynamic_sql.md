# 07. 動的 SQL

## 目次

1. [動的 SQL とは](#1-動的-sql-とは)
2. [EXEC（文字列実行）](#2-exec文字列実行)
3. [sp_executesql（推奨）](#3-sp_executesql推奨)
4. [SQLインジェクション対策](#4-sqlインジェクション対策)
5. [動的 ORDER BY](#5-動的-order-by)
6. [動的テーブル名・列名](#6-動的テーブル名列名)
7. [動的 WHERE 句の構築](#7-動的-where-句の構築)
8. [実践パターン：柔軟な検索プロシージャ](#8-実践パターン柔軟な検索プロシージャ)

---

## 1. 動的 SQL とは

実行時に SQL 文字列を組み立てて実行する手法。  
以下のような「静的 SQL では実現できない」要件で使います：

- WHERE 条件の有無が実行時に決まる
- ORDER BY のカラム名がパラメータで渡される
- テーブル名・スキーマ名が動的に変わる

**注意**: 動的 SQL は **SQL インジェクション最大のリスク箇所**。必ずパラメータ化またはホワイトリスト検証を行うこと。

---

## 2. EXEC（文字列実行）

最もシンプルだが、パラメータ化できないため非推奨。

```sql
DECLARE @Sql NVARCHAR(MAX);
DECLARE @TableName NVARCHAR(128) = 'Todos';

SET @Sql = N'SELECT * FROM dbo.' + QUOTENAME(@TableName);
EXEC (@Sql);
```

`QUOTENAME()` は識別子を `[...]` で囲む。テーブル名・列名のインジェクションを防ぐのに必須。

---

## 3. sp_executesql（推奨）

パラメータ化が可能で、実行プランのキャッシュが効く。**動的 SQL のベストプラクティス**。

```sql
DECLARE @Sql    NVARCHAR(MAX);
DECLARE @Params NVARCHAR(MAX);
DECLARE @SearchTitle NVARCHAR(200) = N'タスク';
DECLARE @Done        BIT           = 0;

SET @Sql = N'
    SELECT Id, Title, Done, CreatedAt
    FROM   dbo.Todos
    WHERE  Title LIKE @Title + N''%''
      AND  Done  = @Done
    ORDER BY CreatedAt DESC;
';

SET @Params = N'@Title NVARCHAR(200), @Done BIT';

EXEC sp_executesql
    @Sql,
    @Params,
    @Title = @SearchTitle,
    @Done  = @Done;
```

### OUTPUT パラメータを取得する

```sql
DECLARE @Sql       NVARCHAR(MAX);
DECLARE @Params    NVARCHAR(MAX);
DECLARE @RowCount  INT;

SET @Sql    = N'SELECT @RowCount = COUNT(*) FROM dbo.Todos WHERE Done = @Done;';
SET @Params = N'@Done BIT, @RowCount INT OUTPUT';

EXEC sp_executesql
    @Sql,
    @Params,
    @Done     = 0,
    @RowCount = @RowCount OUTPUT;

PRINT @RowCount;
```

---

## 4. SQLインジェクション対策

### 危険な例（絶対に書かないこと）

```sql
-- ❌ 危険: ユーザー入力を直接連結
DECLARE @UserInput NVARCHAR(200) = N"'; DROP TABLE dbo.Todos; --";
DECLARE @Sql NVARCHAR(MAX) = N'SELECT * FROM dbo.Todos WHERE Title = ''' + @UserInput + N'''';
EXEC (@Sql);
-- → テーブルが削除される可能性がある
```

### 安全な書き方

| 対象 | 対策 |
|---|---|
| 値（文字列・数値） | `sp_executesql` でパラメータ化 |
| 識別子（テーブル名・列名） | `QUOTENAME()` でエスケープ |
| 識別子の動的指定 | ホワイトリスト検証（後述） |

### ホワイトリスト検証の例

```sql
CREATE OR ALTER PROCEDURE dbo.usp_GetTodosSorted
    @SortColumn NVARCHAR(50) = 'CreatedAt',
    @SortOrder  NVARCHAR(4)  = 'DESC'
AS
BEGIN
    SET NOCOUNT ON;

    -- ✅ ホワイトリスト検証（列名のインジェクション対策）
    IF @SortColumn NOT IN ('Id', 'Title', 'CreatedAt')
    BEGIN
        THROW 50001, '無効なソート列です。', 1;
    END

    IF @SortOrder NOT IN ('ASC', 'DESC')
    BEGIN
        THROW 50002, '無効なソート順です。', 1;
    END

    DECLARE @Sql NVARCHAR(MAX);
    SET @Sql = N'
        SELECT Id, Title, Done, CreatedAt
        FROM   dbo.Todos
        ORDER BY ' + QUOTENAME(@SortColumn) + N' ' + @SortOrder;

    EXEC sp_executesql @Sql;
END
GO
```

---

## 5. 動的 ORDER BY

動的 SQL を使わずに ORDER BY を動的にする方法（推奨）:

```sql
-- CASE 式を使って動的ソート（動的 SQL 不要）
SELECT Id, Title, CreatedAt
FROM   dbo.Todos
ORDER BY
    CASE WHEN @SortColumn = 'Title'     AND @SortOrder = 'ASC'  THEN Title     END ASC,
    CASE WHEN @SortColumn = 'Title'     AND @SortOrder = 'DESC' THEN Title     END DESC,
    CASE WHEN @SortColumn = 'CreatedAt' AND @SortOrder = 'ASC'  THEN CreatedAt END ASC,
    CASE WHEN @SortColumn = 'CreatedAt' AND @SortOrder = 'DESC' THEN CreatedAt END DESC;
```

---

## 6. 動的テーブル名・列名

```sql
CREATE OR ALTER PROCEDURE dbo.usp_GetTableRowCount
    @SchemaName NVARCHAR(128) = 'dbo',
    @TableName  NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;

    -- ホワイトリスト検証（sys.tables で実在確認）
    IF NOT EXISTS
    (
        SELECT 1
        FROM   sys.tables t
        INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE  t.name   = @TableName
          AND  s.name   = @SchemaName
    )
    BEGIN
        THROW 50404, '指定されたテーブルが見つかりません。', 1;
    END

    DECLARE @Sql NVARCHAR(MAX);
    SET @Sql = N'SELECT COUNT(*) AS RowCount FROM '
               + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName);

    EXEC sp_executesql @Sql;
END
GO
```

---

## 7. 動的 WHERE 句の構築

条件が可変のフィルタリングプロシージャのパターン。

```sql
CREATE OR ALTER PROCEDURE dbo.usp_SearchTodos
    @Title     NVARCHAR(200) = NULL,
    @Done      BIT           = NULL,
    @StartDate DATE          = NULL,
    @EndDate   DATE          = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Sql    NVARCHAR(MAX);
    DECLARE @Params NVARCHAR(MAX);

    SET @Sql = N'
        SELECT Id, Title, Done, CreatedAt
        FROM   dbo.Todos
        WHERE  1 = 1
    ';

    -- 条件を動的に追加
    IF @Title IS NOT NULL
        SET @Sql = @Sql + N' AND Title LIKE @Title + N''%''';

    IF @Done IS NOT NULL
        SET @Sql = @Sql + N' AND Done = @Done';

    IF @StartDate IS NOT NULL
        SET @Sql = @Sql + N' AND CAST(CreatedAt AS DATE) >= @StartDate';

    IF @EndDate IS NOT NULL
        SET @Sql = @Sql + N' AND CAST(CreatedAt AS DATE) <= @EndDate';

    SET @Sql = @Sql + N' ORDER BY CreatedAt DESC;';

    SET @Params = N'
        @Title     NVARCHAR(200),
        @Done      BIT,
        @StartDate DATE,
        @EndDate   DATE
    ';

    EXEC sp_executesql
        @Sql,
        @Params,
        @Title     = @Title,
        @Done      = @Done,
        @StartDate = @StartDate,
        @EndDate   = @EndDate;
END
GO
```

---

## 8. 実践パターン：柔軟な検索プロシージャ

> 動的 SQL を使わずに実装する代替案（静的 SQL でパフォーマンスが良い場合）:

```sql
-- 動的 SQL なしの柔軟な WHERE（"catch-all query" パターン）
SELECT Id, Title, Done, CreatedAt
FROM   dbo.Todos
WHERE  (@Title IS NULL OR Title LIKE @Title + '%')
  AND  (@Done  IS NULL OR Done  = @Done)
  AND  (@StartDate IS NULL OR CAST(CreatedAt AS DATE) >= @StartDate)
  AND  (@EndDate   IS NULL OR CAST(CreatedAt AS DATE) <= @EndDate)
OPTION (RECOMPILE);  -- ← 毎回最適なプランを作成（パラメータスニッフィング対策）
```

`OPTION (RECOMPILE)` を付けることでパラメータスニッフィング問題を回避できます（詳細は [10_performance.md](10_performance.md)）。
