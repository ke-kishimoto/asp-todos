using MyTodo.Infrastructure.Models;

namespace MyTodo.Infrastructure.Repositories;

public interface IItemRepository
{
    Task<ItemEntity?> GetByIdAsync(int id);

    Task<ItemEntity?> GetByItemCodeAsync(string itemCode);
}