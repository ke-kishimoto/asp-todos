namespace MyTodo.Application.Repositories;

public interface ICategoryRepository
{
    Task AddAsync(string name);
    Task UpdateAsync(int id, string name);
    Task DeleteAsync(int id);
}
