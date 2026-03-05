using MyTodo.Domain.Item;
using MyTodo.Infrastructure.Repositories;

namespace MyTodo.Application.Services;

public class ItemService: IItemService
{

    private readonly IItemRepository _repo;
    public ItemService(IItemRepository repo)
    {
        _repo = repo;
    }
    public async Task<Item?> GetByIdAsync(int id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item == null ? null : new Item(
            Id: new ItemId(item.Id), 
            Name: new ItemName(item.ItemName), 
            Code: new ItemCode(item.ItemCode), 
            Price: new ItemPrice(item.Price)
            );
    }

    public async Task<Item?> GetByItemCodeAsync(string itemCode)
    {
        var item = await _repo.GetByItemCodeAsync(itemCode);
        return item == null ? null : new Item(
            Id: new ItemId(item.Id), 
            Name: new ItemName(item.ItemName), 
            Code: new ItemCode(item.ItemCode), 
            Price: new ItemPrice(item.Price)
            );
    }
}