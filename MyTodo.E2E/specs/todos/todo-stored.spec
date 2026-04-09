# Todo Stored Procedure

## ストアドでTodoリストを更新できる
* テーブル "todos" のデータを全て削除する
* テーブル "todos" に <table:fixtures/todos/seed/csv/todos-all-false.csv> の内容を投入する
* ストアドプロシージャ "usp_Todos_BulkUpdate" を実行する
* テーブル "todos" の内容が <table:fixtures/todos/expected/csv/todo-updated-by-procedure.csv> と一致している

## 引数有りのストアドでTodoリストを更新できる

日付の比較時には日本時間になっている

* テーブル "todos" のデータを全て削除する
* テーブル "todos" に <table:fixtures/todos/seed/csv/todos-all-false.csv> の内容を投入する
* ストアドプロシージャ "usp_Todos_BulkUpdate_By_Date" を以下の引数で実行する

| Parameter | Value |
| --- | --- |
| @StartDate | 2024-06-02T00:00:00Z |
| @EndDate | 2024-06-04T23:59:59Z |

* テーブル "todos" の内容が <table:fixtures/todos/expected/csv/todo-updated-by-procedure-params.csv> と一致している