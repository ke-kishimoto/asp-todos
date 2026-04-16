# Gauge Spec Template

Use this structure for a straightforward function scenario.

```md
# <FunctionName> test

## <FunctionName> no tesuto
* "postgres" no table "<reference-table-1>" no data o subete sakujo suru
* "postgres" no table "<reference-table-2>" no data o subete sakujo suru
* "postgres" no table "<updated-table-1>" no data o subete sakujo suru
* "postgres" no table "<reference-table-1>" ni <table:fixtures/<function-name>/seed/<reference-table-1>.csv> no naiyo o tonyu suru
* "postgres" no table "<reference-table-2>" ni <table:fixtures/<function-name>/seed/<reference-table-2>.csv> no naiyo o tonyu suru
* "postgres" deSQL "SELECT <function-name>(<parameter-values>)" o jikko suru
* "postgres" no table "<updated-table-1>" no naiyo ga ika no toori de aru <table:fixtures/<function-name>/expected/<updated-table-1>.csv>
```

Replace the romaji placeholders with natural Japanese in the generated spec file.

Prefer this exact Japanese step sequence for the final output when applicable:

```md
## <FunctionName>のテスト
* "postgres" のテーブル "<reference-table-1>" のデータを全て削除する
* "postgres" のテーブル "<reference-table-2>" のデータを全て削除する
* "postgres" のテーブル "<updated-table-1>" のデータを全て削除する
* "postgres" のテーブル "<reference-table-1>" に以下の内容を投入する <table:fixtures/<function-name>/seed/<reference-table-1>.csv>
* "postgres" のテーブル "<reference-table-2>" に以下の内容を投入する <table:fixtures/<function-name>/seed/<reference-table-2>.csv>
* "postgres" でSQL "SELECT <function-name>(<parameter-values>)" を実行する
* "postgres" のテーブル "<updated-table-1>" の内容が以下の通りである <table:fixtures/<function-name>/expected/<updated-table-1>.csv>
```

If multiple updated tables matter, add one assertion step per table.
If setup requires SQL rather than CSV, use the SQL execution step and reference the `.sql` file explicitly.
