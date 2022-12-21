
using Dawn;
using Newtonsoft.Json;

namespace WebApi
{
    /// <summary>
    /// Some useful math methods.
    /// </summary>
    public static class Util
    {
        private const string FilePath = "./docItems.json";
        public static Double CosineSimilarity(ReadOnlySpan<double> a, ReadOnlySpan<double> b)
        {
            int aIdx = 0;
            int bIdx = 0;
            int len = a.Length;

            const Double epsilon = 1e-12f;

            Double ab = 0;
            Double a2 = 0;
            Double b2 = 0;

            for (int lim = aIdx + len; aIdx < lim; aIdx++, bIdx++)
            {
                ab += (Double)a[aIdx] * b[bIdx];
                a2 += (Double)a[aIdx] * a[aIdx];
                b2 += (Double)b[bIdx] * b[bIdx];
            }

            Double similarity = ab / (Math.Sqrt(a2 * b2) + epsilon);

            if (Math.Abs(similarity) > 1)
                return similarity > 1 ? 1 : -1;

            return similarity;
        }

        public static async Task<bool> Save(List<DocItem> items)
        {
            var json = JsonConvert.SerializeObject(items);
            await File.WriteAllTextAsync(FilePath, json);
            return true;
        }

        public static async Task<List<DocItem>> Load()
        {
            var json = await File.ReadAllTextAsync(FilePath);
            var items = JsonConvert.DeserializeObject<List<DocItem>>(json);
            return items;
        }
    }
}