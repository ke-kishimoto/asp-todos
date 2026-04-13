# 01. ストアドプロシージャの基本構文

## 目次

1. [作成（CREATE PROCEDURE）](#1-作成create-procedure)
2. [変更（ALTER PROCEDURE）](#2-変更alter-procedure)
3. [削除（DROP PROCEDURE）](#3-削除drop-procedure)
4. [冪等な作成（CREATE OR ALTER）](#4-冪等な作成create-or-alter)
5. [実行（EXECUTE / EXEC）](#5-実行execute--exec)
6. [存在確認・定義確認](#6-存在確認定義確認)
7. [SET オプション](#7-set-オプション)

---

## 1. 作成（CREATE PROCEDURE）

```sql
CREATE PROCEDURE dbo.usp_SampleProcedure
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 'Hello, World!' AS Message;
END
GO
```

### 注意点

- スキーマ名（`dbo.`）を必ず付ける。省略するとデフォルトスキーマが使われ混乱の原因になる。
- `SET NOCOUNT ON` は「n rows affected」という余分な結果セットを抑止する。ADO.NET / EF Core との組み合わせで特に重要。
- `GO` はバッチ区切り文字（SQL Server Management Studio / sqlcmd での実行時に必要）。

---

## 2. 変更（ALTER PROCEDURE）

既存プロシージャのロジックを書き換える。権限設定はそのまま維持される。

```sql
ALTER PROCEDURE dbo.usp_SampleProcedure
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 'Updated Hello!' AS Message;
END
GO
```

---

## 3. 削除（DROP PROCEDURE）

```sql
DROP PROCEDURE IF EXISTS dbo.usp_SampleProcedure;
GO
```

`IF EXISTS` を使うと、プロシージャが存在しない場合もエラーにならない（SQL Server 2016 以降）。

---

## 4. 冪等な作成（CREATE OR ALTER）

デプロイスクリプトでよく使うパターン。存在すれば変更、なければ作成する（SQL Server 2016 SP1 以降）。

```sql
CREATE OR ALTER PROCEDURE dbo.usp_SampleProcedure
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 'CREATE OR ALTER' AS Mode;
END
GO
```

> このプロジェクトの `usp_Todos_BulkUpdate.sql` でも `CREATE OR ALTER` を採用しています。

---

## 5. 実行（EXECUTE / EXEC）

```sql
-- 基本実行
EXECUTE dbo.usp_SampleProcedure;

-- 短縮形
EXEC dbo.usp_SampleProcedure;

-- パラメータ付き（位置指定）
EXEC dbo.usp_Todos_BulkUpdate_By_Date '2024-01-01', '2024-12-31';

-- パラメータ付き（名前指定）— 順序を問わないので可読性が高い
EXEC dbo.usp_Todos_BulkUpdate_By_Date
    @StartDate = '2024-01-01',
    @EndDate   = '2024-12-31';
```

### 戻り値（RETURN）

`RETURN` で整数値（0 = 成功、非ゼロ = エラーコード）を返すことができます。

```sql
CREATE OR ALTER PROCEDURE dbo.usp_GetReturnValue
AS
BEGIN
    SET NOCOUNT ON;
    -- 処理...
    RETURN 0;   -- 正常終了
END
GO

DECLARE @ReturnCode INT;
EXEC @ReturnCode = dbo.usp_GetReturnValue;
PRINT @ReturnCode;  -- 0
```

---

## 6. 存在確認・定義確認

### 存在確認

```sql
-- sys.objects を使う方法
IF OBJECT_ID('dbo.usp_SampleProcedure', 'P') IS NOT NULL
    PRINT '存在します';
ELSE
    PRINT '存在しません';

-- sys.procedures を使う方法
SELECT name, create_date, modify_date
FROM   sys.procedures
WHERE  name = 'usp_SampleProcedure';
```

### 定義確認

```sql
-- DDL を取得
EXEC sp_helptext 'dbo.usp_SampleProcedure';

-- または
SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.usp_SampleProcedure'));
```

### パラメータ一覧

```sql
EXEC sp_help 'dbo.usp_Todos_BulkUpdate_By_Date';
```

---

## 7. SET オプション

ストアドプロシージャ内では以下の `SET` オプションを冒頭に置くことが推奨されます。

| オプション | 推奨値 | 説明 |
|---|---|---|
| `SET NOCOUNT ON` | ON | 影響行数メッセージを抑止 |
| `SET XACT_ABORT ON` | ON | エラー時に自動ロールバック（トランザクション使用時） |
| `SET ANSI_NULLS ON` | ON | NULL 比較の ANSI 準拠を有効化 |
| `SET QUOTED_IDENTIFIER ON` | ON | ダブルクォートを識別子として扱う |

```sql
CREATE OR ALTER PROCEDURE dbo.usp_BestPracticeTemplate
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- ここに処理を書く
END
GO
```

> `SET ANSI_NULLS ON` と `SET QUOTED_IDENTIFIER ON` はプロシージャ本体の外（`AS BEGIN` の前）に書くことで、プロシージャのメタデータとして保存されます。

---

## このプロジェクトでの実例

```sql
-- usp_Todos_BulkUpdate_By_Date.sql より
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_Todos_BulkUpdate_By_Date
    @StartDate Date,
    @EndDate   Date
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRAN;
            UPDATE dbo.Todos SET Done = 'True'
            WHERE  CreatedAt BETWEEN @StartDate AND @EndDate;
        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH
END
GO
```
