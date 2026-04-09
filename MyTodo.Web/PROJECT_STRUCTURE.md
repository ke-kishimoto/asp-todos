# MyTodo.Web プロジェクト構造解説

## 使用技術の概要

このプロジェクトには **2つのWebフロントエンド技術** と **1つのAPIレイヤー** が共存しています。

| 技術 | URL パターン | 特徴 |
|---|---|---|
| **MVC** | `/` `/mvc/todos/*` `/mvc/items/*` | Controller + View の分離設計。`Views/` フォルダのテンプレートを使用 |
| **Blazor Server** | `/blazor/todos` `/blazor/orders` | コンポーネントベース。WebSocket で差分更新する SPA 的 UI |
| **Web API** | `/api/todos/*` | JSON を返す REST API |

> **注意**: Razor Pages は削除済みです。`Pages/` ディレクトリは存在しません。

---

## フォルダ・ファイル対応表

```
MyTodo.Web/
├── Program.cs                          ← [共通] エントリポイント・DI設定
├── appsettings.json                    ← [共通] アプリ設定（DB接続文字列など）
├── appsettings.Development.json        ← [共通] 開発環境専用の設定（本番設定を上書き）
│
├── Controllers/                        ★ MVC / Web API レイヤー
│   ├── HomeController.cs               ← [MVC] / (ルート) を処理するController
│   ├── TodosController.cs              ← [MVC] /mvc/todos/* のリクエストを処理するController
│   ├── ItemsController.cs              ← [MVC] /mvc/items/* のリクエストを処理するController（認証必須）
│   ├── TodosApiController.cs           ← [Web API] /api/todos/* のリクエストを処理するController
│   ├── BlazorTodosController.cs        ← [MVC] /blazor/todos を受けて Blazor ホスト View を返す
│   └── BlazorOrdersController.cs       ← [MVC] /blazor/orders を受けて Blazor ホスト View を返す
│
├── Models/                             ★ 共通 ViewModel
│   ├── ErrorViewModel.cs               ← [MVC] エラーページ用モデル
│   ├── TodoItemViewModel.cs            ← Todo の表示データ形状の定義
│   ├── ItemViewModel.cs                ← Item の表示データ形状の定義
│   └── ItemCreateViewModel.cs          ← Item 作成フォーム用モデル
│
│   ┌──────────────────────────────────────────────────────┐
│   │  📄 MVC（Views/ フォルダ）                            │
│   │  Controller が処理し、結果を Views/ のテンプレートで描画 │
│   │  .cshtml とペアになる .cs ファイルは存在しない          │
│   │  （ロジックはすべて Controllers/ に書く）              │
│   └──────────────────────────────────────────────────────┘
│
├── Views/
│   ├── _ViewImports.cshtml             ← [MVC] Views/ 全体に適用される @using/@addTagHelper
│   ├── _ViewStart.cshtml               ← [MVC] Views/ 全体に適用されるデフォルトレイアウト指定
│   │
│   ├── Shared/
│   │   └── _Layout.cshtml              ← [MVC + Blazor Host] 共通HTMLレイアウト（ヘッダー・フッター・<body>枠）
│   │                                      Blazor 用に <base href="/"> と <script type="importmap"> が含まれている
│   │
│   ├── Home/
│   │   ├── Index.cshtml                ← HomeController.Index() の描画テンプレート（/）
│   │   └── Error.cshtml                ← HomeController.Error() の描画テンプレート（エラーページ）
│   │
│   ├── Todos/                          ← [MVC] TodosController の各アクション対応テンプレート
│   │   ├── Index.cshtml                ← Index() アクションの描画テンプレート
│   │   ├── Create.cshtml               ← Create() アクションの描画テンプレート
│   │   ├── Edit.cshtml                 ← Edit() アクションの描画テンプレート
│   │   ├── Delete.cshtml               ← Delete() アクションの描画テンプレート
│   │   └── Details.cshtml              ← Details() アクションの描画テンプレート
│   │
│   ├── Items/                          ← [MVC] ItemsController の各アクション対応テンプレート
│   │   ├── Index.cshtml                ← Index() アクションの描画テンプレート
│   │   └── Create.cshtml               ← Create() アクションの描画テンプレート
│   │
│   ├── BlazorTodos/
│   │   └── Index.cshtml                ← BlazorTodosController.Index() から返される Blazor ホスト View
│   │                                      <component type="typeof(TodoList)"> を置くだけ。
│   │                                      HTTP リクエストはここで受け取るが、その後の画面操作は
│   │                                      Blazor コンポーネントが WebSocket で処理する。
│   └── BlazorOrders/
│       └── Index.cshtml                ← BlazorOrdersController.Index() から返される Blazor ホスト View
│
│
│   ┌──────────────────────────────────────────────────────┐
│   │  📄 BLAZOR SERVER（BlazorComponents/ フォルダ）        │
│   │  .razor ファイル = HTML テンプレート + C# ロジックの一体型 │
│   │  WebSocket 経由でサーバーと常時接続し、差分のみ更新する  │
│   └──────────────────────────────────────────────────────┘
│
├── BlazorComponents/
│   ├── _Imports.razor                  ← [Blazor] 全 .razor に適用される @using/@inject の共通定義
│   └── Todos/
│       ├── TodoList.razor              ← [Blazor] 【親コンポーネント】Todo一覧 + CRUD 画面全体
│       │                                  状態（表示データ・UIの開閉状態）をここで一元管理する
│       │                                  URL: /blazor/todos で表示される実体
│       ├── TodoFormPanel.razor         ← [Blazor] 【子コンポーネント】新規作成/編集用サイドパネル
│       │                                  TodoList から [Parameter] でデータを受け取り、
│       │                                  保存完了時は EventCallback で TodoList に通知する
│       └── TodoDeleteModal.razor       ← [Blazor] 【子コンポーネント】削除確認モーダル
│                                          TodoList から削除対象アイテムを受け取り、
│                                          OK/Cancel の結果を EventCallback で通知する
│   └── Orders/
│       └── Order.razor                 ← [Blazor] 商品注文フォーム。ItemCode で商品を検索し行追加できる
│
│
├── Auth/
│   └── FakeAuthHandler.cs              ← [開発用] 開発環境での認証バイパス用カスタムスキーム
│
├── Properties/
│   └── launchSettings.json             ← [共通] 開発時の起動設定（ポート番号・環境変数など）
│
└── wwwroot/                            ← [共通] 静的ファイル（JS/CSS/画像）の公開フォルダ
    ├── css/site.css                    ← アプリ共通のカスタムCSS
    ├── js/site.js                      ← アプリ共通のカスタムJS
    └── lib/                            ← Bootstrap, jQuery など サードパーティライブラリ
```

---

## リクエストの流れ

```
【MVC】
  ブラウザ → GET /mvc/todos
    → Controllers/TodosController.cs の Index() 実行
    → Application 層でDBからデータ取得
    → Views/Todos/Index.cshtml で HTML レンダリング
    → レスポンスを返す（以降は次のリクエスト待ち）

【Blazor Server】
  ブラウザ → GET /blazor/todos（初回のみ HTTP）
    → BlazorTodosController.Index() → Views/BlazorTodos/Index.cshtml を返す
    → blazor.server.js が実行され、WebSocket(SignalR) で接続確立
    → BlazorComponents/Todos/TodoList.razor が描画される
    → 以降のボタン操作/検索/CRUD は HTTP リクエストなし、WebSocket で差分更新

【Web API】
  ブラウザ/外部クライアント → GET /api/todos
    → Controllers/TodosApiController.cs の GetAll() 実行
    → Application 層でDBからデータ取得
    → JSON をレスポンスとして返す（HTMLなし）
```

---

## ファイルの役割サマリー

| 技術 | テンプレート | ロジック (C#) | 特記事項 |
|---|---|---|---|
| **MVC** | `Views/**/*.cshtml` | `Controllers/*.cs` (Controller) | テンプレートとロジックは別フォルダ |
| **Blazor Server** | `BlazorComponents/**/*.razor` | 同じ `.razor` ファイルの `@code {}` | HTML と C# が1ファイルに同居 |
| **Web API** | なし（JSON を返す） | `Controllers/*.cs` (ControllerBase) | `[ApiController]` 属性で識別 |

---

## Blazor のホスティング構造

Blazor 画面は **MVC Controller が HTTP 入口となり、View が Blazor コンポーネントをホスト** します。

```
1. ブラウザが /blazor/todos に HTTP GET
        ↓
2. BlazorTodosController.Index() が実行される
        ↓
3. Views/BlazorTodos/Index.cshtml が MVC View としてレンダリング
   （Views/Shared/_Layout.cshtml でヘッダー・フッターを含む <html>〜</body> を生成）
        ↓
4. View 内の <component type="typeof(TodoList)" render-mode="ServerPrerendered" />
   が TodoList.razor を HTML に展開（初期表示を速くするため）
        ↓
5. ブラウザが blazor.server.js を実行
   → WebSocket (SignalR) 接続確立
        ↓
6. 以降はすべて Blazor Server が担当
   HTTP リクエストは発生せず、ボタン操作などは WebSocket で処理
```

このため `Views/Shared/_Layout.cshtml` には Blazor 動作に必要な `<base href="/">` と
`<script type="importmap">` が含まれています。
