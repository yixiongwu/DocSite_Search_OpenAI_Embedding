using Dawn;
using WebApi;
using OpenAI.GPT3.Extensions;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Accord.Math.Distances;
using System.Collections.Concurrent;

const string Model = "text-embedding-ada-002";
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string OPENAI_KEY = Environment.GetEnvironmentVariable("OPENAI_KEY") ?? throw new ArgumentNullException("OPENAI_KEY is null");

builder.Services.AddOpenAIService(settings => settings.ApiKey = OPENAI_KEY);

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapGet("/online", () =>
{
    return $"service online, {DateTime.Now}";
});

app.MapGet("/models", async () =>
{
    var openai = app.Services.GetService<IOpenAIService>() ?? throw new ApplicationException("OpenAI service is null");
    var models = await openai.Models.ListModel();
    return models;
});

app.MapGet("/completion", async (string prompt) =>
{
    var openai = app.Services.GetService<IOpenAIService>() ?? throw new ApplicationException("OpenAI service is null");
    var result = await openai.Completions.CreateCompletion(new CompletionCreateRequest()
    {
        Prompt = prompt,
        MaxTokens = 100
    }, Models.TextDavinciV3);

    Guard.Argument(result, nameof(result)).NotNull();
    Guard.Argument(result.Successful, nameof(result.Successful)).True($"Code:{result.Error?.Code},Message:{result.Error?.Message}");

    return result.Choices.FirstOrDefault();
});


app.MapPost("/embedding", async (EmbeddingRequest request) =>
{
    var openai = app.Services.GetService<IOpenAIService>() ?? throw new ApplicationException("OpenAI service is null");

    var result = await openai.Embeddings.CreateEmbedding(new EmbeddingCreateRequest
    {
        Input = request.Items.Select(it => it.Title + ":" + it.Content).ToList(),
        Model = Model
    });

    Guard.Argument(result, nameof(result)).NotNull();
    Guard.Argument(result.Successful, nameof(result.Successful)).True($"Code:{result.Error?.Code},Message:{result.Error?.Message}");

    for (int i = 0; i < request.Items.Count; i++)
    {
        var item = request.Items[i];
        item.Embedding = result.Data[i].Embedding;
    }

    await Util.Save(request.Items);
    return request.Items.Count;
});

app.MapPost("/search", async (SearchRequest request) =>
{
    var openai = app.Services.GetService<IOpenAIService>() ?? throw new ApplicationException("OpenAI service is null");

    var result = await openai.Embeddings.CreateEmbedding(new EmbeddingCreateRequest
    {
        Input = new List<string> { request.QueryText },
        Model = Model
    });

    Guard.Argument(result, nameof(result)).NotNull();
    Guard.Argument(result.Successful, nameof(result.Successful)).True($"Code:{result.Error?.Code},Message:{result.Error?.Message}");

    var embedding = result.Data.First().Embedding;

    var items = await Util.Load();

    ConcurrentBag<SearchResponseItem> searchResponseItems = new ConcurrentBag<SearchResponseItem>();
    Cosine cosine = new Cosine();
    items.AsParallel().ForAll(it =>
    {
        var similarities = cosine.Similarity(it.Embedding?.ToArray(), embedding.ToArray());
        searchResponseItems.Add(new SearchResponseItem(it.Title, it.Content, similarities));
    });
    return searchResponseItems.OrderByDescending(it => it.Similarities).Take(request.Count);
});


app.Run();

