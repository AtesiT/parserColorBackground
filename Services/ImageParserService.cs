using HtmlAgilityPack;
using System.Web;

namespace parserColorBackground.Services
{
    public class ImageParserService
    {
        private readonly HttpClient _httpClient;

        public ImageParserService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<List<string>> ParseGoogleImages(string colorName, int maxResults = 10)
        {
            var imageUrls = new List<string>();

            try
            {
                var searchQuery = $"{colorName} background";
                var encodedQuery = HttpUtility.UrlEncode(searchQuery);
                var url = $"https://www.google.com/search?q={encodedQuery}&tbm=isch";

                var response = await _httpClient.GetStringAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                // Парсинг изображений из результатов поиска
                var imgNodes = doc.DocumentNode.SelectNodes("//img");

                if (imgNodes != null)
                {
                    foreach (var img in imgNodes.Take(maxResults))
                    {
                        var src = img.GetAttributeValue("src", "");
                        if (!string.IsNullOrEmpty(src) && src.StartsWith("http"))
                        {
                            imageUrls.Add(src);
                        }
                    }
                }

                // Альтернативный метод - генерация образца цвета
                if (imageUrls.Count == 0)
                {
                    imageUrls = GenerateColorSamples(colorName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                imageUrls = GenerateColorSamples(colorName);
            }

            return imageUrls;
        }

        private List<string> GenerateColorSamples(string colorName)
        {
            // Возвращаем URL-адреса с цветными изображениями через placeholder сервис
            var colors = new Dictionary<string, string>
            {
                { "розовый", "FFC0CB" },
                { "чёрный", "000000" },
                { "синий", "0000FF" },
                { "зелёный", "00FF00" },
                { "красный", "FF0000" },
                { "белый", "FFFFFF" },
                { "жёлтый", "FFFF00" }
            };

            var colorCode = colors.ContainsKey(colorName.ToLower())
                ? colors[colorName.ToLower()]
                : "808080";

            return new List<string>
            {
                $"https://via.placeholder.com/800x600/{colorCode}/{colorCode}",
                $"https://via.placeholder.com/1024x768/{colorCode}/{colorCode}",
                $"https://via.placeholder.com/1920x1080/{colorCode}/{colorCode}"
            };
        }
    }
}