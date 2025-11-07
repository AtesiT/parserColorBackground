using parserColorBackground.Services;
using parserColorBackground.Models;

namespace parserColorBackground
{
    public partial class MainPage : ContentPage
    {
        private readonly DatabaseService _databaseService;
        private readonly ImageParserService _imageParserService;
        private readonly JintService _jintService;
        private bool _isShowingSplashes = false;
        private string _currentSelectedImageUrl = string.Empty;

        public MainPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _imageParserService = new ImageParserService();
            _jintService = new JintService();

            // Подписываемся на вывод консоли JINT
            _jintService.ConsoleOutput += OnJintConsoleOutput;

            // Подписываемся на сообщение о копировании SQL
            MessagingCenter.Subscribe<SqlExamplesPage, string>(this, "SqlCopied", (sender, sql) =>
            {
                SqlQueryEditor.Text = sql;
            });

            LoadSavedSettings();
        }

        private void OnJintConsoleOutput(object sender, string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                JintConsoleLabel.Text += message + "\n";
                JintConsoleFrame.IsVisible = true;
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadSavedSettings();
        }

        private async void LoadSavedSettings()
        {
            try
            {
                _jintService.ExecuteScript("log('🔄 Загрузка сохраненных настроек...')");

                var savedBackground = await _databaseService.GetCurrentBackgroundAsync();

                if (savedBackground != null && !string.IsNullOrEmpty(savedBackground.ImageUrl))
                {
                    this.BackgroundImageSource = ImageSource.FromUri(new Uri(savedBackground.ImageUrl));
                    SelectedColorLabel.Text = $"Текущий фон: {savedBackground.ColorName}";
                    _currentSelectedImageUrl = savedBackground.ImageUrl;

                    _jintService.ExecuteScript($"info('✅ Фон загружен: {savedBackground.ColorName}')");
                }
                else
                {
                    var savedColor = _databaseService.GetCurrentColor();
                    if (!string.IsNullOrEmpty(savedColor))
                    {
                        var color = GetColorFromName(savedColor);
                        this.BackgroundColor = color;
                        SelectedColorLabel.Text = $"Текущий цвет: {savedColor}";

                        var contrastColor = GetContrastColor(color);
                        UpdateTextColors(contrastColor);

                        _jintService.ExecuteScript($"info('✅ Цвет загружен: {savedColor}')");
                    }
                }

                var savedSplashName = _databaseService.GetCurrentSplashName();
                if (!string.IsNullOrEmpty(savedSplashName))
                {
                    SelectedSplashLabel.Text = $"Текущая заставка: {savedSplashName}";
                    _jintService.ExecuteScript($"info('✅ Заставка загружена: {savedSplashName}')");
                }
            }
            catch (Exception ex)
            {
                _jintService.ExecuteScript($"error('❌ Ошибка загрузки: {ex.Message}')");
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        private void OnClearSqlClicked(object sender, EventArgs e)
        {
            SqlQueryEditor.Text = string.Empty;
            SqlResultFrame.IsVisible = false;
            _jintService.ExecuteScript("log('🗑️ SQL редактор очищен')");
        }

        private async void OnExecuteSqlClicked(object sender, EventArgs e)
        {
            var sql = SqlQueryEditor.Text?.Trim();

            if (string.IsNullOrWhiteSpace(sql))
            {
                await DisplayAlert("Ошибка", "Введите SQL запрос", "OK");
                return;
            }

            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                _jintService.ExecuteScript($"log('📝 Выполнение SQL: {sql}')");

                var result = await _databaseService.ExecuteRawSqlAsync(sql);

                SqlResultLabel.Text = result;
                SqlResultFrame.IsVisible = true;

                _jintService.SetValue("sqlQuery", sql);
                _jintService.SetValue("sqlResult", result);

                var analysis = _jintService.ExecuteScript(@"
                    var lines = sqlResult.split('\n').length;
                    var isSuccess = sqlResult.includes('✅');
                    var message = isSuccess 
                        ? 'Запрос выполнен успешно! Строк в результате: ' + lines
                        : 'Результат получен. Строк: ' + lines;
                    info(message);
                    message;
                ");

                _jintService.ExecuteScript("log('✅ SQL выполнен успешно')");
            }
            catch (Exception ex)
            {
                SqlResultLabel.Text = $"Ошибка: {ex.Message}";
                SqlResultFrame.IsVisible = true;
                _jintService.ExecuteScript($"error('❌ Ошибка SQL: {ex.Message}')");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnShowSqlExamplesClicked(object sender, EventArgs e)
        {
            try
            {
                _jintService.ExecuteScript("log('📚 Открытие примеров SQL...')");
                var examplesPage = new SqlExamplesPage();
                await Navigation.PushModalAsync(examplesPage);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть примеры: {ex.Message}", "OK");
                _jintService.ExecuteScript($"error('❌ Ошибка открытия примеров: {ex.Message}')");
            }
        }

        private async void OnViewModeClicked(object sender, EventArgs e)
        {
            _isShowingSplashes = !_isShowingSplashes;

            if (_isShowingSplashes)
            {
                ViewModeButton.Text = "🖼️ Заставки";
                CollectionTitleLabel.Text = "Доступные заставки";
                _jintService.ExecuteScript("log('🔄 Переключение на режим заставок')");
                await LoadSplashesPreview();
            }
            else
            {
                ViewModeButton.Text = "📸 Фоны";
                CollectionTitleLabel.Text = "Изображения фонов";
                ImagesCollectionView.ItemsSource = null;
                _jintService.ExecuteScript("log('🔄 Переключение на режим фонов')");
            }
        }

        private async Task LoadSplashesPreview()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                var splashes = await _databaseService.GetActiveSplashesAsync();
                _jintService.ExecuteScript($"log('🖼️ Загрузка {splashes.Count} заставок...')");

                var imageItems = new List<ImageItem>();

                foreach (var splash in splashes)
                {
                    var urls = await _imageParserService.ParseHighQualityWallpapers(splash.SplashName, 4);

                    foreach (var url in urls)
                    {
                        imageItems.Add(new ImageItem
                        {
                            Title = $"{splash.SplashName} - HD Wallpaper",
                            ImageUrl = url,
                            Type = "Splash"
                        });
                    }
                }

                ImagesCollectionView.ItemsSource = imageItems;
                CollectionTitleLabel.Text = "HD Wallpaper для заставок";

                _jintService.ExecuteScript($"info('✅ Загружено {imageItems.Count} изображений заставок')");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить wallpaper: {ex.Message}", "OK");
                _jintService.ExecuteScript($"error('❌ Ошибка загрузки: {ex.Message}')");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnAlertButtonClicked(object sender, EventArgs e)
        {
            try
            {
                _jintService.ExecuteScript("log('🎨 Открытие выбора цвета фона...')");

                var colors = await _databaseService.GetActiveColorsAsync();

                if (colors.Count == 0)
                {
                    await DisplayAlert("Ошибка", "Нет доступных цветов в базе данных", "OK");
                    _jintService.ExecuteScript("warn('⚠️ Нет доступных цветов')");
                    return;
                }

                _jintService.SetValue("colors", colors.Select(c => c.ColorName).ToArray());

                var script = @"
                    var colorList = colors.join(', ');
                    var count = colors.length;
                    log('Доступно цветов: ' + count);
                    info('Цвета: ' + colorList);
                    'Выберите один из ' + count + ' цветов';
                ";

                var message = _jintService.ExecuteScript(script);

                var colorButtons = colors.Select(c => c.ColorName).ToArray();

                var action = await DisplayActionSheet(
                    "Выберите цвет фона",
                    "Отмена",
                    null,
                    colorButtons);

                if (action != "Отмена" && !string.IsNullOrEmpty(action))
                {
                    _jintService.ExecuteScript($"log('✅ Выбран цвет: {action}')");
                    await LoadColorBackground(action);
                }
                else
                {
                    _jintService.ExecuteScript("log('❌ Выбор цвета отменен')");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
                _jintService.ExecuteScript($"error('❌ Ошибка: {ex.Message}')");
            }
        }

        private async void OnAddColorClicked(object sender, EventArgs e)
        {
            try
            {
                _jintService.ExecuteScript("log('➕ Запуск мастера добавления цвета фона...')");

                // Показываем Alert для ввода названия цвета
                var result = await DisplayPromptAsync(
                    "🎨 Новый цвет фона",
                    "Введите название цвета для поиска в Google Images:",
                    "🔍 Найти",
                    "❌ Отмена",
                    placeholder: "например: голубой, бирюзовый, малиновый",
                    maxLength: 50
                );

                if (!string.IsNullOrWhiteSpace(result))
                {
                    // ШАГ 1: Валидация через JINT
                    _jintService.ExecuteScript($"log('📝 ШАГ 1: Валидация названия «{result}»')");

                    bool isValid = _jintService.ValidateString(result);

                    if (!isValid)
                    {
                        _jintService.ExecuteScript($"error('❌ Валидация не пройдена: название слишком короткое')");
                        await DisplayAlert("❌ Ошибка валидации",
                            "Название цвета должно содержать минимум 2 символа",
                            "OK");
                        return;
                    }

                    _jintService.ExecuteScript($"info('✅ Валидация пройдена успешно')");

                    // ШАГ 2: Проверка дубликатов через JINT
                    _jintService.ExecuteScript($"log('📝 ШАГ 2: Проверка дубликатов в базе данных')");

                    var colors = await _databaseService.GetActiveColorsAsync();
                    _jintService.SetValue("existingColors", colors.Select(c => c.ColorName.ToLower()).ToArray());
                    _jintService.SetValue("newColor", result.ToLower());

                    var checkScript = @"
                log('🔍 Поиск дубликатов...');
                var exists = false;
                var foundAt = -1;
                
                for (var i = 0; i < existingColors.length; i++) {
                    if (existingColors[i] === newColor) {
                        exists = true;
                        foundAt = i;
                        break;
                    }
                }
                
                if (exists) {
                    warn('⚠️ Найден дубликат на позиции: ' + foundAt);
                } else {
                    info('✅ Дубликатов не найдено');
                }
                
                exists;
            ";

                    var existsResult = _jintService.ExecuteScript(checkScript);
                    bool exists = existsResult.Equals("true", StringComparison.OrdinalIgnoreCase);

                    if (exists)
                    {
                        await DisplayAlert("⚠️ Дубликат найден",
                            $"Цвет «{result}» уже существует в базе данных!",
                            "OK");
                        return;
                    }

                    // ШАГ 3: Построение поискового запроса через JINT
                    _jintService.ExecuteScript($"log('📝 ШАГ 3: Построение Google поискового запроса')");

                    var googleQuery = _jintService.BuildSearchQuery(result, "color");

                    _jintService.ExecuteScript($"info('🌐 Google запрос: «{googleQuery}»')");

                    // ШАГ 4: Парсинг изображений
                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsRunning = true;

                    _jintService.ExecuteScript($"log('📝 ШАГ 4: Запуск парсинга Google Images')");
                    _jintService.ExecuteScript($"log('🌐 URL: https://www.google.com/search?q={Uri.EscapeDataString(googleQuery)}&tbm=isch')");

                    var urls = await _imageParserService.ParseGoogleImages(result, 10);

                    // ШАГ 5: Анализ результатов через JINT
                    _jintService.ExecuteScript($"log('📝 ШАГ 5: Анализ результатов парсинга')");

                    var analysisMessage = _jintService.AnalyzeParsingResults(urls.Count, result);

                    if (urls.Count > 0)
                    {
                        // ШАГ 6: Сохранение в БД
                        _jintService.ExecuteScript($"log('📝 ШАГ 6: Сохранение в базу данных')");

                        await _databaseService.AddColorAsync(new ColorOption
                        {
                            ColorName = result,
                            IsActive = true
                        });

                        _jintService.ExecuteScript($"info('✅ Цвет «{result}» успешно добавлен в БД')");

                        // ШАГ 7: Отображение preview
                        _jintService.ExecuteScript($"log('📝 ШАГ 7: Отображение предпросмотра')");

                        var previewImage = new Image
                        {
                            Source = ImageSource.FromUri(new Uri(urls[0])),
                            Aspect = Aspect.AspectFill
                        };
                        PreviewFrame.Content = previewImage;

                        _jintService.ExecuteScript("info('✅ ВСЕ ШАГИ ЗАВЕРШЕНЫ УСПЕШНО!')");

                        await DisplayAlert("✅ Успех!",
                            $"Цвет «{result}» успешно добавлен!\n\n" +
                            $"🖼️ Найдено изображений: {urls.Count}\n" +
                            $"🔍 Поисковый запрос: {googleQuery}\n\n" +
                            $"💡 Теперь вы можете выбрать его через кнопку «Выбрать цвет фона»",
                            "OK");
                    }
                    else
                    {
                        _jintService.ExecuteScript("error('❌ Парсинг не дал результатов')");

                        await DisplayAlert("⚠️ Предупреждение",
                            $"Цвет «{result}» добавлен, но изображения не найдены.\n\n" +
                            $"🔍 Запрос: {googleQuery}\n\n" +
                            $"Попробуйте:\n" +
                            $"• Другое название\n" +
                            $"• Проверить интернет-соединение\n" +
                            $"• Использовать английское название",
                            "OK");
                    }
                }
                else
                {
                    _jintService.ExecuteScript("log('❌ Добавление цвета отменено пользователем')");
                }
            }
            catch (Exception ex)
            {
                _jintService.ExecuteScript($"error('❌ КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}')");
                await DisplayAlert("❌ Ошибка", $"Не удалось добавить цвет: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnSplashButtonClicked(object sender, EventArgs e)
        {
            try
            {
                _jintService.ExecuteScript("log('🖼️ Открытие выбора заставки...')");

                var splashes = await _databaseService.GetActiveSplashesAsync();

                if (splashes.Count == 0)
                {
                    await DisplayAlert("Ошибка", "Нет доступных заставок в базе данных", "OK");
                    _jintService.ExecuteScript("warn('⚠️ Нет доступных заставок')");
                    return;
                }

                _jintService.SetValue("splashes", splashes.Select(s => s.SplashName).ToArray());

                var script = @"
                    var splashList = splashes.join(', ');
                    var count = splashes.length;
                    log('Доступно заставок: ' + count);
                    info('Заставки: ' + splashList);
                    'Выберите одну из ' + count + ' заставок';
                ";

                var message = _jintService.ExecuteScript(script);

                var splashButtons = splashes.Select(s => s.SplashName).ToArray();

                var action = await DisplayActionSheet(
                    "Выберите заставку",
                    "Отмена",
                    null,
                    splashButtons);

                if (action != "Отмена" && !string.IsNullOrEmpty(action))
                {
                    _jintService.ExecuteScript($"log('✅ Выбрана заставка: {action}')");
                    await LoadSplashScreen(action);
                }
                else
                {
                    _jintService.ExecuteScript("log('❌ Выбор заставки отменен')");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
                _jintService.ExecuteScript($"error('❌ Ошибка: {ex.Message}')");
            }
        }

        private async void OnAddSplashClicked(object sender, EventArgs e)
        {
            try
            {
                _jintService.ExecuteScript("log('➕ Запуск мастера добавления заставки...')");

                // Показываем Alert для ввода названия заставки
                var result = await DisplayPromptAsync(
                    "🖼️ Новая заставка",
                    "Введите название темы для поиска wallpaper в Google:",
                    "🔍 Найти",
                    "❌ Отмена",
                    placeholder: "например: Деревня, Лес, Пляж, Закат",
                    maxLength: 50
                );

                if (!string.IsNullOrWhiteSpace(result))
                {
                    // ШАГ 1: Валидация через JINT
                    _jintService.ExecuteScript($"log('📝 ШАГ 1: Валидация названия «{result}»')");

                    bool isValid = _jintService.ValidateString(result);

                    if (!isValid)
                    {
                        _jintService.ExecuteScript($"error('❌ Валидация не пройдена: название слишком короткое')");
                        await DisplayAlert("❌ Ошибка валидации",
                            "Название заставки должно содержать минимум 2 символа",
                            "OK");
                        return;
                    }

                    _jintService.ExecuteScript($"info('✅ Валидация пройдена успешно')");

                    // ШАГ 2: Проверка дубликатов
                    _jintService.ExecuteScript($"log('📝 ШАГ 2: Проверка дубликатов в базе данных')");

                    var exists = await _databaseService.SplashExistsAsync(result);

                    if (exists)
                    {
                        _jintService.ExecuteScript($"warn('⚠️ Найден дубликат: {result}')");
                        await DisplayAlert("⚠️ Дубликат найден",
                            $"Заставка «{result}» уже существует в базе данных!",
                            "OK");
                        return;
                    }

                    _jintService.ExecuteScript("info('✅ Дубликатов не найдено')");

                    // ШАГ 3: Построение поискового запроса через JINT
                    _jintService.ExecuteScript($"log('📝 ШАГ 3: Построение Google поискового запроса для wallpaper')");

                    var googleQuery = _jintService.BuildSearchQuery(result, "wallpaper");

                    _jintService.ExecuteScript($"info('🌐 Google запрос: «{googleQuery}»')");

                    // ШАГ 4: Парсинг wallpaper
                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsRunning = true;

                    _jintService.ExecuteScript($"log('📝 ШАГ 4: Запуск парсинга HD wallpaper')");
                    _jintService.ExecuteScript($"log('🌐 URL: https://www.google.com/search?q={Uri.EscapeDataString(googleQuery)}&tbm=isch&tbs=isz:l')");

                    var urls = await _imageParserService.ParseHighQualityWallpapers(result, 8);

                    // ШАГ 5: Анализ результатов через JINT
                    _jintService.ExecuteScript($"log('📝 ШАГ 5: Анализ результатов парсинга')");

                    var analysisMessage = _jintService.AnalyzeParsingResults(urls.Count, result);

                    if (urls.Count > 0)
                    {
                        // ШАГ 6: Сохранение в БД
                        _jintService.ExecuteScript($"log('📝 ШАГ 6: Сохранение в базу данных')");

                        await _databaseService.AddSplashByNameAsync(result);

                        _jintService.ExecuteScript($"info('✅ Заставка «{result}» успешно добавлена в БД')");

                        // ШАГ 7: Отображение preview
                        _jintService.ExecuteScript($"log('📝 ШАГ 7: Отображение предпросмотра первого wallpaper')");

                        var previewImage = new Image
                        {
                            Source = ImageSource.FromUri(new Uri(urls[0])),
                            Aspect = Aspect.AspectFill
                        };
                        PreviewFrame.Content = previewImage;

                        _jintService.ExecuteScript("info('✅ ВСЕ ШАГИ ЗАВЕРШЕНЫ УСПЕШНО!')");

                        await DisplayAlert("✅ Успех!",
                            $"Заставка «{result}» успешно добавлена!\n\n" +
                            $"🖼️ Найдено HD wallpaper: {urls.Count}\n" +
                            $"🔍 Поисковый запрос: {googleQuery}\n\n" +
                            $"💡 Теперь вы можете выбрать её через кнопку «Выбрать заставку»",
                            "OK");
                    }
                    else
                    {
                        _jintService.ExecuteScript("error('❌ Парсинг не дал результатов')");

                        await DisplayAlert("⚠️ Предупреждение",
                            $"Заставка «{result}» добавлена, но wallpaper не найдены.\n\n" +
                            $"🔍 Запрос: {googleQuery}\n\n" +
                            $"Попробуйте:\n" +
                            $"• Более конкретное название\n" +
                            $"• Проверить интернет-соединение\n" +
                            $"• Использовать популярные темы",
                            "OK");
                    }
                }
                else
                {
                    _jintService.ExecuteScript("log('❌ Добавление заставки отменено пользователем')");
                }
            }
            catch (Exception ex)
            {
                _jintService.ExecuteScript($"error('❌ КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}')");
                await DisplayAlert("❌ Ошибка", $"Не удалось добавить заставку: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async Task LoadSplashScreen(string splashName)
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                SelectedSplashLabel.Text = $"Выбрана заставка: {splashName}";
                _jintService.ExecuteScript($"log('🎬 Загрузка заставки: {splashName}')");

                _databaseService.SetCurrentSplashAsync(splashName);

                var urls = await _imageParserService.ParseHighQualityWallpapers(splashName, 8);

                if (urls.Count > 0)
                {
                    _jintService.ExecuteScript($"info('✅ Найдено {urls.Count} wallpaper для {splashName}')");

                    _databaseService.SetCurrentSplashImageUrl(urls[0]);

                    var splashImage = new Image
                    {
                        Source = ImageSource.FromUri(new Uri(urls[0])),
                        Aspect = Aspect.AspectFill
                    };
                    PreviewFrame.Content = splashImage;

                    _isShowingSplashes = true;
                    ViewModeButton.Text = "🖼️ Заставки";
                    CollectionTitleLabel.Text = $"Wallpaper: {splashName}";

                    var imageItems = urls.Select((url, index) => new ImageItem
                    {
                        Title = $"{splashName} - HD Wallpaper {index + 1}",
                        ImageUrl = url,
                        Type = "Splash"
                    }).ToList();

                    ImagesCollectionView.ItemsSource = imageItems;

                    await DisplayAlert("Успех",
                        $"✅ Заставка '{splashName}' сохранена!\n\n🖼️ Найдено {urls.Count} HD wallpaper.\n\n💡 Выберите понравившийся wallpaper из списка ниже.\n\n🎬 Он будет отображаться при запуске приложения",
                        "OK");
                }
                else
                {
                    _jintService.ExecuteScript($"warn('⚠️ Wallpaper не найдены для {splashName}')");

                    await DisplayAlert("Внимание",
                        $"Не удалось найти wallpaper для '{splashName}'.\n\nПопробуйте другое название или проверьте подключение к интернету.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить wallpaper: {ex.Message}", "OK");
                _jintService.ExecuteScript($"error('❌ Ошибка загрузки: {ex.Message}')");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async Task LoadColorBackground(string colorName)
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                SelectedColorLabel.Text = $"Выбран цвет: {colorName}";
                _jintService.ExecuteScript($"log('🎨 Загрузка фона цвета: {colorName}')");

                var selectedColor = GetColorFromName(colorName);

                _databaseService.SetCurrentColor(colorName);

                await AnimateBackgroundColor(selectedColor);

                var imageUrls = await _imageParserService.ParseGoogleImages(colorName);

                if (imageUrls.Count > 0)
                {
                    _jintService.ExecuteScript($"info('✅ Найдено {imageUrls.Count} изображений для {colorName}')");

                    var imageItems = imageUrls.Select((url, index) => new ImageItem
                    {
                        Title = $"{colorName} - фон {index + 1}",
                        ImageUrl = url,
                        Type = "Color"
                    }).ToList();

                    ImagesCollectionView.ItemsSource = imageItems;

                    _isShowingSplashes = false;
                    ViewModeButton.Text = "📸 Фоны";
                    CollectionTitleLabel.Text = "Изображения фонов";

                    PreviewFrame.BackgroundColor = selectedColor;

                    var firstImage = new Image
                    {
                        Source = ImageSource.FromUri(new Uri(imageUrls[0])),
                        Aspect = Aspect.AspectFill
                    };
                    PreviewFrame.Content = firstImage;

                    await DisplayAlert("Успех",
                        $"✅ Найдено {imageUrls.Count} изображений для цвета '{colorName}'\n\n💡 Выберите изображение из списка, чтобы установить его как фон",
                        "OK");
                }
                else
                {
                    _jintService.ExecuteScript($"warn('⚠️ Изображения не найдены для {colorName}')");

                    await DisplayAlert("Внимание",
                        "Не удалось найти изображения. Используется стандартный цвет.",
                        "OK");

                    PreviewFrame.BackgroundColor = selectedColor;

                    var contrastColor = GetContrastColor(selectedColor);
                    PreviewFrame.Content = new Label
                    {
                        Text = "Предпросмотр фона",
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        FontSize = 18,
                        TextColor = contrastColor
                    };

                    UpdateTextColors(contrastColor);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить изображения: {ex.Message}", "OK");
                _jintService.ExecuteScript($"error('❌ Ошибка: {ex.Message}')");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async Task AnimateBackgroundColor(Color targetColor)
        {
            var animation = new Animation(v =>
            {
                this.BackgroundColor = targetColor.WithAlpha((float)v);
            }, 0, 1);

            animation.Commit(this, "BackgroundColorAnimation", 16, 500, Easing.SinInOut);
            await Task.Delay(500);

            this.BackgroundColor = targetColor;
        }

        private void UpdateTextColors(Color textColor)
        {
            SelectedColorLabel.TextColor = textColor;
            SelectedSplashLabel.TextColor = textColor;
            CollectionTitleLabel.TextColor = textColor;
        }

        private async void OnImageSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is ImageItem imageItem)
            {
                try
                {
                    _jintService.ExecuteScript($"log('🖼️ Выбрано изображение: {imageItem.Title}')");

                    var backgroundImage = new Image
                    {
                        Source = ImageSource.FromUri(new Uri(imageItem.ImageUrl)),
                        Aspect = Aspect.AspectFill
                    };

                    PreviewFrame.Content = backgroundImage;

                    if (imageItem.Type == "Color")
                    {
                        this.BackgroundImageSource = ImageSource.FromUri(new Uri(imageItem.ImageUrl));
                        _currentSelectedImageUrl = imageItem.ImageUrl;

                        var colorName = _databaseService.GetCurrentColor();
                        await _databaseService.SaveCurrentBackgroundAsync(imageItem.ImageUrl, colorName);

                        _jintService.ExecuteScript($"info('✅ Фон сохранен: {colorName}')");

                        await DisplayAlert("Сохранено",
                            $"✅ Фон установлен!\n\nЭтот фон будет отображаться при следующем запуске приложения",
                            "OK");
                    }
                    else if (imageItem.Type == "Splash")
                    {
                        _databaseService.SetCurrentSplashImageUrl(imageItem.ImageUrl);

                        _jintService.ExecuteScript($"info('✅ Заставка сохранена: {imageItem.Title}')");

                        await DisplayAlert("Сохранено",
                            $"✅ Заставка установлена!\n\n🎬 Эта заставка будет показываться при запуске приложения (2-3 секунды)",
                            "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось загрузить изображение: {ex.Message}", "OK");
                    _jintService.ExecuteScript($"error('❌ Ошибка загрузки изображения: {ex.Message}')");
                }
            }
        }

        private Color GetColorFromName(string colorName)
        {
            return colorName.ToLower() switch
            {
                "розовый" => Colors.Pink,
                "чёрный" or "черный" => Colors.Black,
                "синий" => Colors.Blue,
                "зелёный" or "зеленый" => Colors.Green,
                "красный" => Colors.Red,
                "белый" => Colors.White,
                "жёлтый" or "желтый" => Colors.Yellow,
                "оранжевый" => Colors.Orange,
                "фиолетовый" => Colors.Purple,
                "серый" => Colors.Gray,
                "коричневый" => Colors.Brown,
                "голубой" => Colors.LightBlue,
                "бирюзовый" => Colors.Turquoise,
                "малиновый" => Colors.Crimson,
                _ => Colors.LightGray
            };
        }

        private Color GetContrastColor(Color backgroundColor)
        {
            var brightness = (backgroundColor.Red * 299 + backgroundColor.Green * 587 + backgroundColor.Blue * 114) / 1000;
            return brightness < 0.5 ? Colors.White : Colors.Black;
        }
    }
}