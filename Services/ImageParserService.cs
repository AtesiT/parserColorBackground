using HtmlAgilityPack;
using System.Web;

namespace parserColorBackground.Services
{
    public class ImageParserService
    {
        private readonly HttpClient _httpClient;
        private JintService _jintService; // Добавляем JINT

        public ImageParserService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        // Метод для установки JINT сервиса
        public void SetJintService(JintService jintService)
        {
            _jintService = jintService;
        }

        public async Task<List<string>> ParseGoogleImages(string colorName, int maxResults = 10)
        {
            var imageUrls = new List<string>();

            try
            {
                _jintService?.ExecuteScript($"log('🔍 Начало парсинга изображений для цвета: {colorName}')");

                var searchQuery = $"{colorName} background wallpaper 4k";

                // Используем JINT для формирования запроса
                _jintService?.SetValue("color", colorName);
                _jintService?.SetValue("maxCount", maxResults);
                var queryScript = "var query = color + ' background wallpaper 4k'; log('Поисковый запрос: ' + query); query;";
                var query = _jintService?.ExecuteScript(queryScript) ?? searchQuery;

                var encodedQuery = HttpUtility.UrlEncode(searchQuery);
                var url = $"https://www.google.com/search?q={encodedQuery}&tbm=isch";

                _jintService?.ExecuteScript($"log('🌐 URL для парсинга: {url}')");

                var response = await _httpClient.GetStringAsync(url);

                _jintService?.ExecuteScript($"log('✅ Получен ответ от сервера, размер: {response.Length} символов')");

                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                var imgNodes = doc.DocumentNode.SelectNodes("//img");

                if (imgNodes != null)
                {
                    _jintService?.ExecuteScript($"log('📊 Найдено img узлов: {imgNodes.Count}')");

                    int parsedCount = 0;
                    foreach (var img in imgNodes.Take(maxResults))
                    {
                        var src = img.GetAttributeValue("src", "");
                        if (!string.IsNullOrEmpty(src) && src.StartsWith("http"))
                        {
                            imageUrls.Add(src);
                            parsedCount++;

                            // Логируем через JINT каждое найденное изображение
                            _jintService?.ExecuteScript($"log('🖼️ Изображение #{parsedCount}: {src.Substring(0, Math.Min(50, src.Length))}...')");
                        }
                    }

                    _jintService?.ExecuteScript($"info('✅ Парсинг завершен. Найдено {parsedCount} изображений')");
                }
                else
                {
                    _jintService?.ExecuteScript("warn('⚠️ Не найдено img узлов')");
                }

                if (imageUrls.Count == 0)
                {
                    _jintService?.ExecuteScript("warn('⚠️ Используются placeholder изображения')");
                    imageUrls = GenerateColorSamples(colorName);
                }
            }
            catch (Exception ex)
            {
                _jintService?.ExecuteScript($"error('❌ Ошибка парсинга: {ex.Message}')");
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                imageUrls = GenerateColorSamples(colorName);
            }

            return imageUrls;
        }

        public async Task<List<string>> ParseHighQualityWallpapers(string theme, int maxResults = 15)
        {
            var imageUrls = new List<string>();

            try
            {
                _jintService?.ExecuteScript($"log('🔍 Начало парсинга HD wallpaper для темы: {theme}')");

                // Используем JINT для определения типа поиска
                _jintService?.SetValue("theme", theme);
                var searchQueryScript = @"
                    var searchQueries = {
                        'пустыня': 'desert landscape wallpaper 4k hd',
                        'джунгли': 'jungle tropical forest wallpaper 4k hd',
                        'город': 'city skyline urban wallpaper 4k hd night',
                        'океан': 'ocean sea waves wallpaper 4k hd blue',
                        'горы': 'mountains peaks landscape wallpaper 4k hd',
                        'космос': 'space galaxy stars nebula wallpaper 4k hd'
                    };
                    
                    var themeLower = theme.toLowerCase();
                    var query = searchQueries[themeLower] || (theme + ' landscape wallpaper 4k hd');
                    log('📝 Сформирован запрос для темы ' + theme + ': ' + query);
                    query;
                ";

                var searchQuery = _jintService?.ExecuteScript(searchQueryScript) ?? $"{theme} landscape wallpaper 4k hd";

                var encodedQuery = HttpUtility.UrlEncode(searchQuery);
                var url = $"https://www.google.com/search?q={encodedQuery}&tbm=isch&tbs=isz:l";

                _jintService?.ExecuteScript($"log('🌐 URL для парсинга HD: {url}')");

                var response = await _httpClient.GetStringAsync(url);

                _jintService?.ExecuteScript($"log('✅ Получен ответ, размер: {response.Length} символов')");

                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                var imgNodes = doc.DocumentNode.SelectNodes("//img");

                if (imgNodes != null)
                {
                    _jintService?.ExecuteScript($"log('📊 Найдено img узлов: {imgNodes.Count}')");

                    int parsedCount = 0;
                    foreach (var img in imgNodes.Take(maxResults))
                    {
                        var src = img.GetAttributeValue("src", "");
                        if (!string.IsNullOrEmpty(src) && src.StartsWith("http"))
                        {
                            imageUrls.Add(src);
                            parsedCount++;

                            _jintService?.ExecuteScript($"log('🖼️ HD Wallpaper #{parsedCount}')");
                        }
                    }

                    // Используем JINT для подсчета результатов
                    _jintService?.SetValue("parsedCount", parsedCount);
                    _jintService?.SetValue("expectedCount", maxResults);
                    var analysisScript = @"
                        var percentage = (parsedCount / expectedCount * 100).toFixed(2);
                        var message = '✅ Парсинг завершен. Найдено ' + parsedCount + ' из ' + expectedCount + ' (' + percentage + '%)';
                        info(message);
                        message;
                    ";
                    _jintService?.ExecuteScript(analysisScript);
                }
                else
                {
                    _jintService?.ExecuteScript("warn('⚠️ Не найдено img узлов для HD wallpaper')");
                }

                if (imageUrls.Count == 0)
                {
                    _jintService?.ExecuteScript("warn('⚠️ Используются placeholder HD wallpaper')");
                    imageUrls = GenerateSplashSamples(theme);
                }
            }
            catch (Exception ex)
            {
                _jintService?.ExecuteScript($"error('❌ Ошибка парсинга HD: {ex.Message}')");
                Console.WriteLine($"Ошибка парсинга HD обоев: {ex.Message}");
                imageUrls = GenerateSplashSamples(theme);
            }

            return imageUrls;
        }

        private List<string> GenerateColorSamples(string colorName)
        {
            _jintService?.ExecuteScript($"log('🎨 Генерация placeholder для цвета: {colorName}')");

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
                { "коричневый", "8B4513" },
                { "голубой", "87CEEB" },
                { "бирюзовый", "40E0D0" },
                { "малиновый", "DC143C" }
            };

            var colorCode = colors.ContainsKey(colorName.ToLower())
                ? colors[colorName.ToLower()]
                : "808080";

            _jintService?.ExecuteScript($"log('🎨 Цветовой код для {colorName}: #{colorCode}')");

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
            _jintService?.ExecuteScript($"log('🖼️ Генерация placeholder wallpaper для: {splashName}')");

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
                { "деревня", new List<string>
                    {
                        "https://via.placeholder.com/1920x1080/8B4513/F5DEB3?text=Village+Wallpaper+4K",
                        "https://via.placeholder.com/2560x1440/A0522D/FFDEAD?text=Countryside+HD",
                    }
                },
                { "лес", new List<string>
                    {
                        "https://via.placeholder.com/1920x1080/228B22/006400?text=Forest+Wallpaper+4K",
                        "https://via.placeholder.com/2560x1440/2E8B57/90EE90?text=Green+Forest+HD",
                    }
                },
                { "пляж", new List<string>
                    {
                        "https://via.placeholder.com/1920x1080/87CEEB/F0E68C?text=Beach+Wallpaper+4K",
                        "https://via.placeholder.com/2560x1440/87CEFA/FFE4B5?text=Sandy+Beach+HD",
                    }
                },
                { "закат", new List<string>
                    {
                        "https://via.placeholder.com/1920x1080/FF4500/FFD700?text=Sunset+Wallpaper+4K",
                        "https://via.placeholder.com/2560x1440/FF6347/FFA500?text=Beautiful+Sunset+HD",
                    }
                }
            };

            var key = splashName.ToLower();
            if (samples.ContainsKey(key))
            {
                return samples[key];
            }

            return new List<string>
            {
                $"https://via.placeholder.com/1920x1080/607D8B/FFFFFF?text={Uri.EscapeDataString(splashName)}+Wallpaper+4K",
                $"https://via.placeholder.com/2560x1440/546E7A/FFFFFF?text={Uri.EscapeDataString(splashName)}+HD",
                $"https://placehold.co/1920x1080/455A64/FFFFFF?text={Uri.EscapeDataString(splashName)}+UHD",
                $"https://placehold.co/2560x1440/37474F/FFFFFF?text={Uri.EscapeDataString(splashName)}+4K"
            };
        }
    }
}