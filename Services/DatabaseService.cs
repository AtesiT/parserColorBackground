using SQLite;
using parserColorBackground.Models;

namespace parserColorBackground.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;
        private const string CURRENT_SPLASH_KEY = "CurrentSplash";
        private const string CURRENT_COLOR_KEY = "CurrentColor";

        public DatabaseService()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "colors.db3");
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<ColorOption>().Wait();
            _database.CreateTableAsync<SplashOption>().Wait();
            InitializeDefaultColors();
            InitializeDefaultSplashes();
        }

        private void InitializeDefaultColors()
        {
            var count = _database.Table<ColorOption>().CountAsync().Result;
            if (count == 0)
            {
                _database.InsertAsync(new ColorOption { ColorName = "розовый", IsActive = true }).Wait();
                _database.InsertAsync(new ColorOption { ColorName = "чёрный", IsActive = true }).Wait();
                _database.InsertAsync(new ColorOption { ColorName = "синий", IsActive = true }).Wait();
                _database.InsertAsync(new ColorOption { ColorName = "зелёный", IsActive = true }).Wait();
                _database.InsertAsync(new ColorOption { ColorName = "красный", IsActive = true }).Wait();
                _database.InsertAsync(new ColorOption { ColorName = "белый", IsActive = true }).Wait();
            }
        }

        private void InitializeDefaultSplashes()
        {
            var count = _database.Table<SplashOption>().CountAsync().Result;
            if (count == 0)
            {
                _database.InsertAsync(new SplashOption
                {
                    SplashName = "Логотип компании",
                    ImageUrl = "https://via.placeholder.com/800x600/4CAF50/FFFFFF?text=Company+Logo",
                    IsActive = true
                }).Wait();

                _database.InsertAsync(new SplashOption
                {
                    SplashName = "Приветствие",
                    ImageUrl = "https://via.placeholder.com/800x600/2196F3/FFFFFF?text=Welcome",
                    IsActive = true
                }).Wait();

                _database.InsertAsync(new SplashOption
                {
                    SplashName = "Загрузка",
                    ImageUrl = "https://via.placeholder.com/800x600/FF9800/FFFFFF?text=Loading...",
                    IsActive = true
                }).Wait();

                _database.InsertAsync(new SplashOption
                {
                    SplashName = "Старт",
                    ImageUrl = "https://via.placeholder.com/800x600/9C27B0/FFFFFF?text=Start",
                    IsActive = true
                }).Wait();
            }
        }

        #region ColorOption Methods

        public Task<List<ColorOption>> GetActiveColorsAsync()
        {
            return _database.Table<ColorOption>()
                           .Where(c => c.IsActive)
                           .ToListAsync();
        }

        public Task<List<ColorOption>> GetAllColorsAsync()
        {
            return _database.Table<ColorOption>().ToListAsync();
        }

        public Task<int> AddColorAsync(ColorOption color)
        {
            return _database.InsertAsync(color);
        }

        public Task<int> UpdateColorAsync(ColorOption color)
        {
            return _database.UpdateAsync(color);
        }

        public Task<int> DeleteColorAsync(ColorOption color)
        {
            return _database.DeleteAsync(color);
        }

        #endregion

        #region SplashOption Methods

        public Task<List<SplashOption>> GetActiveSplashesAsync()
        {
            return _database.Table<SplashOption>()
                           .Where(s => s.IsActive)
                           .ToListAsync();
        }

        public Task<List<SplashOption>> GetAllSplashesAsync()
        {
            return _database.Table<SplashOption>().ToListAsync();
        }

        public Task<int> AddSplashAsync(SplashOption splash)
        {
            return _database.InsertAsync(splash);
        }

        public Task<int> UpdateSplashAsync(SplashOption splash)
        {
            return _database.UpdateAsync(splash);
        }

        public Task<int> DeleteSplashAsync(SplashOption splash)
        {
            return _database.DeleteAsync(splash);
        }

        #endregion

        #region SQL Execution

        public async Task<string> ExecuteRawSqlAsync(string sql)
        {
            try
            {
                sql = sql.Trim();
                var sqlLower = sql.ToLower();

                if (sqlLower.StartsWith("select"))
                {
                    if (sqlLower.Contains("coloroptions"))
                    {
                        var result = await _database.QueryAsync<ColorOption>(sql);
                        return FormatQueryResult(result);
                    }
                    else if (sqlLower.Contains("splashoptions"))
                    {
                        var result = await _database.QueryAsync<SplashOption>(sql);
                        return FormatQueryResult(result);
                    }
                    else
                    {
                        return "❌ Неизвестная таблица.\n\nДоступные таблицы:\n- ColorOptions\n- SplashOptions";
                    }
                }
                else if (sqlLower.StartsWith("insert") || sqlLower.StartsWith("update") || sqlLower.StartsWith("delete"))
                {
                    var result = await _database.ExecuteAsync(sql);
                    return $"✅ Выполнено успешно\nЗатронуто строк: {result}";
                }
                else
                {
                    return "❌ Поддерживаются только:\n- SELECT\n- INSERT\n- UPDATE\n- DELETE";
                }
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка SQL:\n{ex.Message}";
            }
        }

        private string FormatQueryResult<T>(List<T> results)
        {
            if (results.Count == 0)
                return "📭 Результатов не найдено";

            var output = $"📊 Найдено записей: {results.Count}\n\n";

            foreach (var item in results)
            {
                if (item is ColorOption color)
                {
                    output += $"🎨 ID: {color.Id}\n";
                    output += $"   Цвет: {color.ColorName}\n";
                    output += $"   Активен: {(color.IsActive ? "✓" : "✗")}\n\n";
                }
                else if (item is SplashOption splash)
                {
                    output += $"🖼️ ID: {splash.Id}\n";
                    output += $"   Название: {splash.SplashName}\n";
                    output += $"   URL: {splash.ImageUrl}\n";
                    output += $"   Активен: {(splash.IsActive ? "✓" : "✗")}\n\n";
                }
            }

            return output;
        }

        #endregion

        #region Preferences Management

        public async Task<SplashOption> GetCurrentSplashAsync()
        {
            try
            {
                var savedSplashName = Preferences.Default.Get(CURRENT_SPLASH_KEY, string.Empty);

                if (!string.IsNullOrEmpty(savedSplashName))
                {
                    var savedSplash = await _database.Table<SplashOption>()
                                                     .Where(s => s.SplashName == savedSplashName && s.IsActive)
                                                     .FirstOrDefaultAsync();

                    if (savedSplash != null)
                        return savedSplash;
                }

                var splashes = await GetActiveSplashesAsync();
                return splashes.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current splash: {ex.Message}");
                return null;
            }
        }

        public void SetCurrentSplashAsync(string splashName)
        {
            try
            {
                Preferences.Default.Set(CURRENT_SPLASH_KEY, splashName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting current splash: {ex.Message}");
            }
        }

        public string GetCurrentSplashName()
        {
            try
            {
                return Preferences.Default.Get(CURRENT_SPLASH_KEY, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current splash name: {ex.Message}");
                return string.Empty;
            }
        }

        public void SetCurrentColor(string colorName)
        {
            try
            {
                Preferences.Default.Set(CURRENT_COLOR_KEY, colorName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting current color: {ex.Message}");
            }
        }

        public string GetCurrentColor()
        {
            try
            {
                return Preferences.Default.Get(CURRENT_COLOR_KEY, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current color: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<ColorOption> GetCurrentColorAsync()
        {
            try
            {
                var savedColorName = GetCurrentColor();

                if (!string.IsNullOrEmpty(savedColorName))
                {
                    var savedColor = await _database.Table<ColorOption>()
                                                    .Where(c => c.ColorName == savedColorName && c.IsActive)
                                                    .FirstOrDefaultAsync();

                    if (savedColor != null)
                        return savedColor;
                }

                var colors = await GetActiveColorsAsync();
                return colors.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current color: {ex.Message}");
                return null;
            }
        }

        public void ClearPreferences()
        {
            try
            {
                Preferences.Default.Remove(CURRENT_SPLASH_KEY);
                Preferences.Default.Remove(CURRENT_COLOR_KEY);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing preferences: {ex.Message}");
            }
        }

        public void ClearCurrentSplash()
        {
            try
            {
                Preferences.Default.Remove(CURRENT_SPLASH_KEY);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing current splash: {ex.Message}");
            }
        }

        public void ClearCurrentColor()
        {
            try
            {
                Preferences.Default.Remove(CURRENT_COLOR_KEY);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing current color: {ex.Message}");
            }
        }

        #endregion
    }
}