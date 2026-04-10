# Todo Create
* テーブル "todos" のデータを全て削除する

## Todoが作成できる CSV確認ver
* URL "mvc/todos/create" を開く
* 要素 "input[name='title']" に "New Todo" と入力する
* 要素 "button[type='submit']" をクリックする
* URL "mvc/todos" に遷移している
* 要素 "tbody tr" が "1" 件表示されている
* テーブル "todos" の内容が <table:fixtures/todos/expected/csv/todo-created.csv> と一致している

## Todoが作成できる Table確認ver1
* URL "mvc/todos/create" を開く
* 要素 "input[name='title']" に "New Todo" と入力する
* 要素 "button[type='submit']" をクリックする
* URL "mvc/todos" に遷移している
* 要素 "tbody tr" が "1" 件表示されている
* テーブル "todos" の内容が以下の通りである

|id|title|done|
|--|-----|----|
|1 |New Todo|False|

## Todoが作成できる Table確認ver2
* URL "mvc/todos/create" を開く
* 要素 "input[name='title']" に "New Todo" と入力する
* 要素 "button[type='submit']" をクリックする
* URL "mvc/todos" に遷移している
* テーブル "todos" の条件 "id = 1" のレコードの内容が以下の通りである

|Column|Value   |
|------|--------|
|id    |1       |
|title |New Todo|
|done  |False   |
