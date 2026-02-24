using Microsoft.EntityFrameworkCore;
using MyRazorCrud.Models;

namespace MyRazorCrud.Data;

// -----------------------------------------------------------------------
// AppDbContext : EF Core のデータベースコンテキスト
//
// DbContext は EF Core の中心的なクラスで、以下の役割を持つ：
//   1. DB接続の管理
//   2. エンティティ（モデル）とテーブルのマッピング定義
//   3. クエリ・保存操作の窓口（DbSet<T> を通じて操作）
//
// ★ インポートリポジトリパターン（InMemoryTodoRepository）との対比：
//   - InMemoryTodoRepository : メモリ上にデータを保持（再起動でリセット）
//   - AppDbContext + EF Core : SQL Server に永続化（再起動後もデータが残る）
// -----------------------------------------------------------------------
public class AppDbContext : DbContext
{
    // DI から DbContextOptions を受け取る
    // → Program.cs で builder.Services.AddDbContext<AppDbContext>(...) で設定した
    //   接続文字列などのオプションがここに渡される
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // ---------------------------------------------------------------
    // DbSet<T> : テーブルに対応するプロパティ
    //
    // DbSet<TodoItem> Todos → "Todos" テーブルにマップ
    //
    // 使い方例：
    //   var all  = await _db.Todos.ToListAsync();           // SELECT *
    //   var item = await _db.Todos.FindAsync(1);            // SELECT WHERE id=1
    //   _db.Todos.Add(new TodoItem { Title = "foo" });      // INSERT 予約
    //   await _db.SaveChangesAsync();                       // 実際にDBへ送信
    // ---------------------------------------------------------------
    public DbSet<TodoItem> Todos { get; set; }

    // ---------------------------------------------------------------
    // OnModelCreating : テーブル・カラムの詳細設定（Fluent API）
    //
    // Data Annotations（[Required] 等）でも設定できるが、
    // モデルをDB設定から分離したい場合はここに集約するのが一般的
    // ---------------------------------------------------------------
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TodoItem>(entity =>
        {
            // テーブル名の明示（省略すると DbSet プロパティ名 "Todos" が使われる）
            entity.ToTable("Todos");

            // 主キー（デフォルトで Id という名前のプロパティが主キーになるが明示）
            entity.HasKey(e => e.Id);

            // Id は DB側で自動採番（IDENTITY）
            entity.Property(e => e.Id)
                  .ValueGeneratedOnAdd();

            // Title は NOT NULL、最大200文字
            entity.Property(e => e.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            // Done のデフォルト値を false に設定
            entity.Property(e => e.Done)
                  .HasDefaultValue(false);

            // CreatedAt はデフォルトを現在時刻（DB側）に設定
            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");
        });
    }
}
