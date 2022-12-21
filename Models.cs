﻿namespace WebApi
{
    public class DocItem
    {
        public DocItem(int id, string title, string content)
        {
            Id = id;
            Title = title;
            Content = content;
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public List<double>? Embedding { get; set; }
    }

    public record EmbeddingRequest(List<DocItem> Items);

    public record SearchRequest(string QueryText, int Count);

    public record SearchResponseItem(string Title, string Content, double Similarities);
}
