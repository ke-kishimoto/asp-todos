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

    public async Task<ItemEntity?> GetByIdAsync(int id)
    {
        return await _db.Items.FindAsync(id);
    }
}