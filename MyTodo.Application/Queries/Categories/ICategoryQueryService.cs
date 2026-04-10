namespace MyTodo.Application.Queries.Categories;

public record CategoryReadModel(int Id, string Name, DateTime CreatedAt);

public interface ICategoryQueryService
{
    Task<IReadOnlyList<CategoryReadModel>> GetAllAsync();
}
