# アーキテクチャ・技術選定根拠

> **このドキュメントの目的**  
> `MyTodo` ソリューションを ASP.NET Core 新規プロジェクトのテンプレートとして活用する際に、
> 採用しているアーキテクチャ・設計思想・技術スタックのその**選定理由**を明確に説明するための資料です。  
> PM・非エンジニアが稟議資料として参照できる概要から、技術リーダー・開発者が設計判断の根拠を確認できる詳細まで、多層的に記述しています。

---

## 目次

1. [エグゼクティブサマリー](#1-エグゼクティブサマリー)
2. [アーキテクチャ全体像](#2-アーキテクチャ全体像)
3. [クリーンアーキテクチャ](#3-クリーンアーキテクチャ)
4. [CQRS（コマンド・クエリ責務分離）](#4-cqrsコマンドクエリ責務分離)
5. [プレゼンテーション層の使い分け（MVC / Blazor Server / Web API）](#5-プレゼンテーション層の使い分けmvc--blazor-server--web-api)
6. [CSS 設計（ITCSS + Every Layout）](#6-css-設計itcss--every-layout)
7. [テスト戦略（xUnit + Gauge + Playwright）](#7-テスト戦略xunit--gauge--playwright)
8. [技術スタック一覧](#8-技術スタック一覧)
9. [このテンプレートの適用判断基準](#9-このテンプレートの適用判断基準)

---

## 1. エグゼクティブサマリー

### このテンプレートが解決する課題

| 課題 | どう解決するか |
|---|---|
| 機能追加のたびにどこを変更すればよいかわからない | **クリーンアーキテクチャ**が各層の責務を明確に区分する |
| データ取得ロジックと書き込みロジックが混在して保守困難 | **CQRS** が読み書きのモデルとコードを完全に分離する |
| 画面の種類（通常遷移・リアルタイム・API）によって最適な技術が異なる | **MVC / Blazor Server / Web API** を目的別に使い分ける構造を示す |
| CSS が肥大化してスタイルが衝突・管理不能になる | **ITCSS + Every Layout** でスタイルを層構造で管理する |
| テストが開発者にしか読めない・仕様と乖離する | **Gauge** で日本語 Markdown の仕様書とテストを一体化させる |

### 採用技術の一言まとめ

```
クリーンアーキテクチャ    = "変更しやすいコードの骨格"
CQRS                      = "読み書きの最適化を独立させる設計"
MVC                        = "標準的な画面遷移の王道"
Blazor Server             = "C# でリアルタイム UI を書く"
Web API                   = "どんなクライアントとも繋がれる出口"
ITCSS + Every Layout      = "壊れにくい CSS の作り方"
Gauge + Playwright        = "仕様書がそのままE2Eテストになる"
```

### ステークホルダーへの一言

- **PM・非エンジニア向け**: このテンプレートに従って開発すると、チームメンバーが入れ替わっても「どこに何があるか」が一目でわかる構造になります。新機能追加・バグ修正のコストを長期的に低く保てます。
- **技術リーダー向け**: 各技術の採用理由とトレードオフを明示しています。チームの文脈に合わせた取捨選択の判断材料としてください。
- **開発者向け**: コーディングパターンの詳細は [`.github/skills/SKILL.md`](../.github/skills/SKILL.md) を参照してください。

---

## 2. アーキテクチャ全体像

### プロジェクト構成

```
dotnetSample.sln
├── MyTodo.Domain/          ドメイン層（ビジネスルール・エンティティ・値オブジェクト）
├── MyTodo.Infrastructure/  インフラ層（EF Core・SQL Server・リポジトリ実装）
├── MyTodo.Application/     アプリケーション層（コマンドハンドラー・クエリサービス）
├── MyTodo.Web/             プレゼンテーション層（MVC / Blazor Server / Web API）
├── TodoApp.Tests/          単体テスト（xUnit）
└── MyTodo.E2E/             E2Eテスト（Gauge + Playwright）
```

### 依存関係図

```
┌─────────────────────────────────────────────────────────┐
│                  MyTodo.Web (UI / API)                  │
│         MVC Controller / Blazor / REST API              │
└──────────────────────────┬──────────────────────────────┘
                           │ 依存
┌──────────────────────────▼──────────────────────────────┐
│                  MyTodo.Application                     │
│    CommandHandlers / QueryService Interface             │
│    Repository Interface                                 │
└──────────────┬──────────────────────────▲───────────────┘
               │ 依存                     │ インターフェース経由で逆依存
┌──────────────▼────────────┐   ┌─────────┴───────────────┐
│       MyTodo.Domain       │◀──│  MyTodo.Infrastructure  │
│  TodoItem / Item          │   │  EF Core / SQL Server   │
│  値オブジェクト            │   │  Repository 実装        │
└───────────────────────────┘   └─────────────────────────┘
```

**依存の方向は常に「外側 → 内側」** です。Domain 層は他の層を一切参照しません。  
Infrastructure は Application が定義したインターフェースを実装することで依存を逆転させています（依存性逆転の原則）。

---

## 3. クリーンアーキテクチャ

### 採用理由

プロジェクトの初期段階では「とりあえず動く」コードが書けても、機能が増えると「どこに書けばいいかわからない」「修正すると別のところが壊れる」という問題が頻発します。クリーンアーキテクチャは、**変更が最も多いインフラ詳細（DBの種類・UIフレームワーク）から、変更が少ないビジネスロジックを守る**ための構造的解決策です。

### メリット

| メリット | 説明 |
|---|---|
| **テストが書きやすい** | Domain・Application 層が外部依存を持たないため、DB・HTTP なしでビジネスロジックを単体テスト可能 |
| **インフラを差し替えられる** | `ITodoRepository` のインターフェース経由にすることで、SQL Server → PostgreSQL や In-Memory に差し替えても上位層のコードを変更不要 |
| **責務が明確** | 「このクラスは何をする層か」が一目でわかり、コードレビューや新規参加者のオンボーディングが速い |
| **長期保守性** | 外部ライブラリのバージョンアップ・廃止の影響が Infrastructure 層に封じ込められる |
| **並行開発しやすい** | インターフェースが決まれば、Domain と Infrastructure を別担当者が並行実装できる |

### デメリット・トレードオフ

| デメリット | 対策・判断 |
|---|---|
| **初期の学習コスト** | 各層の責務と依存ルールへの理解が必要。チームへの研修・SKILL.md の整備で対処 |
| **小規模アプリへの過剰設計** | 数画面の社内ツール・PoC では恩恵より複雑さが上回ることがある。[適用判断基準](#9-このテンプレートの適用判断基準) を参照 |
| **ファイル数が増える** | 1 機能追加に複数のファイル（Command / Repository Interface / 実装）が必要。Visual Studio の IDE 支援・テンプレートで負荷を軽減 |
| **パフォーマンス調整が難しい場合がある** | 厳密な層分離により、DB の JOIN を最大限活用したい場合に QueryService 層で吸収しきれないことも。ReadModel への直接マッピングで対処 |

### このプロジェクトでの実践

```csharp
// Application 層：インターフェースのみに依存（Infrastructure を知らない）
public class CreateTodoCommandHandler
{
    private readonly ITodoRepository _repo;  // ← インターフェース
    public CreateTodoCommandHandler(ITodoRepository repo) => _repo = repo;
}

// Infrastructure 層：Application のインターフェースを実装
public class EfTodoRepository : ITodoRepository
{
    private readonly AppDbContext _db;
    public async Task<TodoItem> AddAsync(string title) { ... }
}
```

```csharp
// Domain 層：ビジネスロジックだけ。DB も HTTP も一切知らない
public record TodoItems(IEnumerable<TodoItem> Items)
{
    public TodoItems AllCompleted()
        => new(Items.Select(i => i with { IsCompleted = new TodoIsCompleted(true) }));
}
```

---

## 4. CQRS（コマンド・クエリ責務分離）

### 採用理由

一般的なリポジトリパターンだけでは、読み取り（一覧表示・検索）と書き込み（作成・更新・削除）のモデルが同じドメインオブジェクトを共有します。これは複数のテーブルを JOIN した読み取り専用 DTO を作りたいときやバリデーション付き書き込みモデルを扱うときに、**モデルが肥大化し、変更の影響範囲が広がる**原因になります。CQRS はこの問題を根本から解決します。

### 設計の分離

```
書き込み（Command）                    読み取り（Query）
─────────────────────────────────────────────────────
CreateTodoCommand                     ITodoQueryService
UpdateTodoCommand                     TodoReadModel (DTO)
DeleteTodoCommand
        │                                     │
CommandHandler                        Infrastructure の
（ITodoRepository 経由）               QueryService 実装
        │                                     │
  ドメインオブジェクト             DB を直接読む最適化クエリ
  （TodoItem record）              （EF Core Projection）
```

### メリット

| メリット | 説明 |
|---|---|
| **読み取りを自由に最適化できる** | ドメインモデルを経由せず、表示に最適化した `TodoReadModel` に直接プロジェクションできる |
| **書き込みのバリデーションが集約される** | `CommandHandler` にバリデーション・ビジネスルール適用を集中させられる |
| **変更の影響範囲が小さい** | 一覧画面のカラム追加は `ReadModel` と `QueryService` だけ変更すれば済む |
| **スケールアウト適性** | 将来的に読み取り DB と書き込み DB を分離（Read Replica）するアーキテクチャへの移行が容易 |
| **テストしやすい** | Command と Query が独立しているため、それぞれを単独でテスト可能 |

### デメリット・トレードオフ

| デメリット | 対策・判断 |
|---|---|
| **コードの二重管理** | 書き込み用のドメインモデルと読み取り用の ReadModel を個別に管理する必要がある。シンプルな CRUD では冗長に感じることも |
| **メッセージブローカー等は不要** | 本プロジェクトは同期 CQRS（イベント駆動・Event Sourcing は採用しない）。複雑性をシンプルに保つための意図的な選択 |
| **チームへの説明コスト** | CQRS を知らないメンバーにとっては「Command と Query を分ける意味」が直感的でない場合も。ドキュメント整備が重要 |

### このプロジェクトでの実践

```
MyTodo.Application/
  Commands/Todos/
    CreateTodoCommand.cs   ← Command record + Handler（書き込みのみ）
  Queries/Todos/
    ITodoQueryService.cs   ← 読み取りインターフェース
    TodoReadModel.cs       ← 表示専用 DTO（ドメインオブジェクトとは別）

MyTodo.Infrastructure/
  Queries/
    EfTodoQueryService.cs  ← EF Core で最適化プロジェクション実装
```

`TodoReadModel` はビューに必要なカラムを自由に定義でき、将来的に集計列・関連エンティティの埋め込みを追加しても `TodoItem` ドメインオブジェクトへの影響はありません。

---

## 5. プレゼンテーション層の使い分け（MVC / Blazor Server / Web API）

### 選択マトリクス

| 要件 | 採用技術 | 理由 |
|---|---|---|
| 通常の画面遷移・CRUD 操作 | **MVC** | サーバーサイドレンダリングで SEO 対応しやすく、フォーム送信の標準仕様に沿っている |
| 動的な行追加・リアルタイム更新・インタラクティブな UI | **Blazor Server** | C# のみで SPA 相当の UI を構築でき、JavaScript との二重管理が不要 |
| 外部クライアント（モバイルアプリ・他システム・SPA）向けデータ提供 | **Web API** | REST JSON で任意のクライアントと疎結合に連携できる |

### MVC

**採用理由**
- ASP.NET Core の標準的な UI パターン。学習リソースが豊富でチームの習熟コストが低い
- Razor View はサーバーサイドで HTML を生成するため、SEO・初期表示速度に有利
- フォームバリデーション・リダイレクト・PRG パターンを素直に実装できる

**メリット**
- チームへの教育コストが最も低い
- Blazor や JavaScript と混在させやすい
- View のテンプレートエンジン（Razor）が強力で部品化しやすい

**デメリット・注意点**
- ページ遷移のたびに全ページリロードが発生するため、UX が SPA と比べて劣る場合がある
- インタラクティブな UI（部分更新）には JavaScript や Blazor の補助が必要

### Blazor Server

**採用理由**
- JavaScript を書かずに C# だけで動的・リアルタイムな UI を実装できる
- バックエンドと同じ言語・型でコードを共有できるため、型安全性が高い
- 同一ソリューション内のサービス（ApplicationService 等）を直接 DI 注入して使える

**メリット**
- フロントエンドとバックエンドで言語・ツールチェーンが統一される
- C# の豊富なライブラリをUIコードから直接利用可能
- データバインディングが簡潔で、CRUD UI の構築が速い

**デメリット・注意点**
- **WebSocket（SignalR）接続を常時維持するため、同時接続数に応じてサーバーリソースが増加する**
- ネットワーク切断時にコンポーネントの状態が失われる
- SEO には適さない（サーバーサイドで完全な HTML を返さない）
- Blazor WebAssembly と異なり、オフライン動作やオンプレ展開のない環境でのスケールには設計が必要

> **このプロジェクトでの Blazor ホスティング構造**
> ```
> ブラウザ → GET /blazor/todos
>   → BlazorTodosController.Index()       ← MVC が HTTP 入口
>   → Views/BlazorTodos/Index.cshtml       ← <component> タグで Razor に埋め込む
>   → blazor.server.js が WebSocket 接続
>   → 以降は Blazor Server が差分 DOM 更新
> ```
> MVC を HTTP のエントリーポイントにすることで、認証・ルーティングの制御を MVC の仕組みに乗せたまま Blazor の動的 UI を活用できます。

### Web API

**採用理由**
- 将来的なモバイルアプリ対応・他システムとの連携を見据えた出口として用意
- MVC / Blazor とは独立した JSON API として、フロントエンドフレームワークからも呼び出せる
- REST 設計に従うことでインターフェースが標準化される

**メリット**
- クライアント非依存（React・Vue・モバイル・他社システム等と連携可能）
- Swagger / OpenAPI 自動生成でクライアント向けドキュメントが整備される
- 水平スケールしやすい（状態を持たない）

**デメリット・注意点**
- 認証・CORS 設定など、Web ブラウザ外からのアクセス要件を別途考慮する必要がある
- 同一ソリューション内の MVC 画面と二重実装になる側面がある

---

## 6. CSS 設計（ITCSS + Every Layout）

### 採用理由

CSS はルールを明確にしないまま書き続けると**詳細度の衝突・スタイルの上書き合戦・グローバルな副作用**が発生し、修正するたびに別の場所が壊れるようになります。ITCSS（Inverted Triangle CSS）は CSS ルールセットを詳細度の低い順に層状に管理するアーキテクチャで、この問題を構造的に解決します。Every Layout は最小限のCSSで汎用的なレイアウトパターンを実現する設計思想です。

### ITCSS の 7 層構造

```
wwwroot/css/
├── 01-settings.css   ← CSS カスタムプロパティ（変数）の定義のみ。副作用なし
├── 02-tools.css      ← ミックスイン相当の共通スタイル定義
├── 03-generic.css    ← リセット・ノーマライズ（ブラウザ差異の吸収）
├── 04-elements.css   ← 素の HTML 要素のデフォルトスタイル（クラス指定なし）
├── 05-objects.css    ← Every Layout パターン（Stack / Cluster / Center 等）
├── 06-components.css ← 再利用可能な UI コンポーネント（ボタン・カード等）
└── 07-utilities.css  ← 上書き用ユーティリティクラス（詳細度が最も高い）
```

**上の層ほど詳細度が低く影響範囲が広い。下の層ほど詳細度が高く局所的。**

### CSS 変数（カスタムプロパティ）の活用

`01-settings.css` にすべての値を集中定義し、他のファイルからは変数参照のみを行うルールにより、**デザイン変更時の変更箇所を 1 ファイルに集約できます**。

```css
/* 01-settings.css */
:root {
  --s0: 1rem;
  --s1: 1.5rem;
  --color-text: #222;
  --color-primary: #0078d4;
}

/* 06-components.css （数値をハードコーディングしない） */
.btn-primary {
  background-color: var(--color-primary);
  padding: var(--s0);
}
```

### Every Layout パターン

```css
/* 05-objects.css : Stack （垂直方向の均等スペーシング） */
.stack > * + * { margin-block-start: var(--s1); }

/* Cluster （横並びの柔軟なグルーピング） */
.cluster { display: flex; flex-wrap: wrap; gap: var(--s0); }
```

### メリット

| メリット | 説明 |
|---|---|
| **スタイルの衝突を防ぐ** | 詳細度の流れが一方向（上から下）なので、意図しない上書きが起きにくい |
| **どこに何を書けばよいか明確** | 新しいスタイルを追加するとき、7 層のどこに置くか判断基準がある |
| **デザイン変更に強い** | CSS 変数（設計トークン）を変更するだけでサイト全体に反映される |
| **Every Layout の再利用性** | Stack・Cluster・Center 等は再利用可能で、個別の `margin` 指定が激減する |
| **Bootstrap 等のフレームワーク不要** | 必要最小限の CSS のみで設計でき、未使用スタイルによる肥大化がない |

### デメリット・注意点

| デメリット | 対策・判断 |
|---|---|
| **学習コスト** | ITCSS・Every Layout を知らないメンバーには用語と層の役割説明が必要 |
| **Bootstrap 等との混在が難しい** | Bootstrap の詳細度とIMCSS の層が競合しやすい。本プロジェクトは独自 CSS のみで設計 |
| **ユーティリティクラスが増えすぎるリスク** | `07-utilities.css` の乱用はインラインスタイルと同じ問題を引き起こす。慎重に追加する |
| **CSS 変数非対応ブラウザ** | IE11 は非対応（2022 年サポート終了済み）。モダンブラウザ限定であれば問題なし |

---

## 7. テスト戦略（xUnit + Gauge + Playwright）

### テストの全体像

```
┌─────────────────────────────────────────────────────────┐
│      E2E テスト（Gauge + Playwright）                   │
│  → ブラウザ操作でアプリ全体のシナリオを検証             │
│  → 日本語 Markdown の spec ファイルが仕様書を兼ねる     │
├─────────────────────────────────────────────────────────┤
│      単体テスト（xUnit）                                │
│  → Domain 層のビジネスロジックを DB なしで高速検証      │
└─────────────────────────────────────────────────────────┘
```

### 単体テスト（xUnit）

**採用理由**
- .NET エコシステムで最も広く使われているテストフレームワーク
- 属性ベースのシンプルな記述で学習コストが低い
- アサーションライブラリ（Shouldly・FluentAssertions 等）との組み合わせも容易

**テスト対象**
- **Domain 層のビジネスロジックを中心にテストする**。`TodoItems.AllCompleted()` のような純粋な C# コードは DB・HTTP なしで高速に実行できます。

```csharp
// テスト例（TodoItemTest.cs）
[Fact]
public void AllCompleted_全アイテムが完了状態になる()
{
    var items = new TodoItems(new[] { new TodoItem(..., IsCompleted: false) });
    var result = items.AllCompleted();
    Assert.All(result.Items, i => Assert.True(i.IsCompleted.Value));
}
```

---

### E2E テスト：Gauge

**採用理由**
- spec ファイルを**日本語 Markdown** で記述できる。仕様書・受け入れ条件・テストが一体化する
- 非エンジニアのステークホルダーも spec ファイルを読めば何をテストしているか理解できる
- ステップ実装を C# で書けるため、.NET チームの技術スタックを統一できる

**メリット**

| メリット | 説明 |
|---|---|
| **仕様と実装が乖離しにくい** | spec（Markdown）がそのままテストになるため、「仕様書とテストが別物」になりにくい |
| **非エンジニアが読める** | ビジネス要件を自然言語で書いた spec が成果物になる |
| **C# によるステップ実装** | Playwright・NUnit・xUnit 等の .NET ライブラリをテストコード内で直接使える |
| **並列実行・レポート生成** | 標準で HTML レポートが生成され、CI/CD への組み込みも容易 |

**spec ファイルの例**

```markdown
# Todo の作成

## 新しい Todo を作成できる
* ブラウザでトップページを開く
* "新しいタスク" という名前で Todo を作成する
* Todo 一覧に "新しいタスク" が表示される
```

**デメリット・注意点**

| デメリット | 対策・判断 |
|---|---|
| **日本語情報が少ない** | 公式ドキュメント（英語）で補う。本プロジェクトの spec ファイルが内部リファレンスになる |
| **VSCode / Rider の IDE サポートが限定的** | Gauge の VSCode 拡張で基本的な補完・実行は可能 |
| **実行が遅い** | E2E テストはブラウザ起動を伴うため必然的に遅い。単体テストで拾えるバグはそちらで対処し、E2E はシナリオレベルに集中する |

---

### E2E テスト：Playwright

**採用理由**
- Microsoft 製の公式ブラウザ自動化ライブラリ。.NET（C#）バインディングが提供されている
- Chromium・Firefox・WebKit（Safari）のクロスブラウザテストを単一 API でサポート
- ロケーター API が堅牢で、要素取得が脆弱な `XPath` や `CSS セレクター` の問題を改善

**メリット**

| メリット | 説明 |
|---|---|
| **Microsoft 製で .NET との親和性が高い** | NuGet パッケージ提供、公式サポート、長期メンテナンスの信頼性 |
| **クロスブラウザ対応** | 1 度書いたテストで Chrome・Firefox・Safari 相当を検証できる |
| **自動待機（Auto-Wait）** | 要素が Visible・Enabled になるまで自動的に待機するため `Thread.Sleep` 不要 |
| **Playwright Test Generator** | ブラウザ操作を記録してテストコードを自動生成できる |
| **スクリーンショット・動画記録** | 失敗時の状態を自動保存でき、デバッグが容易 |

**デメリット・注意点**

| デメリット | 対策・判断 |
|---|---|
| **E2E テストの保守コスト** | UI の変更（id・クラス名・テキスト）がテスト破綻につながる。ロケーターを仕様に密着させる、`data-testid` 属性を活用するなどで緩和 |
| **実行環境のセットアップが必要** | `playwright install` でブラウザのバイナリをダウンロードする必要がある。CI/CD パイプラインへの組み込み時に考慮が必要 |
| **Blazor Server との相性** | WebSocket 接続のタイミングによりロケーターの待機が必要な場面がある |

---

## 8. 技術スタック一覧

| 技術 | バージョン | 用途 | 採用理由（一言） |
|---|---|---|---|
| .NET | 10.0 | アプリケーション全体のランタイム | LTS ではないが最新機能・パフォーマンス改善を早期に享受 |
| ASP.NET Core MVC | 10.0 | 画面遷移系 UI | 標準的・学習コスト低・SEO 対応 |
| ASP.NET Core Blazor Server | 10.0 | インタラクティブ UI | C# のみで動的 UI を構築 |
| ASP.NET Core Web API | 10.0 | REST API 提供 | 外部クライアント・将来の SPA 対応 |
| Entity Framework Core | 10.0.3 | ORM（DB アクセス） | .NET 標準 ORM・マイグレーション管理が容易 |
| SQL Server 2022 | Docker | RDB | EF Core との親和性・Azure SQL との互換性 |
| Microsoft.Identity.Web | 最新 | 認証（Entra ID / OIDC） | Azure AD との統合が公式サポート |
| xUnit | 最新 | 単体テスト | .NET 標準・学習コスト低 |
| Gauge | 最新 | E2E テストフレームワーク | 日本語 Markdown spec・BDD 的アプローチ |
| Playwright (.NET) | 最新 | ブラウザ自動化 | Microsoft 製・クロスブラウザ・Auto-Wait |

---

## 9. このテンプレートの適用判断基準

### このテンプレートが適しているプロジェクト

| 条件 | 説明 |
|---|---|
| **中〜大規模アプリケーション** | CRUD 機能が 5 以上、ドメインルールが複数存在し、複数名で開発する場合 |
| **長期運用・保守が見込まれる** | チームメンバーの入れ替わりを想定し、コードの読みやすさ・差し替えやすさを重視する場合 |
| **DB・インフラの変更可能性がある** | 将来的に SQL Server → PostgreSQL、オンプレ → Azure SQL などの移行を検討している場合 |
| **ビジネス要件とテストを一体化したい** | Gauge spec でビジネス要件とテストを日本語で管理し、ステークホルダーと共有したい場合 |
| **複数の UI パターンが混在する** | 通常の画面遷移と動的 UI・API を同一システム内に持ちたい場合 |

### このテンプレートを使わない（または一部省略する）べきケース

| ケース | 推奨する対応 |
|---|---|
| **PoC・プロトタイプ** | Minimal API + 直接 DB アクセスのシンプルな構成で素早く検証する |
| **1〜2 画面の社内ツール** | MVC のみ + 単一プロジェクト構成で十分。層を分けることが過剰設計になる |
| **1 人開発・短期プロジェクト** | CQRS の読み書き分離やリポジトリ抽象化がかえって開発速度を下げる可能性がある |
| **チームに .NET の経験者が少ない** | まず標準的な MVC のみで開始し、段階的にクリーンアーキテクチャ要素を導入する |

### 段階的な導入アプローチ

クリーンアーキテクチャと CQRS を一度に導入するのが難しい場合は、以下の順で段階的に適用することを推奨します。

```
Step 1: MVC + 単一プロジェクト で動作確認
Step 2: Domain 層を分離（ビジネスロジックを外に出す）
Step 3: Application 層を分離（コマンドハンドラー導入）
Step 4: Infrastructure 層を分離（リポジトリインターフェース導入）
Step 5: CQRS を導入（ReadModel / QueryService の分離）
Step 6: E2E テスト（Gauge + Playwright）の追加
```

---

## 参考資料

- [README.md](../README.md) — セットアップ手順・プロジェクト構成
- [.github/copilot-instructions.md](../.github/copilot-instructions.md) — コーディング規約・アーキテクチャ方針
- [.github/skills/SKILL.md](../.github/skills/SKILL.md) — 各層の詳細なコーディングパターン
- [Clean Architecture（Robert C. Martin）](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Every Layout](https://every-layout.dev/)
- [ITCSS: Scalable and Maintainable CSS Architecture](https://www.creativebloq.com/web-design/manage-large-css-projects-itcss-101517528)
- [Microsoft Playwright .NET](https://playwright.dev/dotnet/)
- [Gauge Documentation](https://docs.gauge.org/)
