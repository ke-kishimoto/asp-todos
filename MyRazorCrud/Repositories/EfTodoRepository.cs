using Microsoft.EntityFrameworkCore;
using MyRazorCrud.Data;
using MyRazorCrud.Models;

namespace MyRazorCrud.Repositories;

// -----------------------------------------------------------------------
// EfTodoRepository : ITodoRepository の EF Core（SQL Server）実装
//
// ★ Repository パターンの役割：
//   - SQL（EF Core のクエリ）の詳細をここに閉じ込める
//   - 上位層（Service/Controller）は SQL を意識しない
//
// ★ InMemoryTodoRepository との対比：
//   - InMemoryTodoRepository : List<TodoItem> をメモリ上で操作（同期）
//   - EfTodoRepository       : AppDbContext 経由で SQL Server を操作（非同期）
//
// ★ ライフタイムについて：
//   - AppDbContext は Scoped（リクエストごとに生成）なので
//     このリポジトリも Scoped で DI 登録する
// -----------------------------------------------------------------------
public class EfTodoRepository : ITodoRepository
{
    private readonly AppDbContext _db;

    public EfTodoRepository(AppDbContext db)
    {
        _db = db;
    }

    // SELECT * FROM Todos ORDER BY CreatedAt DESC
    public async Task<IReadOnlyList<TodoItem>> GetAllAsync()
    {
        return await _db.Todos
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    // SELECT * FROM Todos WHERE Id = @id
    public async Task<TodoItem?> GetByIdAsync(int id)
    {
        // FindAsync は主キー検索の最短パス（1次キャッシュも利用される）
        return await _db.Todos.FindAsync(id);
    }

    // INSERT INTO Todos (Title, Done, CreatedAt) VALUES (@title, 0, GETUTCDATE())
    public async Task<TodoItem> AddAsync(string title)
    {
        var item = new TodoItem
        {
            Title = title,
            Done = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Todos.Add(item);          // INSERT を予約（まだDBには送らない）
        await _db.SaveChangesAsync(); // ← ここで実際に SQL を発行
        return item;
    }

    // UPDATE Todos SET Title = @title, Done = @done WHERE Id = @id
    public async Task<bool> UpdateAsync(int id, string title, bool done)
    {
        var item = await _db.Todos.FindAsync(id);
        if (item is null) return false;

        item.Title = title;
        item.Done = done;

        // EF Core は変更を検知（Change Tracking）しているため
        // プロパティを書き換えるだけで UPDATE クエリが生成される
        await _db.SaveChangesAsync();
        return true;
    }

    // DELETE FROM Todos WHERE Id = @id
    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.Todos.FindAsync(id);
        if (item is null) return false;

        _db.Todos.Remove(item);       // DELETE を予約
        await _db.SaveChangesAsync(); // ← ここで実際に SQL を発行
        return true;
    }

    public async Task<IReadOnlyList<TodoItem>> GetByKeywordAsync(string keyword)
    {
        return await _db.Todos
            .Where(x => x.Title.Contains(keyword))
            .ToListAsync();
    }
}
