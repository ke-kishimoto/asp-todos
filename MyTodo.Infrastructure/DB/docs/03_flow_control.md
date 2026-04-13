# 03. 制御フロー

## 目次

1. [IF / ELSE](#1-if--else)
2. [CASE 式](#2-case-式)
3. [WHILE ループ](#3-while-ループ)
4. [BREAK / CONTINUE](#4-break--continue)
5. [GOTO](#5-goto)
6. [WAITFOR](#6-waitfor)
7. [実践パターン：バリデーション付き UPDATE](#7-実践パターンバリデーション付き-update)

---

## 1. IF / ELSE

```sql
CREATE OR ALTER PROCEDURE dbo.usp_CompleteTodo
    @TodoId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 対象レコードの存在チェック
    IF NOT EXISTS (SELECT 1 FROM dbo.Todos WHERE Id = @TodoId)
    BEGIN
        RAISERROR('指定された Todo が見つかりません。Id: %d', 16, 1, @TodoId);
        RETURN;
    END

    -- 既に完了済みのチェック
    IF EXISTS (SELECT 1 FROM dbo.Todos WHERE Id = @TodoId AND Done = 1)
    BEGIN
        PRINT '既に完了済みです。';
        RETURN;
    END

    UPDATE dbo.Todos
    SET    Done = 1
    WHERE  Id = @TodoId;

    PRINT '完了に更新しました。';
END
GO
```

### BEGIN...END ブロック

1 文だけなら `BEGIN...END` を省略できますが、**常に書く** ことを推奨します。後でコードを追加したときのバグを防げます。

```sql
-- 省略可（非推奨）
IF @Done = 1
    UPDATE dbo.Todos SET Done = 1 WHERE Id = @Id;

-- 推奨
IF @Done = 1
BEGIN
    UPDATE dbo.Todos SET Done = 1 WHERE Id = @Id;
END
```

---

## 2. CASE 式

### 単純 CASE（値の比較）

```sql
SELECT
    Id,
    Title,
    CASE Done
        WHEN 0 THEN '未完了'
        WHEN 1 THEN '完了'
        ELSE        '不明'
    END AS StatusLabel
FROM dbo.Todos;
```

### 検索 CASE（条件式）

```sql
SELECT
    Id,
    Title,
    CreatedAt,
    CASE
        WHEN CreatedAt >= DATEADD(DAY, -7, GETDATE()) THEN '今週'
        WHEN CreatedAt >= DATEADD(DAY, -30, GETDATE()) THEN '今月'
        ELSE '1ヶ月以上前'
    END AS Period
FROM dbo.Todos
ORDER BY CreatedAt DESC;
```

### UPDATE での CASE 活用

```sql
-- ステータスを反転させる
UPDATE dbo.Todos
SET    Done = CASE WHEN Done = 0 THEN 1 ELSE 0 END
WHERE  Id = @TodoId;
```

---

## 3. WHILE ループ

SQL Server には `FOR` ループがないため、繰り返し処理はすべて `WHILE` で実装します。

```sql
CREATE OR ALTER PROCEDURE dbo.usp_RetryableUpdate
    @TodoId     INT,
    @MaxRetry   INT = 3
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Retry    INT = 0;
    DECLARE @Success  BIT = 0;

    WHILE @Retry < @MaxRetry AND @Success = 0
    BEGIN
        BEGIN TRY
            UPDATE dbo.Todos SET Done = 1 WHERE Id = @TodoId;
            SET @Success = 1;
        END TRY
        BEGIN CATCH
            SET @Retry = @Retry + 1;
            IF @Retry >= @MaxRetry
                THROW;  -- 最大リトライ回数に達したら例外を再スロー
        END CATCH
    END
END
GO
```

---

## 4. BREAK / CONTINUE

```sql
DECLARE @Counter INT = 0;

WHILE @Counter < 10
BEGIN
    SET @Counter = @Counter + 1;

    -- 偶数をスキップ
    IF @Counter % 2 = 0
        CONTINUE;

    -- 7 以上で終了
    IF @Counter >= 7
        BREAK;

    PRINT @Counter;  -- 1, 3, 5 が出力される
END
```

---

## 5. GOTO

`GOTO` は基本的に使用しない（可読性が低下する）。  
唯一許容される用途は「エラー時のジャンプ」パターンですが、現代の SQL Server では `TRY/CATCH` が優先されます。

```sql
-- GOTO は原則使わない。TRY/CATCH を使うこと。
-- 参考として記載のみ。

BEGIN TRAN
    UPDATE dbo.Todos SET Done = 1 WHERE Id = 1;
    IF @@ERROR <> 0 GOTO ErrorHandler
    UPDATE dbo.Todos SET Done = 1 WHERE Id = 2;
    IF @@ERROR <> 0 GOTO ErrorHandler
COMMIT;
RETURN;

ErrorHandler:
    ROLLBACK;
    PRINT 'エラーが発生しました。';
```

---

## 6. WAITFOR

指定時間待機する。バッチスケジューリングや負荷テストで使用。

```sql
-- 5 秒待つ
WAITFOR DELAY '00:00:05';

-- 特定の時刻まで待つ
WAITFOR TIME '23:00:00';
```

---

## 7. 実践パターン：バリデーション付き UPDATE

複数の入力バリデーションをまとめて行うパターン。

```sql
CREATE OR ALTER PROCEDURE dbo.usp_UpdateTodo
    @TodoId INT,
    @Title  NVARCHAR(200),
    @Done   BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -------------------------------------------------------
    -- バリデーション
    -------------------------------------------------------
    IF @TodoId IS NULL OR @TodoId <= 0
    BEGIN
        THROW 50001, 'TodoId は正の整数で指定してください。', 1;
    END

    IF @Title IS NULL OR LEN(TRIM(@Title)) = 0
    BEGIN
        THROW 50002, 'Title は必須です。', 1;
    END

    IF LEN(@Title) > 200
    BEGIN
        THROW 50003, 'Title は 200 文字以内で指定してください。', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Todos WHERE Id = @TodoId)
    BEGIN
        THROW 50404, '指定された Todo が見つかりません。', 1;
    END

    -------------------------------------------------------
    -- 更新処理
    -------------------------------------------------------
    UPDATE dbo.Todos
    SET    Title = TRIM(@Title),
           Done  = @Done
    WHERE  Id = @TodoId;
END
GO
```
