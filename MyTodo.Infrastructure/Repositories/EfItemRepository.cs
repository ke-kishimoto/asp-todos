using Microsoft.EntityFrameworkCore;
using MyTodo.Infrastructure.Models;
using MyTodo.Infrastructure.Data;

namespace MyTodo.Infrastructure.Repositories;

public class EfItemRepository : IItemRepository
{
    private readonly AppDbContext _db;

    public EfItemRepository(AppDbContext db)
    {
        _db = db;
    }

    // SELECT * FROM Items ORDER BY Id
    public async Task<IReadOnlyList<ItemEntity>> GetAllAsync()
    {
        return await _db.Items.OrderBy(x => x.Id).ToListAsync();
    }

    public async Task<ItemEntity?> GetByIdAsync(int id)
    {
        return await _db.Items.FindAsync(id);
    }

    public async Task<ItemEntity?> GetByItemCodeAsync(string itemCode)
    {
        return await _db.Items.FirstOrDefaultAsync(i => i.ItemCode == itemCode);
    }

    // INSERT INTO Items (ItemCode, ItemName, Price) VALUES (@itemCode, @itemName, @price)
    public async Task<ItemEntity> AddAsync(string itemCode, string itemName, int price)
    {
        var item = new ItemEntity
        {
            ItemCode = itemCode,
            ItemName = itemName,
            Price    = price,
        };

        _db.Items.Add(item);          // INSERT を予約（まだ DB に送らない）
        await _db.SaveChangesAsync(); // ← ここで実際に SQL を発行
        return item;
    }
}