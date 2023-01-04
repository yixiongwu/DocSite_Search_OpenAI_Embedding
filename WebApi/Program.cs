using Dawn;
using WebApi;
using OpenAI.GPT3.Extensions;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Accord.Math.Distances;
using System.Collections.Concurrent;

const string Model = "text-embedding-ada-002";
const int LengthLimit = 8200;

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

app.MapGet("/analyzingDocItems", async () =>
{
    var items = await Util.Load(@"../ConsoleApp/docItems.json");
    return new
    {
        items.Count,
        LengthLimit,
        CountMoreThanLengthLimit = items.Count(it => it.Content.Length >= LengthLimit),
        CountMoreThanLengthLimitRatios = items.Where(it => it.Content.Length >= LengthLimit).Select(it => Math.Round((decimal)it.Content.Length / LengthLimit, 2)).ToList()
    };
});

app.MapGet("/embeddingDocItems", async () =>
{

    var openai = app.Services.GetService<IOpenAIService>() ?? throw new ApplicationException("OpenAI service is null");

    // If file is not exist, we need to generate it through the console app
    var items = await Util.Load(@"../ConsoleApp/docItems.json");
    var input = items.Where(it => it.Content.Length > 0)
    .Select(it => it.Content.Substring(0, it.Content.Length > LengthLimit ? LengthLimit : it.Content.Length))
    .ToList();

    var result = await openai.Embeddings.CreateEmbedding(new EmbeddingCreateRequest
    {
        Input = input,
        Model = Model
    });

    Guard.Argument(result, nameof(result)).NotNull();
    Guard.Argument(result.Successful, nameof(result.Successful)).True($"Code:{result.Error?.Code},Message:{result.Error?.Message}");

    for (int i = 0; i < items.Count; i++)
    {
        var item = items[i];
        item.Id = i;
        item.Embedding = result.Data[i].Embedding;
    }

    await Util.Save(items);
    return items.Count;
});

app.MapPost("/recommendations", async (RecommendationRequest request) =>
{
    var count = 3;
    if (request.Count > 0) count = request.Count;

    // Load the doc site from disk
    var items = await Util.Load();

    ConcurrentBag<RecommendationResponseItem> recommendationResponseItems = new ConcurrentBag<RecommendationResponseItem>();
    Cosine cosine = new Cosine();

    var item = items.FirstOrDefault(it => it.Id == request.id);
    if (item != null)
    {
        // Parallel execution to maximize the use of multi-core CPUs
        items.AsParallel().ForAll(it =>
        {
            // Calculate the distance between two Embeddings
            var distance = cosine.Distance(item.Embedding?.ToArray(), it.Embedding?.ToArray());
            recommendationResponseItems.Add(new RecommendationResponseItem(it.Id, it.FileName, it.Title, it.Category, distance, it.Content.Summary(200)));
        });
        return new Recommendation(item.Id, item.FileName,
            item.Title,
            item.Content.Summary(),
            recommendationResponseItems
            .Where(it => it.Id != item.Id)
            .OrderBy(it => it.Distance)
            .Take(count)
            .ToList());
    }
    else
    {
        return null;
    }
});

app.MapPost("/search", async (SearchRequest request) =>
{
    var count = 3;
    if (request.Count > 0) count = request.Count;

    var openai = app.Services.GetService<IOpenAIService>() ?? throw new ApplicationException("OpenAI service is null");

    var result = await openai.Embeddings.CreateEmbedding(new EmbeddingCreateRequest
    {
        Input = new List<string> { request.QueryText },
        Model = Model
    });

    Guard.Argument(result, nameof(result)).NotNull();
    Guard.Argument(result.Successful, nameof(result.Successful)).True($"Code:{result.Error?.Code},Message:{result.Error?.Message}");

    var embedding = result.Data.First().Embedding;

    // Load the doc site items(articles) from disk
    var items = await Util.Load();

    ConcurrentBag<SearchResponseItem> searchResponseItems = new ConcurrentBag<SearchResponseItem>();
    Cosine cosine = new Cosine();

    // Parallel execution to maximize the use of multi-core CPUs
    items.AsParallel().ForAll(it =>
    {
        // Calculate the distance between two Embeddings
        var similarities = cosine.Similarity(it.Embedding?.ToArray(), embedding.ToArray());
        searchResponseItems.Add(new SearchResponseItem(it.Id, it.FileName, it.Title, it.Category, similarities, it.Content.Summary(200)));
    });
    return searchResponseItems.OrderByDescending(it => it.Similarities).Take(count);
});


app.Run();

