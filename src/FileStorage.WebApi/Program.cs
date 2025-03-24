using FileStorage.Common;
using FileStorage.MinIO.Configuration;
using FileStorage.MinIO.StorageService;
using FileStorage.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<MinIOConfiguration>(builder.Configuration.GetSection("MinIO"));
builder.Services.AddScoped<IFileStorage, MinIOFileStorage>();

builder.Services.AddSingleton<IIdGenerator, IdGenerator>();
builder.Services.AddSingleton<ICompressor, ZipCompressor>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/file/{id}", async (string id, [FromServices] IFileStorage storage, CancellationToken cancellationToken) =>
    {
        var (content, fileName, contentType) = await storage.GetFileAsync(id, cancellationToken);

        return Results.File(content, contentType, fileName);
    })
    .WithName("GetFile")
    .WithTags("FileStorage")
    .WithDescription("Read file from storage")
    .Produces<FileContentResult>();

app.MapPost("/file/", async (IFormFile file, [FromServices] IFileStorage storage, CancellationToken cancellationToken) =>
    {
        await using var stream = file.OpenReadStream();

        var id = await storage.PutFileAsync(stream, file.FileName, file.ContentType, cancellationToken);

        return Results.CreatedAtRoute("GetFile", new { id });
    })
    .DisableAntiforgery()
    .WithName("PutFile")
    .WithTags("FileStorage")
    .WithDescription("Put file to storage")
    .Produces<CreatedAtRouteResult>();

app.Run();
