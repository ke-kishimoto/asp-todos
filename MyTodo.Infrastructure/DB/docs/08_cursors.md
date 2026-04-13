# 08. カーソル

## 目次

1. [カーソルとは・使いどころ](#1-カーソルとは使いどころ)
2. [カーソルの基本構文](#2-カーソルの基本構文)
3. [カーソルの種別](#3-カーソルの種別)
4. [カーソルの代替手段（推奨）](#4-カーソルの代替手段推奨)
5. [実践パターン：行ごとの条件分岐処理](#5-実践パターン行ごとの条件分岐処理)
6. [パフォーマンス注意点](#6-パフォーマンス注意点)

---

## 1. カーソルとは・使いどころ

カーソルは結果セットを **1 行ずつ** 処理するための仕組みです。

**通常は集合演算（UPDATE/INSERT/SELECT）で代替できるため、カーソルの使用は最後の手段です。**

### カーソルを使う（検討）べきケース

- 行ごとに異なるストアドプロシージャを呼び出す必要がある
- 前の行の結果に基づいて次の行を処理する（累積計算など）
- 行ごとに複雑な分岐処理があり、集合演算で表現しにくい

---

## 2. カーソルの基本構文

```sql
CREATE OR ALTER PROCEDURE dbo.usp_ProcessTodosOneByOne
AS
BEGIN
    SET NOCOUNT ON;

    -- ① カーソル変数の宣言
    DECLARE @TodoId    INT;
    DECLARE @TodoTitle NVARCHAR(200);

    -- ② カーソル定義
    DECLARE todo_cursor CURSOR
        LOCAL           -- スコープをローカルに限定（推奨）
        FAST_FORWARD    -- READ_ONLY + FORWARD_ONLY の最速設定
    FOR
        SELECT Id, Title
        FROM   dbo.Todos
        WHERE  Done = 0
        ORDER BY CreatedAt;

    -- ③ カーソルオープン
    OPEN todo_cursor;

    -- ④ 最初の行を取得
    FETCH NEXT FROM todo_cursor INTO @TodoId, @TodoTitle;

    -- ⑤ 行がある間ループ（@@FETCH_STATUS = 0 が正常取得）
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- ここで行ごとの処理
        PRINT CAST(@TodoId AS NVARCHAR(10)) + N': ' + @TodoTitle;

        -- 必要に応じて更新・プロシージャ呼び出しなど
        -- EXEC dbo.usp_SomeProcessPerRow @TodoId;

        -- ⑥ 次の行へ
        FETCH NEXT FROM todo_cursor INTO @TodoId, @TodoTitle;
    END

    -- ⑦ クローズ・解放（必ず実行）
    CLOSE todo_cursor;
    DEALLOCATE todo_cursor;
END
GO
```

---

## 3. カーソルの種別

| オプション | 説明 | 推奨 |
|---|---|---|
| `LOCAL` | 現在のプロシージャ内のみ有効 | ✅ 推奨 |
| `GLOBAL` | セッション全体で有効（危険） | 非推奨 |
| `FAST_FORWARD` | 前進のみ・読み取り専用・最速 | ✅ 推奨 |
| `FORWARD_ONLY` | 前進のみ（更新可能） | — |
| `SCROLL` | 任意方向に移動可能 | 必要時のみ |
| `READ_ONLY` | 読み取り専用 | ✅ 推奨 |
| `UPDATABLE` | カーソル位置での更新が可能 | 必要時のみ |
| `STATIC` | 取得時のスナップショットを使用 | 必要時のみ |
| `DYNAMIC` | 常に最新データを参照 | 注意 |
| `KEYSET` | キーセットを保持 | 特殊用途 |

### 推奨の書き方

```sql
DECLARE my_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT ...;
```

---

## 4. カーソルの代替手段（推奨）

### パターン A: UPDATE / INSERT に集合演算を使う

```sql
-- ❌ カーソルで1行ずつ
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM dbo.Todos WHERE Done = 0;
OPEN c;
FETCH NEXT FROM c INTO @Id;
WHILE @@FETCH_STATUS = 0
BEGIN
    UPDATE dbo.Todos SET Done = 1 WHERE Id = @Id;
    FETCH NEXT FROM c INTO @Id;
END
CLOSE c; DEALLOCATE c;

-- ✅ 集合演算で一括（圧倒的に速い）
UPDATE dbo.Todos SET Done = 1 WHERE Done = 0;
```

### パターン B: WHILE + 主キーを使う

```sql
-- カーソルの代わりに WHILE + TOP 1 で行を一つずつ処理
DECLARE @Id INT = 0;

WHILE 1 = 1
BEGIN
    SELECT TOP 1 @Id = Id
    FROM   dbo.Todos
    WHERE  Id > @Id AND Done = 0
    ORDER BY Id;

    IF @@ROWCOUNT = 0 BREAK;

    -- 処理
    EXEC dbo.usp_ProcessOneTodo @Id;
END
```

### パターン C: STRING_AGG / FOR XML PATH（文字列集約）

```sql
-- カーソルで文字列を連結する代わりに STRING_AGG を使う
SELECT STRING_AGG(Title, ', ') WITHIN GROUP (ORDER BY CreatedAt) AS AllTitles
FROM   dbo.Todos
WHERE  Done = 0;
```

---

## 5. 実践パターン：行ごとの条件分岐処理

「カーソルが本当に必要」な例: 行ごとに異なるプロシージャを呼ぶ。

```sql
CREATE OR ALTER PROCEDURE dbo.usp_DispatchTodoActions
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TodoId   INT;
    DECLARE @Priority TINYINT;
    DECLARE @Done     BIT;

    DECLARE dispatch_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT Id, Priority, Done
        FROM   dbo.Todos
        WHERE  ProcessedAt IS NULL
        ORDER BY Priority DESC, CreatedAt;

    OPEN dispatch_cursor;
    FETCH NEXT FROM dispatch_cursor INTO @TodoId, @Priority, @Done;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            IF @Priority >= 8
                EXEC dbo.usp_ProcessHighPriorityTodo   @TodoId;
            ELSE IF @Priority >= 4
                EXEC dbo.usp_ProcessNormalPriorityTodo @TodoId;
            ELSE
                EXEC dbo.usp_ProcessLowPriorityTodo    @TodoId;
        END TRY
        BEGIN CATCH
            -- 1件失敗しても続行
            PRINT N'TodoId=' + CAST(@TodoId AS NVARCHAR(10))
                  + N' の処理に失敗: ' + ERROR_MESSAGE();
        END CATCH

        FETCH NEXT FROM dispatch_cursor INTO @TodoId, @Priority, @Done;
    END

    CLOSE dispatch_cursor;
    DEALLOCATE dispatch_cursor;
END
GO
```

---

## 6. パフォーマンス注意点

- カーソルは集合演算に比べて **数十〜数千倍遅い** ことがある
- 大量データ（数万行以上）に対するカーソル処理は避ける
- どうしてもカーソルが必要な場合は `FAST_FORWARD` を指定する
- `tempdb` の共有を避けるため `LOCAL` を必ず指定する
- カーソルは必ず `CLOSE` → `DEALLOCATE` する（リソースリーク防止）

### `@@FETCH_STATUS` の値

| 値 | 意味 |
|---|---|
| 0 | 正常に取得できた |
| -1 | 結果セットの末尾を超えた（行なし） |
| -2 | フェッチした行が削除されている（DYNAMIC カーソル） |
| -9 | カーソルがフェッチ操作を実行していない |
