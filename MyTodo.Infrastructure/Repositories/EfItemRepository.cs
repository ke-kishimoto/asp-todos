using Microsoft.EntityFrameworkCore;
using MyTodo.Application.Repositories;
using MyTodo.Domain.Item;
using MyTodo.Infrastructure.Data;
using MyTodo.Infrastructure.Mappings;
using MyTodo.Infrastructure.Models;

namespace MyTodo.Infrastructure.Repositories;

public class EfItemRepository : IItemRepository
{
    private readonly AppDbContext _db;

    public EfItemRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Item> AddAsync(string itemCode, string itemName, int price)
    {
        var entity = new ItemEntity
        {
            ItemCode = itemCode,
            ItemName = itemName,
            Price    = price,
        };

        _db.Items.Add(entity);
        await _db.SaveChangesAsync();
        return ItemMapping.ToDomain(entity);
    }
}