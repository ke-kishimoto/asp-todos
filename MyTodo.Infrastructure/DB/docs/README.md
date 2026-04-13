# SQL Server ストアドプロシージャ 総合リファレンス

このフォルダは SQL Server のストアドプロシージャに関する体系的な参考資料です。  
ここを読めば、業務システムで必要となるストアドプロシージャのほぼすべての実装パターンを習得できます。

---

## ドキュメント一覧

| ファイル | 内容 |
|---|---|
| [01_basics.md](01_basics.md) | 基本構文（作成・変更・削除・実行・確認） |
| [02_parameters.md](02_parameters.md) | パラメータ（入力・出力・テーブル値・省略値） |
| [03_flow_control.md](03_flow_control.md) | 制御フロー（IF/ELSE・WHILE・CASE） |
| [04_error_handling.md](04_error_handling.md) | エラーハンドリング（TRY/CATCH・THROW・RAISERROR） |
| [05_transactions.md](05_transactions.md) | トランザクション（BEGIN TRAN・COMMIT・ROLLBACK・セーブポイント） |
| [06_temp_tables_cte.md](06_temp_tables_cte.md) | 一時テーブル・テーブル変数・CTE |
| [07_dynamic_sql.md](07_dynamic_sql.md) | 動的 SQL（sp_executesql・SQLインジェクション対策） |
| [08_cursors.md](08_cursors.md) | カーソル（行単位処理・カーソル種別） |
| [09_security.md](09_security.md) | セキュリティ・権限（EXECUTE AS・GRANT/DENY/REVOKE） |
| [10_performance.md](10_performance.md) | パフォーマンス最適化（統計・プランキャッシュ・ヒント） |

---

## このプロジェクトに登録されているストアドプロシージャ

| ファイル | プロシージャ名 | 説明 |
|---|---|---|
| [../usp_Todos_BulkUpdate.sql](../usp_Todos_BulkUpdate.sql) | `dbo.ups_Todos_BulkUpdate` | 全 Todo を Done = True に一括更新 |
| [../usp_Todos_BulkUpdate_By_Date.sql](../usp_Todos_BulkUpdate_By_Date.sql) | `usp_Todos_BulkUpdate_By_Date` | 指定期間内の Todo を Done = True に一括更新 |

---

## 命名規約

このプロジェクトでは以下の命名規約を採用しています。

| 種類 | プレフィックス | 例 |
|---|---|---|
| ストアドプロシージャ | `usp_` | `usp_Todos_BulkUpdate` |
| ビュー | `vw_` | `vw_TodoSummary` |
| スカラー関数 | `ufn_` | `ufn_GetTodoCount` |
| テーブル値関数 | `uft_` | `uft_GetTodosByCategory` |

---

## 学習ロードマップ

```
1. 基本構文を覚える           → 01_basics.md
2. パラメータを使いこなす     → 02_parameters.md
3. 条件分岐・ループを学ぶ     → 03_flow_control.md
4. エラー処理を実装する       → 04_error_handling.md
5. トランザクションで一貫性を守る → 05_transactions.md
6. 中間データを扱う           → 06_temp_tables_cte.md
7. 動的SQLで柔軟性を高める    → 07_dynamic_sql.md
8. 行単位処理（カーソル）     → 08_cursors.md
9. セキュリティを設定する     → 09_security.md
10. パフォーマンスを最適化する → 10_performance.md
```
