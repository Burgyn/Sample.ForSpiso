
// 💡 Spustenie aplikácie: dotnet watch
// ToDo
// 1. Dokončite použitie IContactRepository v endpointoch
// 2. Upraviť logovací middleware tak, aby logoval len pokiaľ je vyplnená nejaká vlastná hlavička (napr. X-Log: true)


using System.Diagnostics;
using AutoBogus;

var builder = WebApplication.CreateBuilder(args);

// Registrovanie do DI kontajnera
// 👉 https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-7.0
builder.Services.AddSingleton<IContactRepository, ContactDummyRepository>();

var app = builder.Build();

// Keď dorobíš repository tak toto môžeš vymazať
Dictionary<int, Contact> contacts = AutoFaker.Generate<Contact>(10)
    .ToDictionary(c => c.Id);

// prvý middleware
// 👉 https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-7.0
app.Use(async (context, next) => {
    var sw = Stopwatch.StartNew();
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");

    await next.Invoke();

    sw.Stop();
    Console.WriteLine($"Response: {context.Response.StatusCode} ({sw.ElapsedMilliseconds} ms)");
});

// Naše prvé endpointy 👇
app.MapGet("/hello", () => "Hello KROS!");
app.MapGet("/hello/{name}/{number}", (string name, int number) => $"Hello {name} - {number}!");

// Endpointy na prácu s kontaktmi 👇
// Get all
app.MapGet("/contacts", (IContactRepository contactRepository) => contactRepository.GetAll());

// Get by id
app.MapGet("/contacts/{id}", (int id, IContactRepository contactRepository) => {
    var contact = contactRepository.Get(id);
    if (contact != null)
    {
        return Results.Ok(contact);
    }
    else
    {
        return Results.NotFound();
    }
});

// Create
app.MapPost("/contacts", (Contact contact) => {
    contact.Id = contacts.Count + 1;
    contacts.Add(contact.Id, contact);

    return Results.Created($"/contacts/{contact.Id}", new { contact.Id });
});

// Update
app.MapPut("/contacts/{id}", (int id, Contact contact) => {
    if (contacts.TryGetValue(id, out var existingContact))
    {
        existingContact.Name = contact.Name;
        existingContact.Email = contact.Email;
        return Results.Ok(existingContact);
    }
    else
    {
        return Results.NotFound();
    }
});

// Delete
app.MapDelete("/contacts/{id}", (int id) => {
    if (contacts.TryGetValue(id, out var contact))
    {
        contacts.Remove(id);
        return Results.NoContent();
    }
    else
    {
        return Results.NotFound();
    }
});

// Spustenie aplikácie
app.Run();

// Toto všetko má ísť do samostatných súborov

public class Contact
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public interface IContactRepository
{
    IEnumerable<Contact> GetAll();
    Contact Get(int id);
    void Add(Contact contact);
    void Update(Contact contact);
    void Delete(int id);
}

public class ContactDummyRepository : IContactRepository
{
    private readonly Dictionary<int, Contact> _contacts;

    public ContactDummyRepository()
    {
        _contacts = AutoFaker.Generate<Contact>(10)
            .ToDictionary(c => c.Id);
    }

    public IEnumerable<Contact> GetAll()
    {
        return _contacts.Values;
    }

    public Contact Get(int id)
    {
        if (_contacts.TryGetValue(id, out var contact))
        {
            return contact;
        }
        else
        {
            return null;
        }
    }

    public void Add(Contact contact)
    {
        contact.Id = _contacts.Count + 1;
        _contacts.Add(contact.Id, contact);
    }

    public void Update(Contact contact)
    {
        if (_contacts.TryGetValue(contact.Id, out var existingContact))
        {
            existingContact.Name = contact.Name;
            existingContact.Email = contact.Email;
        }
    }

    public void Delete(int id)
    {
        if (_contacts.TryGetValue(id, out var contact))
        {
            _contacts.Remove(id);
        }
    }
}