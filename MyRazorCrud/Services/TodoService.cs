using MyRazorCrud.Models;
using MyRazorCrud.Repositories;

namespace MyRazorCrud.Services;

// -----------------------------------------------------------------------
// TodoService : ITodoService の実装（ビジネスロジック層）
//
// ★ Controllers/Pages は ITodoService だけに依存する
//   → DB実装（EfTodoRepository）を直接知らなくてよい
//
// ★ 業務ロジックの追加例（実際のプロジェクトでの典型）：
//   - タイトルの重複チェック
//   - 完了時に通知メールを送信
//   - 操作ログをDBに記録
//   - 権限チェック（自分のTodoのみ操作可能など）
//   こういった処理はここに追加していく
// -----------------------------------------------------------------------
public class TodoService : ITodoService
{
    // ★ リポジトリ（DAL）のインターフェースに依存
    //   → EfTodoRepository の具体的な実装を知らない
    //   → テスト時にモックに差し替え可能
    private readonly ITodoRepository _repo;

    public TodoService(ITodoRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<TodoItem>> GetAllAsync()
        => _repo.GetAllAsync();

    public Task<TodoItem?> GetByIdAsync(int id)
        => _repo.GetByIdAsync(id);

    // ★ ビジネスロジックの例：タイトルの前後空白を除去してから保存
    public Task<TodoItem> CreateAsync(string title)
        => _repo.AddAsync(title.Trim());

    // ★ ビジネスロジックの例：タイトルの前後空白を除去してから更新
    public Task<bool> UpdateAsync(int id, string title, bool done)
        => _repo.UpdateAsync(id, title.Trim(), done);

    public Task<bool> DeleteAsync(int id)
        => _repo.DeleteAsync(id);
}
