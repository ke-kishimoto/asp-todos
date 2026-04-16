# Japanese Output Template

Use this structure for the generated Markdown document.

```md
# <オブジェクト名>

## 概要
<対象のストアドプログラムが何を行うかを 2-4 文で要約する。>

## 種別
- Procedure または Function

## パラメータ
| 名前 | モード | 型 | デフォルト値 | 説明 |
| --- | --- | --- | --- | --- |
| <parameter_name> | <IN/OUT/INOUT/VARIADIC または空欄> | <data_type> | <default or 該当なし> | <用途> |

パラメータが存在しない場合は `該当なし` と記載する。

## 戻り値
<関数の戻り値、またはプロシージャのため該当なし。必要に応じて OUT パラメータの扱いも補足する。>

## 参照テーブル
| テーブル名 | 主な用途 | レコード取得条件 |
| --- | --- | --- |
| <schema.table> | <参照目的> | <条件 or 複雑なため省略> |

参照テーブルがない場合は `該当なし` と記載する。

## 更新対象テーブル
| テーブル名 | 更新種別 | 対象カラム | 更新条件・キー | 内容 |
| --- | --- | --- | --- | --- |
| <schema.table> | <INSERT/UPDATE/DELETE/MERGE> | <columns> | <predicate> | <what changes> |

更新対象テーブルがない場合は `該当なし` と記載する。

## 全体の処理概要
1. <主要な処理ステップ>
2. <主要な処理ステップ>
3. <主要な処理ステップ>

## 補足
- <トランザクション、例外処理、動的 SQL、性能観点など>
```

Prefer concise explanations. Keep exact identifiers such as parameter names, table names, and column names as they appear in SQL whenever practical.
