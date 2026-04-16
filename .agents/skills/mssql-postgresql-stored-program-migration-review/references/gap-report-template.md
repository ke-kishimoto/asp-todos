# Gap Report Template

Use this structure for the generated Markdown report.

```md
# <MSSQL object name> migration gap report

## 対象
- MSSQL: `<mssql-object-name>`
- PostgreSQL: `<postgresql-object-name>`

## 判定
<全体として、PostgreSQL と比較して実行結果に影響しうる差異があるかを短くまとめる。>

## 差異一覧
| No | 観点 | MSSQL | PostgreSQL | 影響 | 修正案 |
| --- | --- | --- | --- | --- | --- |
| 1 | フィルタ条件 | <current behavior> | <expected behavior> | <possible runtime impact> | <MSSQL-side proposal> |

差異がない場合は、`実行結果に影響しうる差異は確認できませんでした。` と記載する。

## 補足
- <前提、未確認事項、対応保留事項>
```

Focus on behavior, not syntax.
Keep proposals specific enough that a follow-up implementation task can use them directly.
