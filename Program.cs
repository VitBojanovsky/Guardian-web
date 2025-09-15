using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add OpenAPI for development/testing
builder.Services.AddOpenApi();

var app = builder.Build();

// Serve static files (index.html, CSS, JS, etc.)
app.UseDefaultFiles();
app.UseStaticFiles();

// Enable OpenAPI in development
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

// Read connection string from appsettings.json
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");

// ----------------------
// GET: List all FAQs
// ----------------------
app.MapGet("/faq", async () =>
{
    var faqs = new List<object>();
    try
    {
        await using var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(
            "SELECT Id, Question, Answer, CreatedAt FROM faq ORDER BY CreatedAt DESC", conn);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            faqs.Add(new
            {
                id = reader["Id"],
                question = reader["Question"],
                answer = reader["Answer"],
                createdAt = reader["CreatedAt"]
            });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }

    return Results.Json(faqs);
});

// ----------------------
// GET: Single FAQ by ID
// ----------------------
app.MapGet("/faq/{id:int}", async (int id) =>
{
    try
    {
        await using var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(
            "SELECT Id, Question, Answer, CreatedAt FROM faq WHERE Id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Results.Json(new
            {
                id = reader["Id"],
                question = reader["Question"],
                answer = reader["Answer"],
                createdAt = reader["CreatedAt"]
            });
        }
        else
        {
            return Results.NotFound();
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// ----------------------
// POST: Add new FAQ
// ----------------------
app.MapPost("/faq", async (HttpRequest req) =>
{
    var form = await req.ReadFormAsync();
    var question = form["question"].ToString();
    var answer = form["answer"].ToString();

    if (string.IsNullOrWhiteSpace(question))
        return Results.BadRequest("Question is required.");

    try
    {
        await using var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(
            "INSERT INTO faq (Question, Answer) VALUES (@q, @a);", conn);
        cmd.Parameters.AddWithValue("@q", question);
        cmd.Parameters.AddWithValue("@a", string.IsNullOrEmpty(answer) ? DBNull.Value : answer);

        await cmd.ExecuteNonQueryAsync();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }

    return Results.Ok(new { question, answer });
});

// ----------------------
// PUT: Update existing FAQ
// ----------------------
app.MapPut("/faq/{id:int}", async (int id, HttpRequest req) =>
{
    var form = await req.ReadFormAsync();
    var question = form["question"].ToString();
    var answer = form["answer"].ToString();

    if (string.IsNullOrWhiteSpace(question))
        return Results.BadRequest("Question is required.");

    try
    {
        await using var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(
            "UPDATE faq SET Question = @q, Answer = @a, UpdatedAt = NOW() WHERE Id = @id", conn);
        cmd.Parameters.AddWithValue("@q", question);
        cmd.Parameters.AddWithValue("@a", string.IsNullOrEmpty(answer) ? DBNull.Value : answer);
        cmd.Parameters.AddWithValue("@id", id);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0 ? Results.Ok(new { id, question, answer }) : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// ----------------------
// DELETE: Remove FAQ
// ----------------------
app.MapDelete("/faq/{id:int}", async (int id) =>
{
    try
    {
        await using var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand("DELETE FROM faq WHERE Id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0 ? Results.Ok() : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// ----------------------
// Optional: WeatherForecast example
// ----------------------
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
