# 04. エラーハンドリング

## 目次

1. [TRY / CATCH の基本](#1-try--catch-の基本)
2. [エラー情報取得関数](#2-エラー情報取得関数)
3. [THROW（推奨）](#3-throw推奨)
4. [RAISERROR（旧式・互換性）](#4-raiserror旧式互換性)
5. [エラーの再スロー](#5-エラーの再スロー)
6. [ネストされた TRY/CATCH](#6-ネストされた-trycatch)
7. [エラーログテーブルへの記録](#7-エラーログテーブルへの記録)
8. [このプロジェクトでの実例](#8-このプロジェクトでの実例)

---

## 1. TRY / CATCH の基本

```sql
BEGIN TRY
    -- 保護したい処理
    UPDATE dbo.Todos SET Done = 1 WHERE Id = 99999;
END TRY
BEGIN CATCH
    -- エラー発生時の処理
    PRINT ERROR_MESSAGE();
END CATCH
```

TRY ブロック内でエラーが発生すると、即座に CATCH ブロックへジャンプします。  
CATCH ブロックの後、実行は通常どおり続きます（プロシージャは終了しない）。

---

## 2. エラー情報取得関数

CATCH ブロック内でのみ有効な関数群。

| 関数 | 戻り値の型 | 説明 |
|---|---|---|
| `ERROR_NUMBER()` | INT | エラー番号 |
| `ERROR_SEVERITY()` | INT | 重大度（0〜25） |
| `ERROR_STATE()` | INT | 状態（1〜127） |
| `ERROR_MESSAGE()` | NVARCHAR(2048) | エラーメッセージ |
| `ERROR_PROCEDURE()` | SYSNAME | エラーが発生したプロシージャ名 |
| `ERROR_LINE()` | INT | エラーが発生した行番号 |

```sql
BEGIN TRY
    SELECT 1 / 0;  -- ゼロ除算エラー
END TRY
BEGIN CATCH
    SELECT
        ERROR_NUMBER()    AS ErrorNumber,
        ERROR_SEVERITY()  AS Severity,
        ERROR_STATE()     AS State,
        ERROR_MESSAGE()   AS Message,
        ERROR_PROCEDURE() AS Procedure,
        ERROR_LINE()      AS Line;
END CATCH
```

---

## 3. THROW（推奨）

SQL Server 2012 以降。エラーをスローするシンプルな構文。

```sql
-- 自分でエラーを発生させる（エラー番号は 50000 以上を使う）
THROW 50001, 'カスタムエラーメッセージ', 1;
-- 引数: エラー番号, メッセージ, 状態
```

### よくある使い方

```sql
CREATE OR ALTER PROCEDURE dbo.usp_DeleteTodo
    @TodoId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Todos WHERE Id = @TodoId)
    BEGIN
        THROW 50404, 'Todo が見つかりません。', 1;
    END

    DELETE FROM dbo.Todos WHERE Id = @TodoId;
END
GO
```

### エラー番号の使い分け

| 番号範囲 | 用途 |
|---|---|
| 1〜49999 | システム予約（使用不可） |
| **50000〜** | **ユーザー定義エラー（推奨範囲）** |
| 50000 | 一般エラー |
| 50400〜50499 | データ不存在（HTTP 4xx 相当） |
| 50500〜50599 | サーバー内部エラー（HTTP 5xx 相当） |

---

## 4. RAISERROR（旧式・互換性）

`THROW` 登場以前の構文。フォーマット文字列（`%s`, `%d`）を使えるのが特徴。

```sql
-- 基本形
RAISERROR('エラーが発生しました。', 16, 1);
-- 引数: メッセージ, 重大度, 状態

-- フォーマット文字列
RAISERROR('Id=%d のレコードが見つかりません。', 16, 1, @TodoId);

-- WITH NOWAIT: メッセージをすぐに返す（デバッグ用）
RAISERROR('処理中...', 10, 1) WITH NOWAIT;
```

### 重大度の目安

| 重大度 | 意味 |
|---|---|
| 0〜9 | 情報メッセージ（エラーとして扱われない） |
| 10 | 情報メッセージ（CATCH には入らない） |
| 11〜16 | ユーザーが修正可能なエラー |
| 17〜19 | リソース・システムエラー |
| 20〜25 | 致命的エラー（接続切断） |

> **推奨**: 新しいコードでは `RAISERROR` より `THROW` を使うこと。

---

## 5. エラーの再スロー

CATCH で捕捉したエラーを上位の呼び出し元へ再スローする。

```sql
BEGIN TRY
    EXEC dbo.usp_SomeRiskyOperation;
END TRY
BEGIN CATCH
    -- ログ記録などを行ったうえで再スロー
    INSERT INTO dbo.ErrorLog (Message, OccurredAt)
    VALUES (ERROR_MESSAGE(), GETDATE());

    THROW;  -- 引数なしで元のエラーをそのまま再スロー
END CATCH
```

> `THROW;`（引数なし）は CATCH ブロック内でのみ使用可能。元のエラー番号・メッセージ・状態を維持する。

---

## 6. ネストされた TRY/CATCH

```sql
BEGIN TRY
    BEGIN TRAN;

    BEGIN TRY
        UPDATE dbo.Todos SET Done = 1 WHERE Id = 1;
    END TRY
    BEGIN CATCH
        -- 内側のエラーを無視して続行したい場合
        PRINT '1件目の更新に失敗しましたが続行します: ' + ERROR_MESSAGE();
    END CATCH

    -- 2件目は必ず実行
    UPDATE dbo.Todos SET Done = 1 WHERE Id = 2;

    COMMIT;
END TRY
BEGIN CATCH
    ROLLBACK;
    THROW;
END CATCH
```

---

## 7. エラーログテーブルへの記録

### ログテーブルの定義例

```sql
CREATE TABLE dbo.ErrorLog
(
    Id            INT           IDENTITY(1,1) PRIMARY KEY,
    ErrorNumber   INT,
    ErrorSeverity INT,
    ErrorState    INT,
    ErrorMessage  NVARCHAR(2048),
    ProcedureName SYSNAME,
    LineNumber    INT,
    OccurredAt    DATETIME2     NOT NULL DEFAULT GETDATE(),
    ServerName    SYSNAME       NOT NULL DEFAULT @@SERVERNAME,
    LoginName     SYSNAME       NOT NULL DEFAULT SYSTEM_USER
);
```

### ログ記録プロシージャ

```sql
CREATE OR ALTER PROCEDURE dbo.usp_LogError
AS
BEGIN
    SET NOCOUNT ON;

    IF ERROR_NUMBER() IS NULL
        RETURN;  -- CATCH ブロック外では何もしない

    INSERT INTO dbo.ErrorLog
        (ErrorNumber, ErrorSeverity, ErrorState, ErrorMessage, ProcedureName, LineNumber)
    VALUES
        (ERROR_NUMBER(), ERROR_SEVERITY(), ERROR_STATE(),
         ERROR_MESSAGE(), ERROR_PROCEDURE(), ERROR_LINE());
END
GO

-- 利用例
BEGIN TRY
    EXEC dbo.usp_SomeOperation;
END TRY
BEGIN CATCH
    EXEC dbo.usp_LogError;
    THROW;
END CATCH
```

---

## 8. このプロジェクトでの実例

```sql
-- usp_Todos_BulkUpdate_By_Date.sql より
BEGIN TRY
    BEGIN TRAN;
        UPDATE dbo.Todos SET Done = 'True'
        WHERE  CreatedAt BETWEEN @StartDate AND @EndDate;
    COMMIT;
END TRY
BEGIN CATCH
    ROLLBACK;
    THROW;  -- エラーを上位（アプリケーション層）に再スロー
END CATCH
```

このパターンのポイント:
- トランザクションとエラー処理を組み合わせている（詳細は [05_transactions.md](05_transactions.md)）
- `THROW;` で元のエラーをそのまま呼び出し元に伝える
- C# 側では `SqlException` として受け取れる
