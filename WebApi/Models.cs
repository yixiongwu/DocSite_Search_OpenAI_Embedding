namespace WebApi
{
    public class DocItem
    {
        public DocItem(int id, string fileName, string title, string category, string content)
        {
            Id = id;
            FileName = fileName;
            Title = title;
            Category = category;
            Content = content;
        }

        public int Id { get; set; }
        public string FileName { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public List<double>? Embedding { get; set; }
    }

    public record EmbeddingRequest(List<DocItem> Items);

    public record SearchRequest(string QueryText, int Count);

    public record SearchResponseItem(int Id, string FileName, string Title, string Category, double Similarities, string Summary);

    public record RecommendationRequest(int id, int Count);

    public record Recommendation(int Id, string FileName, string Title, string Summary, List<RecommendationResponseItem> Recommendations);
    public record RecommendationResponseItem(int Id, string FileName, string Title, string Category, double Distance, string Summary);

}
