using MyTodo.Domain.Item;

namespace MyTodo.Application.Repositories;

public interface IItemRepository
{
    Task<Item> AddAsync(string itemCode, string itemName, int price);
}
