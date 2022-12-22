namespace WebApi
{
    public class DocItem
    {
        public DocItem(int id, string fileName, string title, string content)
        {
            Id = id;
            FileName = fileName;
            Title = title;
            Content = content;
        }

        public int Id { get; set; }
        public string FileName { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public List<double>? Embedding { get; set; }
    }

    public record EmbeddingRequest(List<DocItem> Items);

    public record SearchRequest(string QueryText, int Count);

    public record SearchResponseItem(int Id, string FileName, string Title, double Similarities);

    public record RecommendationRequest(int id, int Count);

    public record Recommendation(int Id, string FileName, string Title, List<RecommendationResponseItem> Recommendations);
    public record RecommendationResponseItem(int Id, string FileName, string Title, double Distance);

}
