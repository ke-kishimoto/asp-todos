# dotnet-template — E2E テストプロジェクト

## 概要

このプロジェクトは、**Gauge** を使用した E2E（エンドツーエンド）テストのテンプレートです。  
Web UI テスト・Web API テスト・データベース操作をひとつのプロジェクトで実現します。

| 用途 | 使用ライブラリ |
|---|---|
| テストフレームワーク | [Gauge](https://gauge.org/) + Gauge.CSharp.Lib |
| ブラウザ自動操作 | [Microsoft Playwright](https://playwright.dev/dotnet/) |
| Web API テスト | [RestSharp](https://restsharp.dev/) |
| DB 接続 (SQL Server) | Microsoft.Data.SqlClient |
| CSV 読み書き | CsvHelper |
| アサーション | Shouldly |
| ターゲット フレームワーク | .NET 10.0 |

---

## ディレクトリ構成

```
├── specs/                    # Gauge スペックファイル（テストシナリオ）
│   └── todos/
│       ├── todo-api.spec     # Web API テスト
│       ├── todo-list.spec    # Todo 一覧画面テスト
│       ├── todo-create.spec  # Todo 作成画面テスト
│       ├── todo-detail.spec  # Todo 詳細画面テスト
│       └── todo-stored.spec  # ストアドプロシージャテスト
├── steps/                    # ステップ実装（Gauge ステップ定義）
│   ├── WebStepImplementation.cs     # Web UI 操作ステップ
│   ├── WebApiStepImplementation.cs  # Web API テストステップ
│   └── DbStepImplementation.cs      # DB 操作ステップ
├── hooks/
│   └── SetupAndTeardown.cs   # シナリオ前後の Playwright ブラウザ管理
├── src/
│   ├── Config/
│   │   ├── PlaywrightConfig.cs  # Playwright 設定読み込み
│   │   └── DbConfig.cs          # DB 接続設定読み込み
│   └── Helpers/
│       └── DatabaseHelper.cs    # DB 操作ユーティリティ（SQL / CSV / BulkInsert）
├── fixtures/
│   └── todos/
│       ├── seed/             # テスト投入用データ（CSV / SQL）
│       └── expected/         # 期待値データ（CSV / JSON）
├── env/
│   ├── default/              # デフォルト設定（リポジトリ管理）
│   │   ├── default.properties
│   │   ├── dotnet.properties
│   │   ├── web.properties
│   │   └── db.properties
│   └── local/                # ローカル上書き設定（git 管理外）
│       ├── default.properties
│       ├── dotnet.properties
│       └── web.properties
├── reports/                  # テスト実行後レポート（HTML）
├── logs/                     # 実行ログ
└── dotnet-template.csproj
```

---

## 前提条件

以下のツールが事前にインストールされている必要があります。

| ツール | バージョン | 備考 |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 以上 | |
| [Gauge](https://gauge.org/get-started/) | 最新版 | `choco install gauge` または公式インストーラー |
| Gauge C# プラグイン | 最新版 | `gauge install csharp` |
| Gauge HTML レポートプラグイン | 最新版 | `gauge install html-report` |
| SQL Server | 2019 以上 | ローカルまたは Docker |

### インストール確認

```powershell
dotnet --version      # 10.0.x
gauge version         # Gauge バージョン確認
gauge install csharp  # C# プラグイン（未インストールの場合）
gauge install html-report  # HTML レポートプラグイン（未インストールの場合）
```

---

## セットアップ

### 1. リポジトリのクローン

```powershell
git clone <repository-url>
cd csharp-e2e
```

### 2. NuGet パッケージの復元

```powershell
dotnet restore
```

### 3. Playwright ブラウザのインストール

```powershell
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install
```

> リリースビルドを使用する場合は `bin/release/net10.0/playwright.ps1` を使用してください。

### 4. ローカル設定ファイルの作成

`env/local/` フォルダのファイルを環境に合わせて編集します。

#### `env/local/web.properties` — Playwright・URL 設定

```properties
# テスト対象アプリの URL
base_url = http://localhost:5000

# ブラウザ: chromium / firefox / webkit
browser = chromium

# ヘッドレスモード: true / false
headless = false
```

#### `env/local/db.properties`（存在しない場合は新規作成）

```properties
# SQL Server 接続文字列（環境に合わせて変更）
# Windows 認証の場合
db_connection_string = Server=localhost;Database=MyTodoAppDb;Integrated Security=true;TrustServerCertificate=true;

# SQL Server 認証の場合
# db_connection_string = Server=localhost,1433;Database=MyTodoAppDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;

# クエリタイムアウト（秒）
db_command_timeout = 30
```

> `env/local/` 配下のファイルは git 管理対象外です。機密情報（パスワード等）を安全に保管できます。

### 5. テスト対象アプリケーションの起動

テスト実行前に、テスト対象の Web アプリケーションを起動してください。

---

## テストの実行

### 全スペックを実行

```powershell
gauge run specs/
```

### 特定のスペックファイルを実行

```powershell
gauge run specs/todos/todo-list.spec
```

### タグを指定して実行

```powershell
gauge run --tags "smoke" specs/
```

### 並列実行

```powershell
gauge run --parallel specs/
```

---

## テストレポートの確認

テスト実行後、`reports/html-report/index.html` をブラウザで開くと HTML レポートを確認できます。

```powershell
Start-Process reports/html-report/index.html
```

---

## 実装済みステップ一覧

### Web UI ステップ（WebStepImplementation）

| ステップ | 説明 |
|---|---|
| `URL <url> を開く` | 指定 URL に遷移する |
| `URL <url> に遷移している` | 現在の URL を検証する |
| `要素 <selector> に <text> が表示されている` | 要素のテキストを検証する |
| `要素 <selector> が <count> 件表示されている` | 要素の件数を検証する |
| `要素 <selector> に <text> と入力する` | テキストを入力する |
| `要素 <selector> をクリックする` | 要素をクリックする |
| `リンク <text> をクリックする` | リンクテキストでクリックする |
| `見出し <text> が表示されている` | h1 の見出しを検証する |
| `テーブル要素 <selector> の <row> 行目の <column> 列の値が <value> である` | テーブルのセル値を検証する |
| `テーブル要素 <selector> の内容が <table> と一致している` | テーブル全体の内容を検証する |

### Web API ステップ（WebApiStepImplementation）

| ステップ | 説明 |
|---|---|
| `<url> にGETリクエストを送る` | GET リクエストを送信する |
| `ステータスコードが <code> である` | HTTP ステータスコードを検証する |
| `レスポンスのJSON配列の長さが <length> である` | JSON 配列の要素数を検証する |
| `レスポンスのJSONが <json> と一致している` | レスポンス JSON を検証する |

### DB ステップ（DbStepImplementation）

| ステップ | 説明 |
|---|---|
| `テーブル <tableName> のデータを全て削除する` | テーブルを TRUNCATE する |
| `SQL <sql> を実行する` | SQL 文字列を実行する（ファイル参照可） |
| `CSVファイル <csvPath> の内容をテーブル <tableName> に投入する` | CSV ファイルからデータを投入する |
| `テーブル <tableName> に <csv> の内容を投入する` | インラインテーブルからデータを投入する |
| `テーブル <tableName> の内容が <table> と一致している` | テーブルの内容を検証する |
| `テーブル <tableName> の条件 <condition> のレコードの内容が以下の通りである <table>` | 条件付きでテーブルの内容を検証する |
| `ストアドプロシージャ <name> を実行する` | 引数なしでストアドを実行する |
| `ストアドプロシージャ <name> を以下の引数で実行する <table>` | 引数付きでストアドを実行する |

---

## 設定ファイルの詳細

### `env/default/web.properties`

| キー | デフォルト値 | 説明 |
|---|---|---|
| `browser` | `chromium` | 使用ブラウザ（chromium / firefox / webkit） |
| `headless` | `false` | ヘッドレス実行 |
| `slow_mo` | `0` | 操作間の待機時間（ms） |
| `viewport_width` | `1280` | ビューポート幅（px） |
| `viewport_height` | `720` | ビューポート高さ（px） |
| `base_url` | （空） | テスト対象アプリの URL |

### `env/default/db.properties`

| キー | デフォルト値 | 説明 |
|---|---|---|
| `db_connection_string` | `Server=localhost,...` | SQL Server 接続文字列 |
| `db_command_timeout` | `30` | SQL コマンドタイムアウト（秒） |

### `env/default/dotnet.properties`

| キー | デフォルト値 | 説明 |
|---|---|---|
| `GAUGE_CSHARP_PROJECT_FILE` | `dotnet-template.csproj` | プロジェクトファイルパス |
| `GAUGE_CSHARP_PROJECT_CONFIG` | `release` | ビルド構成 |
| `gauge_clear_state_level` | `scenario` | 状態クリアのタイミング |

---

## トラブルシューティング

### Playwright のブラウザが起動しない

```powershell
# ブラウザを再インストール
pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps
```

### DB 接続エラー

- `env/local/db.properties` の接続文字列を確認してください。
- SQL Server が起動しているか確認してください。
- `TrustServerCertificate=True` が設定されているか確認してください。

### Gauge のスペックが見つからない

```powershell
# Gauge のインストール状態を確認
gauge version
gauge install csharp
gauge install html-report
```
