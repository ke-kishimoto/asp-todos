using MyTodo.Domain.Item;

namespace MyTodo.Application.Services;

public interface IItemService
{
    Task<Item?> GetByIdAsync(int id);
}