# 10. パフォーマンス最適化

## 目次

1. [SET NOCOUNT ON](#1-set-nocount-on)
2. [インデックスの活用](#2-インデックスの活用)
3. [実行プランの確認](#3-実行プランの確認)
4. [パラメータスニッフィング](#4-パラメータスニッフィング)
5. [統計情報](#5-統計情報)
6. [クエリヒント](#6-クエリヒント)
7. [一括操作の最適化](#7-一括操作の最適化)
8. [よくあるアンチパターン](#8-よくあるアンチパターンと改善策)

---

## 1. SET NOCOUNT ON

すべてのプロシージャの先頭に必ず書く。

```sql
SET NOCOUNT ON;
```

`n rows affected` メッセージをアプリケーションへ送信しなくなり、ネットワーク往復が減少します。  
EF Coreの `SaveChanges` との組み合わせでも問題ありません。

---

## 2. インデックスの活用

### WHERE 句で使う列にインデックスを作成

```sql
-- CreatedAt フィルタ（usp_Todos_BulkUpdate_By_Date で使用）
CREATE INDEX IX_Todos_CreatedAt ON dbo.Todos (CreatedAt)
    INCLUDE (Done);  -- Done 列もクエリに含まれるなら INCLUDE に追加

-- Done フィルタ（未完了 Todo 取得など）
CREATE INDEX IX_Todos_Done ON dbo.Todos (Done)
    WHERE Done = 0;  -- フィルターインデックス（未完了のみ）
```

### フィルターインデックス（Filtered Index）

特定の条件に絞ったインデックスで、サイズが小さく高速。

```sql
-- 未完了の Todo だけを対象にしたインデックス
CREATE INDEX IX_Todos_Active ON dbo.Todos (CreatedAt)
    INCLUDE (Title)
    WHERE Done = 0;
```

### インデックス使用状況の確認

```sql
-- インデックスの使用統計
SELECT
    o.name          AS TableName,
    i.name          AS IndexName,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates,
    s.last_user_seek
FROM   sys.indexes             i
INNER JOIN sys.objects         o ON o.object_id = i.object_id
LEFT  JOIN sys.dm_db_index_usage_stats s
       ON  s.object_id = i.object_id
      AND  s.index_id  = i.index_id
      AND  s.database_id = DB_ID()
WHERE  o.type = 'U'
ORDER BY s.user_seeks DESC;
```

---

## 3. 実行プランの確認

```sql
-- 推定実行プランの表示（実行しない）
SET SHOWPLAN_XML ON;
GO
EXEC dbo.usp_Todos_BulkUpdate_By_Date '2024-01-01', '2024-12-31';
GO
SET SHOWPLAN_XML OFF;
GO

-- 実際の実行プラン付きで実行
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

EXEC dbo.usp_Todos_BulkUpdate_By_Date '2024-01-01', '2024-12-31';

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
```

### STATISTICS IO の出力の読み方

```
Table 'Todos'. Scan count 1, logical reads 150, physical reads 2, ...
                                      ↑ 論理読み取り: 小さいほど良い
```

---

## 4. パラメータスニッフィング

### 問題

初回実行時のパラメータ値に基づいて最適化されたプランがキャッシュされ、  
別の値で呼ばれた際にサブ最適なプランが流用されることがある。

```sql
-- 初回: @Done = 0（99% の行に一致）→ テーブルスキャンのプランがキャッシュ
-- 2回目: @Done = 1（1% の行）→ インデックスシークのほうが良いのにスキャンで実行
```

### 対策 1: OPTION (RECOMPILE)

毎回プランを再コンパイル。コンパイルコストはあるが確実。

```sql
SELECT Id, Title FROM dbo.Todos WHERE Done = @Done
OPTION (RECOMPILE);
```

### 対策 2: WITH RECOMPILE on プロシージャ

プロシージャ全体を毎回再コンパイル。

```sql
CREATE OR ALTER PROCEDURE dbo.usp_GetTodos @Done BIT
WITH RECOMPILE
AS ...
```

### 対策 3: OPTIMIZE FOR

特定の値でプランを最適化。

```sql
SELECT Id, Title FROM dbo.Todos WHERE Done = @Done
OPTION (OPTIMIZE FOR (@Done = 0));

-- UNKNOWN: パラメータ値を無視して平均的なプランを作る
OPTION (OPTIMIZE FOR (@Done UNKNOWN));
```

### 対策 4: ローカル変数のコピー

```sql
CREATE OR ALTER PROCEDURE dbo.usp_GetTodos
    @Done BIT
AS
BEGIN
    SET NOCOUNT ON;

    -- ローカル変数にコピー（パラメータスニッフィングを回避）
    DECLARE @LocalDone BIT = @Done;

    SELECT Id, Title FROM dbo.Todos WHERE Done = @LocalDone;
END
```

---

## 5. 統計情報

### 手動更新

```sql
-- テーブル全体の統計を更新
UPDATE STATISTICS dbo.Todos;

-- フルスキャンで更新（大きなデータ変動後）
UPDATE STATISTICS dbo.Todos WITH FULLSCAN;

-- データベース全体
EXEC sp_updatestats;
```

### 自動更新の確認

```sql
SELECT
    name,
    is_auto_update_stats_on,
    is_auto_create_stats_on
FROM sys.databases
WHERE name = DB_NAME();
```

---

## 6. クエリヒント

| ヒント | 説明 | 使いどころ |
|---|---|---|
| `NOLOCK` | ダーティリードを許容 | 参照系で厳密性不要な場合 |
| `UPDLOCK` | 読み取り時に更新ロックを取得 | 後続の更新を前提とした読み取り |
| `TABLOCK` | テーブルレベルロック | バルク操作 |
| `INDEX(インデックス名)` | 使用するインデックスを指定 | 最適化ヒントとして |
| `FORCESEEK` | シークを強制 | スキャンを使ってほしくない場合 |
| `FORCESCAN` | スキャンを強制 | 小テーブルなど |
| `RECOMPILE` | クエリ単位で再コンパイル | パラメータスニッフィング対策 |
| `MAXDOP n` | 並列度の制限 | OLTP で並列を抑制 |

```sql
-- インデックスヒント
SELECT Id, Title
FROM   dbo.Todos WITH (INDEX(IX_Todos_CreatedAt))
WHERE  CreatedAt >= '2024-01-01';

-- 並列処理を無効化（OLTP ではシングルスレッドが速いことも）
SELECT Id, Title FROM dbo.Todos
OPTION (MAXDOP 1);
```

---

## 7. 一括操作の最適化

### 大量 UPDATE の分割

```sql
-- ❌ 全件を1回のトランザクションで更新（ログが膨大になりロックが長期化）
UPDATE dbo.Todos SET Done = 1;

-- ✅ バッチ分割して更新
DECLARE @BatchSize INT = 1000;
DECLARE @RowsAffected INT;

REPEAT_LOOP:
    UPDATE TOP (@BatchSize) dbo.Todos
    SET    Done = 1
    WHERE  Done = 0;

    SET @RowsAffected = @@ROWCOUNT;
    IF @RowsAffected = @BatchSize GOTO REPEAT_LOOP;
```

### BULK INSERT / bcp

外部ファイルからの大量データ投入。

```sql
BULK INSERT dbo.Todos
FROM 'C:\data\todos.csv'
WITH
(
    FIELDTERMINATOR = ',',
    ROWTERMINATOR   = '\n',
    FIRSTROW        = 2,       -- ヘッダー行をスキップ
    BATCHSIZE       = 5000,    -- 5000 行単位でコミット
    TABLOCK                    -- テーブルロック（高速化）
);
```

### テーブル値パラメータ（TVP）で一括 INSERT

（詳細は [02_parameters.md](02_parameters.md#4-テーブル値パラメータtvp) 参照）

---

## 8. よくあるアンチパターンと改善策

### ① SELECT * の使用

```sql
-- ❌ 不要な列も転送される
SELECT * FROM dbo.Todos;

-- ✅ 必要な列だけ指定
SELECT Id, Title, Done FROM dbo.Todos;
```

### ② NOLOCK の乱用

```sql
-- ❌ ダーティリードで不整合データを読む可能性
SELECT * FROM dbo.Todos WITH (NOLOCK);

-- ✅ READ COMMITTED SNAPSHOT（RCSI）を有効にして NOLOCK 不要に
ALTER DATABASE MyTodoDB SET READ_COMMITTED_SNAPSHOT ON;
```

### ③ 関数を WHERE 句の列にかける

```sql
-- ❌ インデックスが使えない（列に関数をかけると SARGable でなくなる）
SELECT * FROM dbo.Todos WHERE YEAR(CreatedAt) = 2024;

-- ✅ 範囲指定に変換（インデックスが使える）
SELECT * FROM dbo.Todos
WHERE  CreatedAt >= '2024-01-01' AND CreatedAt < '2025-01-01';
```

### ④ CURSOR での行ごと UPDATE

```sql
-- ❌ カーソルで1行ずつ更新（数万倍遅い）
...カーソルで1行ずつ UPDATE...

-- ✅ 集合ベースの UPDATE
UPDATE dbo.Todos SET Done = 1 WHERE CategoryId = 5;
```

### ⑤ 暗黙の型変換

```sql
-- ❌ 文字列リテラルが暗黙変換されインデックスが使えない場合がある
SELECT * FROM dbo.Todos WHERE Done = 'True';  -- BIT 列に文字列

-- ✅ 正しい型を使う
SELECT * FROM dbo.Todos WHERE Done = 1;
```

### ⑥ NOT IN と NULL

```sql
-- ❌ サブクエリに NULL が含まれると NOT IN は常に空を返す
SELECT * FROM dbo.Todos WHERE CategoryId NOT IN (SELECT Id FROM dbo.Categories WHERE Name IS NULL);

-- ✅ NOT EXISTS を使う
SELECT t.* FROM dbo.Todos t
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Id = t.CategoryId AND c.Name IS NULL);
```
