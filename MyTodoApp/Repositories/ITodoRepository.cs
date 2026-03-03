using MyTodoApp.Models;

namespace MyTodoApp.Repositories;

// -----------------------------------------------------------------------
// ITodoRepository : データアクセス層（DAL）のインターフェース
//
// ★ レイヤー設計の意図：
//   - 上位層（Service）は「どこからデータを取得するか」を知らなくてよい
//   - このインターフェースに依存させることで、実装を差し替え可能にする
//
// ★ 実装の差し替え例：
//   - EfTodoRepository   : SQL Server（本番）
//   - InMemoryTodoRepository : メモリ（テスト・開発初期）
//
// ★ なぜ非同期（Task<T>）にするのか：
//   - DBアクセスはI/O待ちが発生する
//   - async/await で待機中スレッドを解放し、サーバーのスループットを向上させる
// -----------------------------------------------------------------------
public interface ITodoRepository
{
    Task<IReadOnlyList<TodoItem>> GetAllAsync();
    Task<TodoItem?> GetByIdAsync(int id);
    Task<TodoItem> AddAsync(string title);
    Task<bool> UpdateAsync(int id, string title, bool done);
    Task<bool> DeleteAsync(int id);
    Task<IReadOnlyList<TodoItem>> GetByKeywordAsync(string keyword);
}
