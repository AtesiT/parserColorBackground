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
                var searchQuery = $"{colorName} background wallpaper 4k";
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
                // Формируем поисковый запрос для wallpaper с высоким качеством
                var searchQuery = splashName.ToLower() switch
                {
                    "пустыня" => "desert landscape wallpaper 4k hd",
                    "джунгли" => "jungle tropical forest wallpaper 4k hd",
                    "город" => "city skyline urban wallpaper 4k hd night",
                    "океан" => "ocean sea waves wallpaper 4k hd blue",
                    "горы" => "mountains peaks landscape wallpaper 4k hd",
                    "космос" => "space galaxy stars nebula wallpaper 4k hd",
                    _ => $"{splashName} landscape wallpaper 4k hd"
                };

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
                { "черный", "000000" },
                { "синий", "0000FF" },
                { "зелёный", "00FF00" },
                { "зеленый", "00FF00" },
                { "красный", "FF0000" },
                { "белый", "FFFFFF" },
                { "жёлтый", "FFFF00" },
                { "желтый", "FFFF00" },
                { "оранжевый", "FFA500" },
                { "фиолетовый", "9370DB" },
                { "серый", "808080" },
                { "коричневый", "8B4513" }
            };

            var colorCode = colors.ContainsKey(colorName.ToLower())
                ? colors[colorName.ToLower()]
                : "808080";

            return new List<string>
            {
                $"https://via.placeholder.com/1920x1080/{colorCode}/{colorCode}?text=Wallpaper",
                $"https://via.placeholder.com/2560x1440/{colorCode}/{colorCode}?text=HD+Wallpaper",
                $"https://placehold.co/1920x1080/{colorCode}/{colorCode}?text=4K+Wallpaper",
                $"https://placehold.co/2560x1440/{colorCode}/{colorCode}?text=Ultra+HD",
                $"https://via.placeholder.com/3840x2160/{colorCode}/{colorCode}?text=4K+Ultra"
            };
        }

        private List<string> GenerateSplashSamples(string splashName)
        {
            var samples = new Dictionary<string, List<string>>
    {
        { "пустыня", new List<string>
            {
                "https://via.placeholder.com/1920x1080/F4A460/8B4513?text=Desert+Wallpaper+4K",
                "https://via.placeholder.com/2560x1440/DEB887/8B4513?text=Sand+Dunes+HD",
                "https://placehold.co/1920x1080/D2691E/FFE4B5?text=Sahara+Desert",
            }
        },
        { "джунгли", new List<string>
            {
                "https://via.placeholder.com/1920x1080/228B22/006400?text=Jungle+Wallpaper+4K",
                "https://via.placeholder.com/2560x1440/32CD32/006400?text=Tropical+Forest+HD",
            }
        },
        { "город", new List<string>
            {
                "https://via.placeholder.com/1920x1080/708090/2F4F4F?text=City+Wallpaper+4K",
                "https://via.placeholder.com/2560x1440/696969/D3D3D3?text=Urban+Skyline+HD",
            }
        },
        { "океан", new List<string>
            {
                "https://via.placeholder.com/1920x1080/1E90FF/00008B?text=Ocean+Wallpaper+4K",
                "https://via.placeholder.com/2560x1440/4169E1/000080?text=Blue+Ocean+HD",
            }
        },
        { "горы", new List<string>
            {
                "https://via.placeholder.com/1920x1080/A9A9A9/2F4F4F?text=Mountain+Wallpaper+4K",
                "https://via.placeholder.com/2560x1440/808080/FFFAF0?text=Snow+Peaks+HD",
            }
        },
        { "космос", new List<string>
            {
                "https://via.placeholder.com/1920x1080/000080/4169E1?text=Space+Wallpaper+4K",
                "https://via.placeholder.com/2560x1440/191970/9370DB?text=Galaxy+HD",
            }
        },
        // Добавим новые темы
        { "деревня", new List<string>
            {
                "https://via.placeholder.com/1920x1080/8B4513/F5DEB3?text=Village+Wallpaper+4K",
                "https://via.placeholder.com/2560x1440/A0522D/FFDEAD?text=Countryside+HD",
                "https://placehold.co/1920x1080/CD853F/FFE4B5?text=Rural+Village",
            }
        },
        { "лес", new List<string>
            {
                "https://via.placeholder.com/1920x1080/228B22/006400?text=Forest+Wallpaper+4K",
                "https://via.placeholder.com/2560x1440/2E8B57/90EE90?text=Green+Forest+HD",
                "https://placehold.co/1920x1080/3CB371/98FB98?text=Woods",
            }
        },
        { "пляж", new List<string>
            {
                "https://via.placeholder.com/1920x1080/87CEEB/F0E68C?text=Beach+Wallpaper+4K",
                "https://via.placeholder.com/2560x1440/87CEFA/FFE4B5?text=Sandy+Beach+HD",
                "https://placehold.co/1920x1080/00BFFF/F5DEB3?text=Tropical+Beach",
            }
        },
        { "закат", new List<string>
            {
                "https://via.placeholder.com/1920x1080/FF4500/FFD700?text=Sunset+Wallpaper+4K",
                "https://via.placeholder.com/2560x1440/FF6347/FFA500?text=Beautiful+Sunset+HD",
                "https://placehold.co/1920x1080/FF7F50/FFDB58?text=Golden+Sunset",
            }
        }
    };

            var key = splashName.ToLower();
            if (samples.ContainsKey(key))
            {
                return samples[key];
            }

            // Универсальный fallback для ЛЮБОГО названия
            return new List<string>
    {
        $"https://via.placeholder.com/1920x1080/607D8B/FFFFFF?text={Uri.EscapeDataString(splashName)}+Wallpaper+4K",
        $"https://via.placeholder.com/2560x1440/546E7A/FFFFFF?text={Uri.EscapeDataString(splashName)}+HD",
        $"https://placehold.co/1920x1080/455A64/FFFFFF?text={Uri.EscapeDataString(splashName)}+UHD",
        $"https://placehold.co/2560x1440/37474F/FFFFFF?text={Uri.EscapeDataString(splashName)}+4K"
    };
        }

        // Дополнительный метод для получения обоев высокого качества
        public async Task<List<string>> ParseHighQualityWallpapers(string theme, int maxResults = 15)
        {
            var imageUrls = new List<string>();

            try
            {
                // Более специфичный поиск для wallpaper
                var searchQuery = $"{theme} wallpaper 4k ultra hd desktop background";
                var encodedQuery = HttpUtility.UrlEncode(searchQuery);
                var url = $"https://www.google.com/search?q={encodedQuery}&tbm=isch&tbs=isz:l"; // isz:l - большие изображения

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
                    imageUrls = GenerateSplashSamples(theme);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка парсинга HD обоев: {ex.Message}");
                imageUrls = GenerateSplashSamples(theme);
            }

            return imageUrls;
        }
    }
}