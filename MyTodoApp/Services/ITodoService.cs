using MyTodoApp.Models;

namespace MyTodoApp.Services;

// -----------------------------------------------------------------------
// ITodoService : ビジネスロジック層（BLL）のインターフェース
//
// ★ Repository との役割の違い：
//
//   Repository（DAL）:「DBをどう操作するか」
//     → SQL の細部、EF Core の使い方に集中する
//
//   Service（BLL）  :「ビジネスルールをどう適用するか」
//     → バリデーション、複数リポジトリの組み合わせ、
//       通知送信、ログ記録などの横断的な処理を担う
//
// ★ このプロジェクトでは学習目的でレイヤーを分けています。
//   シンプルな CRUD であれば Service が Repository を薄くラップする形になりますが、
//   要件が増えた際（「完了済みだけメール通知する」など）に
//   Service 層に処理を追加することで Controller/Pages を汚さずに済みます。
// -----------------------------------------------------------------------
public interface ITodoService
{
    Task<IReadOnlyList<TodoItem>> GetAllAsync();
    Task<TodoItem?> GetByIdAsync(int id);
    Task<TodoItem> CreateAsync(string title);
    Task<bool> UpdateAsync(int id, string title, bool done);
    Task<bool> DeleteAsync(int id);
    Task<IReadOnlyList<TodoItem>> GetItemsAsync(string keyword);
}
