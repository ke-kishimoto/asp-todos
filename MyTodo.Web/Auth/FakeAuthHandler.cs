using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace MyTodo.Web.Auth;

// -----------------------------------------------------------------------
// FakeAuthHandler : ローカル開発専用の「偽認証」ハンドラー
//
// ★ ASP.NET Core の認証アーキテクチャ：
//   - すべての認証は "認証スキーム" に基づいて動作する
//   - スキームは AuthenticationHandler<TOptions> を継承して実装する
//   - Program.cs で AddAuthentication().AddScheme<>() に登録する
//
// ★ このハンドラーの動作：
//   - HandleAuthenticateAsync() が呼ばれると、毎回「認証成功」を返す
//   - 実際のトークン検証・ログイン画面・リダイレクトは一切行わない
//   - 開発者が Entra ID テナントなしで [Authorize] 付きページを動作確認できる
//
// ★ 本番環境では絶対に使用しない！
//   Program.cs で UseFakeAuth フラグが true の場合のみ登録される
// -----------------------------------------------------------------------
public class FakeAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public FakeAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    // -----------------------------------------------------------------------
    // HandleAuthenticateAsync : 認証の核心メソッド
    //
    // ★ ASP.NET Core は各リクエストでこのメソッドを呼び出す
    //   - AuthenticateResult.Success(ticket) → 認証済みとして処理を続行
    //   - AuthenticateResult.Fail(...)       → 認証失敗（403 等）
    //   - AuthenticateResult.NoResult()      → このハンドラーでは判定しない
    //
    // ★ Claim（クレーム）とは：
    //   認証済みユーザーに関する「属性」を表すキーと値のペア
    //   例) ClaimTypes.Name = "開発太郎"
    //   → 実際の Entra ID ではトークン内のフィールドがクレームとしてマップされる
    // -----------------------------------------------------------------------
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // ローカル開発用のダミークレームを定義する
        // 実際の Entra ID から返ってくるクレームと同じキー名を使用している
        var claims = new[]
        {
            // ユーザーの固有ID（Entra ID では "oid" クレームに相当）
            new Claim(ClaimTypes.NameIdentifier, "fake-user-id-001"),

            // 表示名（Entra ID では "name" クレームに相当）
            new Claim(ClaimTypes.Name, "開発太郎"),

            // メールアドレス（Entra ID では "preferred_username" クレームに相当）
            new Claim(ClaimTypes.Email, "dev-user@example.com"),

            // preferred_username: Entra ID のログイン名（UPN）に相当
            new Claim("preferred_username", "dev-user@example.com"),
        };

        // ClaimsIdentity: 認証されたユーザーの「身元証明」
        //   第2引数 Scheme.Name = 認証スキーム名（"FakeAuth"）
        //   → どのスキームで認証されたかを示す
        var identity = new ClaimsIdentity(claims, Scheme.Name);

        // ClaimsPrincipal: ユーザーを表すオブジェクト（複数の Identity を持てる）
        //   → User.Identity.IsAuthenticated がここで true になる
        var principal = new ClaimsPrincipal(identity);

        // AuthenticationTicket: 認証チケット（スキームとプリンシパルのセット）
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        // 認証成功を返す（これで [Authorize] を通過できる）
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
