using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var todos = new List<Todo>();

// Get all todos
app.MapGet("/todos", () => todos);

// Get a specific todo by ID
app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) =>
{
    var targetTodo = todos.SingleOrDefault(t => t.Id == id);
    return targetTodo is null
    ? TypedResults.NotFound()
    : TypedResults.Ok(targetTodo);
});

// Create a new todo
app.MapPost("/todos", (Todo task) =>
{
    todos.Add(task);
    return TypedResults.Created("/todos/{id}", task);
});

// Delete a todo by ID
app.MapDelete("/todos/{id}", (int id) =>
{
    todos.RemoveAll(t => t.Id == id);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsComplete);