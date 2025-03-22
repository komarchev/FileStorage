using FileStorage.Common;
using FileStorage.MinIO.Configuration;
using FileStorage.MinIO.StorageService;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<MinIOConfiguration>(builder.Configuration.GetSection("MinIO"));
builder.Services.AddScoped<IFileStorage, MinIOFileStorage>();

builder.Services.AddEndpointsApiExplorer(); // <!-- Add this line
builder.Services.AddSwaggerGen(); // <!-- Add this line

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    app.UseSwagger(); // <!-- Add this line
    app.UseSwaggerUI(); // <!-- Add this line
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/file/{path}", async (string path, [FromServices] IFileStorage storage, CancellationToken cancellationToken) =>
{
    var (contentType, content) = await storage.LoadFileAsync(path, cancellationToken);

    return Results.Bytes(content, contentType);
});

app.MapPut("/file/{path}", async (string path, IFormFile file, [FromServices] IFileStorage storage, CancellationToken cancellationToken) =>
    {
        await using var stream = file.OpenReadStream();

        await storage.SaveFileAsync(path, stream, file.ContentType, cancellationToken);
    })
    .DisableAntiforgery();


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