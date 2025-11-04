using SQLite;
using parserColorBackground.Models;

namespace parserColorBackground.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;
        private const string CURRENT_SPLASH_KEY = "CurrentSplash";
        private const string CURRENT_COLOR_KEY = "CurrentColor";
        private const string CURRENT_BACKGROUND_IMAGE_KEY = "CurrentBackgroundImage";
        private const string CURRENT_SPLASH_IMAGE_KEY = "CurrentSplashImage";

        public DatabaseService()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "colors.db3");
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<ColorOption>().Wait();
            _database.CreateTableAsync<SplashOption>().Wait();
            _database.CreateTableAsync<SavedBackground>().Wait();
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
                // Теперь ImageUrl можно оставить пустым или null - изображения будут парситься
                _database.InsertAsync(new SplashOption
                {
                    SplashName = "Пустыня",
                    ImageUrl = null, // Будет парситься из интернета
                    IsActive = true
                }).Wait();

                _database.InsertAsync(new SplashOption
                {
                    SplashName = "Джунгли",
                    ImageUrl = null,
                    IsActive = true
                }).Wait();

                _database.InsertAsync(new SplashOption
                {
                    SplashName = "Город",
                    ImageUrl = null,
                    IsActive = true
                }).Wait();

                _database.InsertAsync(new SplashOption
                {
                    SplashName = "Океан",
                    ImageUrl = null,
                    IsActive = true
                }).Wait();

                _database.InsertAsync(new SplashOption
                {
                    SplashName = "Горы",
                    ImageUrl = null,
                    IsActive = true
                }).Wait();

                _database.InsertAsync(new SplashOption
                {
                    SplashName = "Космос",
                    ImageUrl = null,
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

        #region SavedBackground Methods

        public async Task SaveCurrentBackgroundAsync(string imageUrl, string colorName)
        {
            try
            {
                var allBackgrounds = await _database.Table<SavedBackground>().ToListAsync();
                foreach (var bg in allBackgrounds)
                {
                    bg.IsCurrent = false;
                    await _database.UpdateAsync(bg);
                }

                var newBackground = new SavedBackground
                {
                    ImageUrl = imageUrl,
                    ColorName = colorName,
                    IsCurrent = true,
                    SavedAt = DateTime.Now
                };

                await _database.InsertAsync(newBackground);

                Preferences.Default.Set(CURRENT_BACKGROUND_IMAGE_KEY, imageUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving background: {ex.Message}");
            }
        }

        public async Task<SavedBackground> GetCurrentBackgroundAsync()
        {
            try
            {
                var current = await _database.Table<SavedBackground>()
                                             .Where(b => b.IsCurrent)
                                             .FirstOrDefaultAsync();
                return current;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current background: {ex.Message}");
                return null;
            }
        }

        public string GetCurrentBackgroundImageUrl()
        {
            try
            {
                return Preferences.Default.Get(CURRENT_BACKGROUND_IMAGE_KEY, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting background image url: {ex.Message}");
                return string.Empty;
            }
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
                    else if (sqlLower.Contains("savedbackgrounds"))
                    {
                        var result = await _database.QueryAsync<SavedBackground>(sql);
                        return FormatQueryResult(result);
                    }
                    else
                    {
                        return "❌ Неизвестная таблица.\n\nДоступные таблицы:\n- ColorOptions\n- SplashOptions\n- SavedBackgrounds";
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
                else if (item is SavedBackground background)
                {
                    output += $"🌄 ID: {background.Id}\n";
                    output += $"   Цвет: {background.ColorName}\n";
                    output += $"   URL: {background.ImageUrl}\n";
                    output += $"   Текущий: {(background.IsCurrent ? "✓" : "✗")}\n";
                    output += $"   Сохранен: {background.SavedAt:dd.MM.yyyy HH:mm}\n\n";
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

        // Новые методы для сохранения конкретного изображения заставки
        public void SetCurrentSplashImageUrl(string imageUrl)
        {
            try
            {
                Preferences.Default.Set(CURRENT_SPLASH_IMAGE_KEY, imageUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting current splash image: {ex.Message}");
            }
        }

        public string GetCurrentSplashImageUrl()
        {
            try
            {
                return Preferences.Default.Get(CURRENT_SPLASH_IMAGE_KEY, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current splash image: {ex.Message}");
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

        public void ClearPreferences()
        {
            try
            {
                Preferences.Default.Remove(CURRENT_SPLASH_KEY);
                Preferences.Default.Remove(CURRENT_COLOR_KEY);
                Preferences.Default.Remove(CURRENT_BACKGROUND_IMAGE_KEY);
                Preferences.Default.Remove(CURRENT_SPLASH_IMAGE_KEY);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing preferences: {ex.Message}");
            }
        }

        public async Task UpdateSplashesToNewThemes()
        {
            try
            {
                // Удаляем старые заставки
                await _database.ExecuteAsync("DELETE FROM SplashOptions");

                // Добавляем новые тематические заставки
                await _database.InsertAsync(new SplashOption
                {
                    SplashName = "Пустыня",
                    ImageUrl = "https://via.placeholder.com/800x600/F4A460/FFFFFF?text=Desert",
                    IsActive = true
                });

                await _database.InsertAsync(new SplashOption
                {
                    SplashName = "Джунгли",
                    ImageUrl = "https://via.placeholder.com/800x600/228B22/FFFFFF?text=Jungle",
                    IsActive = true
                });

                await _database.InsertAsync(new SplashOption
                {
                    SplashName = "Город",
                    ImageUrl = "https://via.placeholder.com/800x600/708090/FFFFFF?text=City",
                    IsActive = true
                });

                await _database.InsertAsync(new SplashOption
                {
                    SplashName = "Океан",
                    ImageUrl = "https://via.placeholder.com/800x600/1E90FF/FFFFFF?text=Ocean",
                    IsActive = true
                });

                await _database.InsertAsync(new SplashOption
                {
                    SplashName = "Горы",
                    ImageUrl = "https://via.placeholder.com/800x600/A9A9A9/FFFFFF?text=Mountains",
                    IsActive = true
                });

                await _database.InsertAsync(new SplashOption
                {
                    SplashName = "Космос",
                    ImageUrl = "https://via.placeholder.com/800x600/000080/FFFFFF?text=Space",
                    IsActive = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления заставок: {ex.Message}");
            }
        }

        public async Task<int> AddSplashByNameAsync(string splashName)
        {
            try
            {
                var newSplash = new SplashOption
                {
                    SplashName = splashName,
                    ImageUrl = null, // Изображения будут парситься автоматически
                    IsActive = true
                };

                return await _database.InsertAsync(newSplash);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding splash: {ex.Message}");
                return 0;
            }
        }

        // Проверить существует ли заставка
        public async Task<bool> SplashExistsAsync(string splashName)
        {
            try
            {
                var splash = await _database.Table<SplashOption>()
                                            .Where(s => s.SplashName.ToLower() == splashName.ToLower())
                                            .FirstOrDefaultAsync();
                return splash != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking splash: {ex.Message}");
                return false;
            }
        }
        #endregion
    }
}