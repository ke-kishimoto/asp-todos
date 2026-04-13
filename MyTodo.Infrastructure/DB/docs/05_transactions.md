# 05. トランザクション

## 目次

1. [トランザクションの基本](#1-トランザクションの基本)
2. [@@TRANCOUNT と入れ子トランザクション](#2-trancount-と入れ子トランザクション)
3. [SET XACT_ABORT ON](#3-set-xact_abort-on)
4. [セーブポイント（SAVE TRAN）](#4-セーブポイントsave-tran)
5. [分離レベル](#5-分離レベル)
6. [デッドロック対策](#6-デッドロック対策)
7. [このプロジェクトでの実例](#7-このプロジェクトでの実例)
8. [推奨テンプレート](#8-推奨テンプレート)

---

## 1. トランザクションの基本

```sql
BEGIN TRAN;             -- トランザクション開始
    -- DML 操作
    INSERT INTO ...;
    UPDATE ...;
    DELETE ...;
COMMIT;                 -- 正常終了時：確定
-- または
ROLLBACK;               -- 異常終了時：取り消し
```

### TRY/CATCH との組み合わせ（基本パターン）

```sql
BEGIN TRY
    BEGIN TRAN;

        UPDATE dbo.Todos SET Done = 1 WHERE Id = 1;
        UPDATE dbo.Todos SET Done = 1 WHERE Id = 2;

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK;

    THROW;
END CATCH
```

> `IF @@TRANCOUNT > 0` のチェックは重要。既にロールバック済みのトランザクションに対して `ROLLBACK` を呼ぶとエラーになるため。

---

## 2. @@TRANCOUNT と入れ子トランザクション

`@@TRANCOUNT` は現在アクティブなトランザクションのネスト数を返します。

| 操作 | @@TRANCOUNT の変化 |
|---|---|
| `BEGIN TRAN` | +1 |
| `COMMIT` | -1（0 になったとき実際にコミット） |
| `ROLLBACK` | 0 にリセット（すべての入れ子も取り消し） |

```sql
SELECT @@TRANCOUNT;  -- 0

BEGIN TRAN;
    SELECT @@TRANCOUNT;  -- 1

    BEGIN TRAN;  -- 入れ子（内側）
        SELECT @@TRANCOUNT;  -- 2
    COMMIT;      -- @@TRANCOUNT = 1（まだコミットされていない）

COMMIT;          -- @@TRANCOUNT = 0（ここで実際にコミット）
```

### 入れ子での注意

内側の `ROLLBACK` は **すべての** 入れ子を取り消します（外側のトランザクションも取り消し）。

---

## 3. SET XACT_ABORT ON

エラー発生時に自動的にトランザクションをロールバックし、バッチを中断します。

```sql
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;
        -- XACT_ABORT ON の場合、エラーが起きると自動 ROLLBACK される
        UPDATE dbo.Todos SET Done = 1 WHERE Id = 1;
        UPDATE dbo.Todos SET Done = 1 WHERE Id = 2;
    COMMIT;
END TRY
BEGIN CATCH
    -- XACT_ABORT ON の場合、ここに来た時点で既にロールバック済みのことがある
    IF @@TRANCOUNT > 0
        ROLLBACK;
    THROW;
END CATCH
```

**推奨**: トランザクションを使うプロシージャでは必ず `SET XACT_ABORT ON` を宣言する。

---

## 4. セーブポイント（SAVE TRAN）

トランザクション内に「中間チェックポイント」を設け、そこまで部分的にロールバックできます。

```sql
BEGIN TRY
    BEGIN TRAN;

        UPDATE dbo.Todos SET Done = 1 WHERE Id = 1;

        SAVE TRAN TodoUpdateCheckpoint;  -- セーブポイント設定

        BEGIN TRY
            UPDATE dbo.Todos SET Done = 1 WHERE Id = 999;  -- 存在しない ID
        END TRY
        BEGIN CATCH
            -- セーブポイントまでロールバック（Id=1 の更新は残る）
            ROLLBACK TRAN TodoUpdateCheckpoint;
            PRINT '2件目の更新に失敗しましたが続行します。';
        END CATCH

        UPDATE dbo.Todos SET Done = 1 WHERE Id = 2;

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK;
    THROW;
END CATCH
```

---

## 5. 分離レベル

タイムスタンプ分離レベルを指定することで、ダーティリード・ファントムリードなどを制御できます。

```sql
-- 現在の分離レベルを確認
DBCC USEROPTIONS;

-- セッション全体の分離レベルを変更
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;  -- デフォルト
```

| 分離レベル | ダーティリード | 反復不可読 | ファントムリード | 説明 |
|---|---|---|---|---|
| `READ UNCOMMITTED` | あり | あり | あり | 最も低い。未確定データを読める |
| `READ COMMITTED` | なし | あり | あり | **デフォルト** |
| `REPEATABLE READ` | なし | なし | あり | 同一トランザクション内で同じデータを再読できる |
| `SERIALIZABLE` | なし | なし | なし | 最も高い。ファントムリードも防ぐ |
| `SNAPSHOT` | なし | なし | なし | 行バージョンを使う（楽観的ロック相当） |
| `READ COMMITTED SNAPSHOT` | なし | あり | あり | `tempdb` にバージョンを作成（Azure SQL DB デフォルト） |

### ヒントによる一時的な変更

```sql
-- テーブルヒントで特定テーブルのみロック挙動を変更
SELECT Id, Title FROM dbo.Todos WITH (NOLOCK);     -- ダーティリードを許容（READ UNCOMMITTED 相当）
SELECT Id, Title FROM dbo.Todos WITH (UPDLOCK);    -- 読み取り時に更新ロックを取得
SELECT Id, Title FROM dbo.Todos WITH (HOLDLOCK);   -- SERIALIZABLE 相当
```

---

## 6. デッドロック対策

### デッドロックが起きる典型パターン

```
セッション A: Todos テーブル → Categories テーブルの順にロック取得
セッション B: Categories テーブル → Todos テーブルの順にロック取得
→ 相互待機でデッドロック
```

### 対策

1. **ロック取得順序を統一する**（最も効果的）
2. `SNAPSHOT` 分離レベルを使う
3. デッドロック発生時のリトライロジックをアプリ側に実装
4. トランザクションをできるだけ短くする

```sql
-- アプリ側リトライの考え方（C# 擬似コード）
for (int i = 0; i < maxRetry; i++)
{
    try
    {
        await ExecuteStoredProcedureAsync();
        break;
    }
    catch (SqlException ex) when (ex.Number == 1205)  // デッドロック
    {
        if (i == maxRetry - 1) throw;
        await Task.Delay(100 * (i + 1));
    }
}
```

---

## 7. このプロジェクトでの実例

```sql
-- usp_Todos_BulkUpdate.sql
CREATE OR ALTER PROCEDURE dbo.ups_Todos_BulkUpdate
AS
BEGIN
    BEGIN TRY
        BEGIN TRAN;
            UPDATE dbo.Todos SET Done = 'True';
        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH
END
GO
```

このプロシージャは全件更新という重い操作を安全にトランザクション管理しています。  
`SET NOCOUNT ON` と `SET XACT_ABORT ON` を追加するとさらに堅牢になります。

---

## 8. 推奨テンプレート

業務システムで使うトランザクションの推奨パターン:

```sql
CREATE OR ALTER PROCEDURE dbo.usp_YourProcedureName
    -- パラメータ
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

            -- ここに DML 操作を書く
            UPDATE dbo.Todos SET Done = 1 WHERE ...;
            INSERT INTO dbo.AuditLog (...) VALUES (...);

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK;

        -- 必要に応じてログ記録
        -- EXEC dbo.usp_LogError;

        THROW;
    END CATCH
END
GO
```
