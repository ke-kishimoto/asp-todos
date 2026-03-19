using MyTodo.Domain.Item;

namespace MyTodo.Application.Services;

public interface IItemService
{
    /// <summary>全アイテムを取得する</summary>
    Task<IReadOnlyList<Item>> GetAllAsync();

    Task<Item?> GetByIdAsync(int id);
    Task<Item?> GetByItemCodeAsync(string itemCode);

    /// <summary>新規アイテムを登録する</summary>
    Task<Item> CreateAsync(string itemCode, string itemName, int price);
}