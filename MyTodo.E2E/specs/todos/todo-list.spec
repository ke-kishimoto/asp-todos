# Todo list
tags: browser

## 一覧が表示されている CsvPath
* テーブル "todos" のデータを全て削除する
* テーブル "todos" に <table:fixtures/todos/seed/csv/todos.csv> の内容を投入する
* URL "mvc/todos" を開く
* 要素 "h1" に "Todos" が表示されている
* 要素 "tbody tr" が "3" 件表示されている
* テーブル要素 "table" の内容が <table:fixtures/todos/expected/csv/todo-list.csv> と一致している

## 一覧が表示されている InlineTable
* テーブル "todos" のデータを全て削除する
* テーブル "todos" に以下の内容を投入する

|id|title|done|created_at|
|--|-----|----|---------|
|1 |Sample Todo|false|2024-06-01T10:00:00Z|
|2 |Another Todo|true|2024-06-02T12:00:00Z|
|3 |Third Todo|false|2024-06-03T14:00:00Z|

* URL "mvc/todos" を開く
* 要素 "h1" に "Todos" が表示されている
* 要素 "tbody tr" が "3" 件表示されている
* テーブル要素 "table" の内容が以下の通りである

|row|ID|Title       |Done |Created           |
|---|--|------------|-----|------------------|
|1  |3 |Third Todo  |False|2024/06/04 8:00:00|
|2  |2 |Another Todo|True |2024/06/03 6:00:00|
|3  |1 |Sample Todo |False|2024/06/02 4:00:00|


## 一覧が表示されている DynamicParameter

データの行数分実行される

|row|ID|Title       |Done |Created           |
|---|--|------------|-----|------------------|
|1  |3 |Third Todo  |False|2024/06/04 8:00:00|
|2  |2 |Another Todo|True |2024/06/03 6:00:00|
|3  |1 |Sample Todo |False|2024/06/02 4:00:00|

* テーブル "todos" のデータを全て削除する
* CSVファイル "fixtures/todos/seed/csv/todos.csv" の内容をテーブル "todos" に投入する
* URL "mvc/todos" を開く
* 要素 "h1" に "Todos" が表示されている
* 要素 "tbody tr" が "3" 件表示されている
* テーブル要素 "table" の <row> 行目の "ID" 列の値が <ID> である
* テーブル要素 "table" の <row> 行目の "Title" 列の値が <Title> である
* テーブル要素 "table" の <row> 行目の "Done" 列の値が <Done> である
* テーブル要素 "table" の <row> 行目の "Created" 列の値が <Created> である

## 新規作成画面に遷移する
* URL "mvc/todos" を開く
* リンク "Create New" をクリックする
* URL "mvc/todos/create" に遷移している
* 要素 "h1" に "Create Todo" が表示されている

