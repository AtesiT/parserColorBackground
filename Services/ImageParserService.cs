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

        public async Task<List<string>> ParseSplashImages(string splashName, int maxResults = 10)
        {
            var imageUrls = new List<string>();

            try
            {
                var searchQuery = $"{splashName} splash screen app";
                var encodedQuery = HttpUtility.UrlEncode(searchQuery);
                var url = $"https://www.google.com/search?q={encodedQuery}&tbm=isch";

                var response = await _httpClient.GetStringAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(response);

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

                if (imageUrls.Count == 0)
                {
                    imageUrls = GenerateSplashSamples(splashName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка парсинга заставок: {ex.Message}");
                imageUrls = GenerateSplashSamples(splashName);
            }

            return imageUrls;
        }

        private List<string> GenerateColorSamples(string colorName)
        {
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
                $"https://via.placeholder.com/1920x1080/{colorCode}/{colorCode}",
                $"https://placehold.co/800x600/{colorCode}/{colorCode}",
                $"https://placehold.co/1024x768/{colorCode}/{colorCode}"
            };
        }

        private List<string> GenerateSplashSamples(string splashName)
        {
            var samples = new Dictionary<string, List<string>>
            {
                {
                    "логотип компании",
                    new List<string>
                    {
                        "https://via.placeholder.com/800x600/4CAF50/FFFFFF?text=Company+Logo",
                        "https://via.placeholder.com/1080x1920/4CAF50/FFFFFF?text=Welcome",
                        "https://placehold.co/800x600/4CAF50/FFFFFF?text=Logo",
                        "https://placehold.co/1080x1920/2E7D32/FFFFFF?text=Company"
                    }
                },
                {
                    "приветствие",
                    new List<string>
                    {
                        "https://via.placeholder.com/800x600/2196F3/FFFFFF?text=Welcome",
                        "https://via.placeholder.com/1080x1920/2196F3/FFFFFF?text=Hello",
                        "https://placehold.co/800x600/1976D2/FFFFFF?text=Welcome",
                        "https://placehold.co/1080x1920/0D47A1/FFFFFF?text=Greetings"
                    }
                },
                {
                    "загрузка",
                    new List<string>
                    {
                        "https://via.placeholder.com/800x600/FF9800/FFFFFF?text=Loading...",
                        "https://via.placeholder.com/1080x1920/FF9800/FFFFFF?text=Please+Wait",
                        "https://placehold.co/800x600/F57C00/FFFFFF?text=Loading",
                        "https://placehold.co/1080x1920/E65100/FFFFFF?text=Wait"
                    }
                },
                {
                    "старт",
                    new List<string>
                    {
                        "https://via.placeholder.com/800x600/9C27B0/FFFFFF?text=Start",
                        "https://via.placeholder.com/1080x1920/9C27B0/FFFFFF?text=Begin",
                        "https://placehold.co/800x600/7B1FA2/FFFFFF?text=Start",
                        "https://placehold.co/1080x1920/4A148C/FFFFFF?text=Launch"
                    }
                }
            };

            var key = splashName.ToLower();
            if (samples.ContainsKey(key))
            {
                return samples[key];
            }

            // Генерируем случайные заставки если не найдено
            return new List<string>
            {
                $"https://via.placeholder.com/800x600/607D8B/FFFFFF?text={Uri.EscapeDataString(splashName)}",
                $"https://via.placeholder.com/1080x1920/607D8B/FFFFFF?text={Uri.EscapeDataString(splashName)}",
                $"https://placehold.co/800x600/455A64/FFFFFF?text={Uri.EscapeDataString(splashName)}",
                $"https://placehold.co/1080x1920/37474F/FFFFFF?text={Uri.EscapeDataString(splashName)}"
            };
        }
    }
}