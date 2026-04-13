# 06. 一時テーブル・テーブル変数・CTE

## 目次

1. [ローカル一時テーブル（#temp）](#1-ローカル一時テーブルtemp)
2. [グローバル一時テーブル（##temp）](#2-グローバル一時テーブルtemp)
3. [テーブル変数（@table）](#3-テーブル変数table)
4. [一時テーブル vs テーブル変数 比較](#4-一時テーブル-vs-テーブル変数-比較)
5. [CTE（共通テーブル式）](#5-cte共通テーブル式)
6. [再帰 CTE](#6-再帰-cte)
7. [実践パターン：集計中間データの活用](#7-実践パターン集計中間データの活用)

---

## 1. ローカル一時テーブル（#temp）

プロシージャ（またはセッション）内で一時的なテーブルを作成します。  
プロシージャが終了すると自動的に削除されます。

```sql
CREATE OR ALTER PROCEDURE dbo.usp_GetTodoSummary
AS
BEGIN
    SET NOCOUNT ON;

    -- 一時テーブル作成
    CREATE TABLE #TodoSummary
    (
        CategoryId   INT           NOT NULL,
        CategoryName NVARCHAR(100) NOT NULL,
        TotalCount   INT           NOT NULL DEFAULT 0,
        DoneCount    INT           NOT NULL DEFAULT 0
    );

    -- データ投入
    INSERT INTO #TodoSummary (CategoryId, CategoryName, TotalCount, DoneCount)
    SELECT
        c.Id,
        c.Name,
        COUNT(t.Id),
        SUM(CASE WHEN t.Done = 1 THEN 1 ELSE 0 END)
    FROM       dbo.Categories c
    LEFT JOIN  dbo.Todos       t ON t.CategoryId = c.Id
    GROUP BY   c.Id, c.Name;

    -- 結果を返す
    SELECT
        CategoryName,
        TotalCount,
        DoneCount,
        TotalCount - DoneCount AS PendingCount,
        CAST(DoneCount * 100.0 / NULLIF(TotalCount, 0) AS DECIMAL(5,1)) AS DoneRate
    FROM   #TodoSummary
    ORDER BY TotalCount DESC;

    -- 明示的に削除（省略可。プロシージャ終了時に自動削除）
    DROP TABLE IF EXISTS #TodoSummary;
END
GO
```

### 一時テーブルへのインデックス追加

```sql
CREATE TABLE #LargeWork
(
    Id    INT NOT NULL,
    Value NVARCHAR(100)
);

-- 主キー（クラスター化インデックス）
ALTER TABLE #LargeWork ADD PRIMARY KEY (Id);

-- 非クラスター化インデックス
CREATE INDEX IX_LargeWork_Value ON #LargeWork (Value);
```

---

## 2. グローバル一時テーブル（##temp）

セッションをまたいで参照できる一時テーブル。すべてのセッションが切断するまで存在します。  
**マルチユーザー環境での利用は競合が起きるため避けること。**

```sql
-- 特殊な用途（デバッグや一時共有）のみ使用
CREATE TABLE ##GlobalTemp (Id INT, Message NVARCHAR(200));
```

---

## 3. テーブル変数（@table）

変数として使えるテーブル。小規模データ向け。

```sql
CREATE OR ALTER PROCEDURE dbo.usp_GetTopTodos
    @TopN INT = 5
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Results TABLE
    (
        Rank     INT           NOT NULL,
        Id       INT           NOT NULL,
        Title    NVARCHAR(200) NOT NULL
    );

    INSERT INTO @Results (Rank, Id, Title)
    SELECT TOP (@TopN)
           ROW_NUMBER() OVER (ORDER BY CreatedAt DESC) AS Rank,
           Id,
           Title
    FROM   dbo.Todos
    WHERE  Done = 0
    ORDER BY CreatedAt DESC;

    SELECT * FROM @Results;
END
GO
```

---

## 4. 一時テーブル vs テーブル変数 比較

| 特徴 | 一時テーブル `#temp` | テーブル変数 `@table` |
|---|---|---|
| 格納場所 | `tempdb` | `tempdb`（または メモリ） |
| インデックス | 作成可能 | 主キーのみ（作成時のみ） |
| 統計情報 | あり | なし（クエリ最適化が弱い） |
| トランザクションログ | あり | 少ない |
| スコープ | セッション / プロシージャ | 現在のバッチ / プロシージャ |
| ROLLBACK の影響 | 受ける | 受けない |
| 推奨行数 | 100 行以上 | 100 行未満 |

**判断基準**:
- 少量データ（数十行以内）→ テーブル変数
- 中〜大量データ、インデックスが必要 → 一時テーブル

---

## 5. CTE（共通テーブル式）

`WITH` 句で名前付きの一時的な結果セットを定義します。  
ビューやサブクエリよりも可読性が高い。

```sql
-- 基本構文
WITH CTEName AS
(
    SELECT ...
)
SELECT * FROM CTEName;
```

### 実用例：完了率付き Todo 一覧

```sql
WITH CategoryStats AS
(
    SELECT
        CategoryId,
        COUNT(*) AS TotalCount,
        SUM(CASE WHEN Done = 1 THEN 1 ELSE 0 END) AS DoneCount
    FROM  dbo.Todos
    GROUP BY CategoryId
),
RankedTodos AS
(
    SELECT
        t.*,
        ROW_NUMBER() OVER (PARTITION BY t.CategoryId ORDER BY t.CreatedAt DESC) AS RowNum
    FROM dbo.Todos t
)
SELECT
    rt.Id,
    rt.Title,
    rt.Done,
    rt.CreatedAt,
    cs.TotalCount,
    cs.DoneCount,
    CAST(cs.DoneCount * 100.0 / NULLIF(cs.TotalCount, 0) AS DECIMAL(5,1)) AS DoneRate
FROM       RankedTodos  rt
INNER JOIN CategoryStats cs ON cs.CategoryId = rt.CategoryId
WHERE rt.RowNum <= 3;  -- カテゴリごとに最新3件
```

---

## 6. 再帰 CTE

自己参照テーブル（ツリー構造・階層データ）を展開するのに使います。

```sql
-- 例: カテゴリ階層（自己参照テーブル）
-- Categories テーブルに ParentId カラムがある前提

WITH CategoryHierarchy AS
(
    -- アンカークエリ（再帰の起点: ルートカテゴリ）
    SELECT
        Id,
        Name,
        ParentId,
        0        AS Level,
        CAST(Name AS NVARCHAR(1000)) AS Path
    FROM  dbo.Categories
    WHERE ParentId IS NULL

    UNION ALL

    -- 再帰クエリ（子カテゴリを繰り返し結合）
    SELECT
        c.Id,
        c.Name,
        c.ParentId,
        ch.Level + 1,
        CAST(ch.Path + N' > ' + c.Name AS NVARCHAR(1000))
    FROM       dbo.Categories    c
    INNER JOIN CategoryHierarchy ch ON ch.Id = c.ParentId
)
SELECT
    REPLICATE(N'　', Level) + Name AS IndentedName,
    Level,
    Path
FROM  CategoryHierarchy
ORDER BY Path;
```

> **再帰の深さ制限**: デフォルトは 100 回。`OPTION (MAXRECURSION n)` で変更可能（0 = 無制限、注意して使用）。

```sql
-- 最大再帰数の指定
... 
OPTION (MAXRECURSION 200);
```

---

## 7. 実践パターン：集計中間データの活用

複雑な集計処理を CTE + 一時テーブルで分割して記述する例。

```sql
CREATE OR ALTER PROCEDURE dbo.usp_GetMonthlyReport
    @Year  INT = NULL,
    @Month INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- パラメータのデフォルト値（今月）
    SET @Year  = ISNULL(@Year,  YEAR(GETDATE()));
    SET @Month = ISNULL(@Month, MONTH(GETDATE()));

    DECLARE @StartDate DATE = DATEFROMPARTS(@Year, @Month, 1);
    DECLARE @EndDate   DATE = EOMONTH(@StartDate);

    -- Step 1: 対象月のデータを一時テーブルに絞り込む
    SELECT Id, Title, Done, CreatedAt
    INTO   #MonthTodos
    FROM   dbo.Todos
    WHERE  CreatedAt BETWEEN @StartDate AND DATEADD(DAY, 1, @EndDate);

    CREATE CLUSTERED INDEX IX_MonthTodos_CreatedAt ON #MonthTodos (CreatedAt);

    -- Step 2: CTE で集計
    WITH DailySummary AS
    (
        SELECT
            CAST(CreatedAt AS DATE) AS Day,
            COUNT(*)                AS AddedCount,
            SUM(CASE WHEN Done = 1 THEN 1 ELSE 0 END) AS DoneCount
        FROM  #MonthTodos
        GROUP BY CAST(CreatedAt AS DATE)
    )
    SELECT
        Day,
        AddedCount,
        DoneCount,
        AddedCount - DoneCount AS PendingCount
    FROM  DailySummary
    ORDER BY Day;

    DROP TABLE IF EXISTS #MonthTodos;
END
GO
```
