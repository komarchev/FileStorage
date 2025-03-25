using FileStorage.Common;
using FileStorage.FileSystem.Configuration;
using FileStorage.FileSystem.StorageService;
using FileStorage.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//builder.Services.Configure<MinIOConfiguration>(builder.Configuration.GetSection("MinIO"));
builder.Services.Configure<FileSystemConfiguration>(builder.Configuration.GetSection("FileStorage"));
builder.Services.AddScoped<IFileStorage, FileSystemStorage>();

builder.Services.AddSingleton<IIdGenerator, GuidIdGenerator>();
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

app.MapGet("/file/{category}/{id}", async (string category, string id, [FromServices] IFileStorage storage, CancellationToken cancellationToken) =>
    {
        var (content, fileName) = await storage.GetFileAsync(id, category, cancellationToken);

        return Results.File(content, string.Empty, fileName);
    })
    .WithName("GetFile")
    .WithTags("FileStorage")
    .WithDescription("Read file from storage")
    .Produces<FileContentResult>();

app.MapPost("/file/{category}", async (string category, IFormFile file, [FromServices] IFileStorage storage, CancellationToken cancellationToken) =>
    {
        await using var stream = file.OpenReadStream();

        var id = await storage.PutFileAsync(stream, file.FileName, category, cancellationToken);

        return Results.CreatedAtRoute("GetFile", new { id });
    })
    .DisableAntiforgery()
    .WithName("PutFile")
    .WithTags("FileStorage")
    .WithDescription("Put file to storage")
    .Produces<CreatedAtRouteResult>();

app.MapMethods("/file/{category}/{id}", [HttpMethods.Head], async (string category, string id, [FromServices] IFileStorage storage, CancellationToken cancellationToken) =>
    {
        var fileExists = await storage.CheckFileAsync(id, category, cancellationToken);
        return fileExists
            ? Results.Ok()
            : Results.NotFound();
    })
    .WithName("CheckFile")
    .WithTags("FileStorage")
    .WithDescription("Check file in storage")
    .Produces<OkResult>()
    .Produces<NotFoundResult>();

app.MapDelete("/file/{category}/{id}", async (string category, string id, [FromServices] IFileStorage storage, CancellationToken cancellationToken) =>
    {
        var result = await  storage.DeleteFileAsync(id, category, cancellationToken);

        return result
            ? Results.Ok()
            : Results.NotFound();
    })
    .WithName("DeleteFile")
    .WithTags("FileStorage")
    .WithDescription("Remove file from storage")
    .Produces<OkResult>();
app.Run();
