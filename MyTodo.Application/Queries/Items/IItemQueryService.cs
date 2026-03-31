namespace MyTodo.Application.Queries.Items;

public interface IItemQueryService
{
    Task<IReadOnlyList<ItemReadModel>> GetAllAsync();
    Task<ItemReadModel?> GetByIdAsync(int id);
    Task<ItemReadModel?> GetByItemCodeAsync(string itemCode);
}
