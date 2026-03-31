namespace MyTodo.Application.Queries.Todos;

public interface ITodoQueryService
{
    Task<IReadOnlyList<TodoReadModel>> GetAllAsync();
    Task<TodoReadModel?> GetByIdAsync(int id);
    Task<IReadOnlyList<TodoReadModel>> SearchAsync(string keyword);
}
