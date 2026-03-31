using Microsoft.EntityFrameworkCore;
using MyTodo.Application.Queries.Items;
using MyTodo.Infrastructure.Data;

namespace MyTodo.Infrastructure.Queries;

public class ItemQueryService : IItemQueryService
{
    private readonly AppDbContext _db;

    public ItemQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ItemReadModel>> GetAllAsync()
    {
        return await _db.Items
            .OrderBy(x => x.Id)
            .Select(x => new ItemReadModel(x.Id, x.ItemCode, x.ItemName, x.Price))
            .ToListAsync();
    }

    public async Task<ItemReadModel?> GetByIdAsync(int id)
    {
        var entity = await _db.Items.FindAsync(id);
        return entity is null
            ? null
            : new ItemReadModel(entity.Id, entity.ItemCode, entity.ItemName, entity.Price);
    }

    public async Task<ItemReadModel?> GetByItemCodeAsync(string itemCode)
    {
        var entity = await _db.Items.FirstOrDefaultAsync(i => i.ItemCode == itemCode);
        return entity is null
            ? null
            : new ItemReadModel(entity.Id, entity.ItemCode, entity.ItemName, entity.Price);
    }
}
