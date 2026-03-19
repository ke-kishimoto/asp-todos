using MyTodo.Infrastructure.Models;

namespace MyTodo.Infrastructure.Repositories;

public interface IItemRepository
{
    /// <summary>全アイテムを取得する</summary>
    Task<IReadOnlyList<ItemEntity>> GetAllAsync();

    Task<ItemEntity?> GetByIdAsync(int id);

    Task<ItemEntity?> GetByItemCodeAsync(string itemCode);

    /// <summary>新規アイテムを登録する</summary>
    Task<ItemEntity> AddAsync(string itemCode, string itemName, int price);
}