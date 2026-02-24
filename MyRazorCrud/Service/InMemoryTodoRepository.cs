using MyRazorCrud.Models;

namespace MyRazorCrud.Services;

public class InMemoryTodoRepository
{
    private readonly List<TodoItem> _items = new();
    private int _nextId = 1;

    public InMemoryTodoRepository()
    {
        // 初期データ（動作確認用）
        Add("Learn Razor Pages");
        Add("Build List page");
        Add("Add Create/Edit/Delete next");
    }

    public IReadOnlyList<TodoItem> GetAll() => _items;

    public TodoItem Add(string title)
    {
        var item = new TodoItem
        {
            Id = _nextId++,
            Title = title,
            Done = false,
            CreatedAt = DateTime.UtcNow
        };

        _items.Add(item);
        return item;
    }

    public TodoItem? GetById(int id)
    {
        return _items.FirstOrDefault(x => x.Id == id);
    }

    public bool Update(int id, string title, bool done)
    {
        var item = _items.FirstOrDefault(x => x.Id == id);
        if (item is null) return false;

        item.Title = title;
        item.Done = done;
        return true;
    }

    public bool Delete(int id)
    {
        var item = _items.FirstOrDefault(x => x.Id == id);
        if (item is null) return false;

        _items.Remove(item);
        return true;
    }
}
