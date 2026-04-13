# 09. セキュリティ・権限

## 目次

1. [基本的な権限モデル](#1-基本的な権限モデル)
2. [GRANT / DENY / REVOKE](#2-grant--deny--revoke)
3. [EXECUTE AS（実行コンテキストの切り替え）](#3-execute-as実行コンテキストの切り替え)
4. [所有権チェーン（Ownership Chaining）](#4-所有権チェーンownership-chaining)
5. [スキーマによるアクセス制御](#5-スキーマによるアクセス制御)
6. [機密データの保護](#6-機密データの保護)
7. [SQLインジェクション防止](#7-sqlインジェクション防止)
8. [権限確認クエリ集](#8-権限確認クエリ集)

---

## 1. 基本的な権限モデル

SQL Server の権限は「プリンシパル → セキュリタブル → 権限」の構造で管理します。

```
プリンシパル（誰が）: ログイン / ユーザー / ロール
     ↓ 持つ
権限（何を）: EXECUTE / SELECT / INSERT / UPDATE / DELETE / ALTER / CONTROL ...
     ↓ に対する
セキュリタブル（何に）: サーバー / データベース / スキーマ / テーブル / プロシージャ ...
```

### ロールの種類

| ロール | 説明 |
|---|---|
| `db_owner` | データベースの完全制御 |
| `db_datareader` | 全テーブルの SELECT |
| `db_datawriter` | 全テーブルの INSERT/UPDATE/DELETE |
| `db_ddladmin` | DDL 操作（CREATE/ALTER/DROP） |
| `db_denydatareader` | SELECT を明示的に拒否 |
| `db_denydatawriter` | 書き込みを明示的に拒否 |

---

## 2. GRANT / DENY / REVOKE

```sql
-- ユーザー / ロールへの権限付与
GRANT EXECUTE ON dbo.usp_Todos_BulkUpdate TO AppUser;
GRANT EXECUTE ON SCHEMA::dbo TO AppRole;    -- スキーマ内の全オブジェクトに実行権限

-- 権限の取り消し
REVOKE EXECUTE ON dbo.usp_Todos_BulkUpdate FROM AppUser;

-- 権限の明示的拒否（ロールから継承した権限も上書きして拒否）
DENY EXECUTE ON dbo.usp_Todos_BulkUpdate TO GuestUser;
```

### ストアドプロシージャへの EXECUTE 権限のみ付与（推奨パターン）

アプリケーションユーザーには **テーブルへの直接アクセスを与えず** 、プロシージャの EXECUTE だけを与えます。

```sql
-- アプリ用ロールを作成
CREATE ROLE AppRole;

-- プロシージャの EXECUTE 権限のみ付与
GRANT EXECUTE ON dbo.usp_GetTodoById        TO AppRole;
GRANT EXECUTE ON dbo.usp_InsertTodo         TO AppRole;
GRANT EXECUTE ON dbo.usp_UpdateTodo         TO AppRole;
GRANT EXECUTE ON dbo.usp_DeleteTodo         TO AppRole;
GRANT EXECUTE ON dbo.usp_Todos_BulkUpdate   TO AppRole;

-- テーブルへの直接アクセスは付与しない
-- （所有権チェーンにより、プロシージャ経由でのアクセスは可能）

-- アプリユーザーをロールに追加
ALTER ROLE AppRole ADD MEMBER AppUser;
```

---

## 3. EXECUTE AS（実行コンテキストの切り替え）

プロシージャを実行するコンテキスト（ユーザー）を指定します。

```sql
-- プロシージャ内で別のユーザーとして実行
CREATE OR ALTER PROCEDURE dbo.usp_ElevatedOperation
WITH EXECUTE AS 'dbo'   -- dbo として実行
AS
BEGIN
    SET NOCOUNT ON;
    -- dbo 権限が必要な操作
    EXEC sp_addrolemember 'db_datareader', 'NewUser';
END
GO
```

### EXECUTE AS オプション

| オプション | 説明 |
|---|---|
| `EXECUTE AS CALLER` | 呼び出し元のコンテキスト（デフォルト） |
| `EXECUTE AS SELF` | プロシージャ作成者のコンテキスト |
| `EXECUTE AS OWNER` | プロシージャ所有者のコンテキスト |
| `EXECUTE AS 'UserName'` | 特定ユーザーのコンテキスト |

> `EXECUTE AS SELF` / `EXECUTE AS OWNER` は所有権チェーンが切れる場合（クロスDB等）に有効。

---

## 4. 所有権チェーン（Ownership Chaining）

同一スキーマ内でプロシージャとテーブルの所有者が同じ場合、**プロシージャ経由のアクセスには個別テーブル権限が不要**です。

```
AppUser → EXECUTE on usp_GetTodos → SELECT on dbo.Todos (所有者: dbo)
                                                         ↑
                                    (usp_GetTodos の所有者も dbo → チェーン成立)
```

```sql
-- この設定だけで AppUser は usp_GetTodos を実行でき、Todos テーブルも読める
GRANT EXECUTE ON dbo.usp_GetTodos TO AppUser;
-- GRANT SELECT ON dbo.Todos TO AppUser; は不要！
```

---

## 5. スキーマによるアクセス制御

異なる機能グループをスキーマで分離し、スキーマ単位で権限を管理します。

```sql
-- スキーマ作成
CREATE SCHEMA report AUTHORIZATION dbo;
CREATE SCHEMA admin  AUTHORIZATION dbo;

-- 集計・レポート系プロシージャをスキーマに配置
CREATE OR ALTER PROCEDURE report.usp_GetMonthlySummary ...

-- レポートロールにスキーマ単位の実行権限を付与
GRANT EXECUTE ON SCHEMA::report TO ReportRole;

-- 管理系スキーマは管理者ロールのみ
GRANT EXECUTE ON SCHEMA::admin  TO AdminRole;
```

---

## 6. 機密データの保護

### Always Encrypted

クライアント側でデータを暗号化し、SQL Server には暗号化された状態でデータが届く。DBA も平文を見られない。

```sql
-- Always Encrypted 列の定義例（SSMS の列暗号化ウィザードで設定するのが一般的）
CREATE TABLE dbo.Users
(
    Id           INT           IDENTITY PRIMARY KEY,
    Email        NVARCHAR(256) COLLATE Latin1_General_BIN2
                               ENCRYPTED WITH
                               (
                                   COLUMN_ENCRYPTION_KEY = MyColumnKey,
                                   ENCRYPTION_TYPE = Deterministic,
                                   ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256'
                               ),
    PasswordHash NVARCHAR(256)
);
```

### Dynamic Data Masking

権限のないユーザーには実データの代わりにマスクされた値を返す。

```sql
-- メールアドレスのマスク設定
ALTER TABLE dbo.Users
ALTER COLUMN Email ADD MASKED WITH (FUNCTION = 'email()');

-- カード番号（最後4桁だけ表示）
ALTER TABLE dbo.CreditCards
ALTER COLUMN CardNumber ADD MASKED WITH (FUNCTION = 'partial(0,"****-****-****-",4)');

-- マスクを解除できるユーザーに UNMASK 権限を付与
GRANT UNMASK ON dbo.Users TO AdminUser;
```

---

## 7. SQLインジェクション防止

（詳細は [07_dynamic_sql.md](07_dynamic_sql.md) を参照）

ストアドプロシージャはそれ自体が SQL インジェクション対策になります。

```sql
-- ✅ プロシージャ経由のパラメータ化クエリはインジェクションを防ぐ
EXEC dbo.usp_GetTodoById @TodoId = 1;

-- ❌ 動的 SQL 内で直接連結するのは危険（07_dynamic_sql.md 参照）
```

---

## 8. 権限確認クエリ集

```sql
-- 現在のユーザーの権限を確認
SELECT * FROM fn_my_permissions(NULL, 'DATABASE');

-- 特定オブジェクトの権限一覧
SELECT
    pr.name            AS PrincipalName,
    pr.type_desc       AS PrincipalType,
    pe.permission_name AS Permission,
    pe.state_desc      AS State
FROM  sys.database_permissions pe
INNER JOIN sys.database_principals  pr ON pr.principal_id = pe.grantee_principal_id
INNER JOIN sys.objects              ob ON ob.object_id    = pe.major_id
WHERE ob.name = 'usp_Todos_BulkUpdate';

-- ロールのメンバー一覧
SELECT
    r.name  AS RoleName,
    m.name  AS MemberName
FROM  sys.database_role_members rm
INNER JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
INNER JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
ORDER BY r.name, m.name;

-- プロシージャの EXECUTE AS 設定確認
SELECT name, execute_as_principal_id, OBJECT_DEFINITION(object_id) AS Definition
FROM  sys.procedures
WHERE name LIKE 'usp_%';
```
