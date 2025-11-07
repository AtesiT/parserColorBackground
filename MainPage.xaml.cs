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
                _jintService.ExecuteScript("log('➕ Добавление нового цвета фона...')");

                var result = await DisplayPromptAsync(
                    "Новый цвет фона",
                    "Введите название цвета (например: голубой, бирюзовый, малиновый):",
                    "Добавить",
                    "Отмена",
                    placeholder: "Название цвета",
                    maxLength: 50
                );

                if (!string.IsNullOrWhiteSpace(result))
                {
                    _jintService.ExecuteScript($"log('🔍 Проверка названия цвета: {result}')");

                    bool isValid = _jintService.ValidateString(result);

                    if (!isValid)
                    {
                        await DisplayAlert("Ошибка", "Название цвета должно содержать минимум 2 символа", "OK");
                        _jintService.ExecuteScript($"warn('⚠️ Некорректное название: {result}')");
                        return;
                    }

                    _jintService.ExecuteScript($"info('✅ Название прошло валидацию: {result}')");

                    var colors = await _databaseService.GetActiveColorsAsync();
                    _jintService.SetValue("existingColors", colors.Select(c => c.ColorName.ToLower()).ToArray());
                    _jintService.SetValue("newColor", result.ToLower());

                    var checkScript = @"
                        var exists = false;
                        for (var i = 0; i < existingColors.length; i++) {
                            if (existingColors[i] === newColor) {
                                exists = true;
                                break;
                            }
                        }
                        exists;
                    ";

                    var existsResult = _jintService.ExecuteScript(checkScript);
                    bool exists = existsResult.Equals("true", StringComparison.OrdinalIgnoreCase);

                    if (exists)
                    {
                        await DisplayAlert("Внимание",
                            $"Цвет '{result}' уже существует в базе данных!",
                            "OK");
                        _jintService.ExecuteScript($"warn('⚠️ Цвет уже существует: {result}')");
                        return;
                    }

                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsRunning = true;

                    await _databaseService.AddColorAsync(new ColorOption
                    {
                        ColorName = result,
                        IsActive = true
                    });

                    _jintService.ExecuteScript($"info('✅ Цвет добавлен в БД: {result}')");

                    _jintService.ExecuteScript($"log('🔍 Поиск изображений для цвета: {result}')");
                    var urls = await _imageParserService.ParseGoogleImages(result, 5);

                    if (urls.Count > 0)
                    {
                        _jintService.SetValue("imageCount", urls.Count);
                        _jintService.ExecuteScript("info('🖼️ Найдено изображений: ' + imageCount)");

                        var previewImage = new Image
                        {
                            Source = ImageSource.FromUri(new Uri(urls[0])),
                            Aspect = Aspect.AspectFill
                        };
                        PreviewFrame.Content = previewImage;

                        await DisplayAlert("Успех",
                            $"✅ Цвет '{result}' успешно добавлен!\n\n🖼️ Найдено {urls.Count} фоновых изображений.\n\nТеперь вы можете выбрать его через кнопку 'Выбрать цвет фона'",
                            "OK");
                    }
                    else
                    {
                        _jintService.ExecuteScript($"warn('⚠️ Изображения не найдены для: {result}')");

                        await DisplayAlert("Внимание",
                            $"Цвет '{result}' добавлен, но изображения не Серьезность\tКод\tОписание\tПроект\tФайл\tСтрока\tСостояние подавления\r\nОшибка (активно)\tCS0103\tИмя \"PreviewFrame\" не существует в текущем контексте.\tparserColorBackground (net8.0-android), parserColorBackground (net8.0-ios), parserColorBackground (net8.0-maccatalyst), parserColorBackground (net8.0-windows10.0.19041.0)\tD:\\Keys And Documents\\Labs\\4 Course\\parserColorBackground\\MainPage.xaml.cs\t363\t\r\nОшибка (активно)\tCS0103\tИмя \"PreviewFrame\" не существует в текущем контексте.\tparserColorBackground (net8.0-android), parserColorBackground (net8.0-ios), parserColorBackground (net8.0-maccatalyst), parserColorBackground (net8.0-windows10.0.19041.0)\tD:\\Keys And Documents\\Labs\\4 Course\\parserColorBackground\\MainPage.xaml.cs\t507\t\r\nОшибка (активно)\tCS0103\tИмя \"PreviewFrame\" не существует в текущем контексте.\tparserColorBackground (net8.0-android), parserColorBackground (net8.0-ios), parserColorBackground (net8.0-maccatalyst), parserColorBackground (net8.0-windows10.0.19041.0)\tD:\\Keys And Documents\\Labs\\4 Course\\parserColorBackground\\MainPage.xaml.cs\t564\t\r\nОшибка (активно)\tCS0103\tИмя \"PreviewFrame\" не существует в текущем контексте.\tparserColorBackground (net8.0-android), parserColorBackground (net8.0-ios), parserColorBackground (net8.0-maccatalyst), parserColorBackground (net8.0-windows10.0.19041.0)\tD:\\Keys And Documents\\Labs\\4 Course\\parserColorBackground\\MainPage.xaml.cs\t639\t\r\nОшибка (активно)\tCS0103\tИмя \"PreviewFrame\" не существует в текущем контексте.\tparserColorBackground (net8.0-android), parserColorBackground (net8.0-ios), parserColorBackground (net8.0-maccatalyst), parserColorBackground (net8.0-windows10.0.19041.0)\tD:\\Keys And Documents\\Labs\\4 Course\\parserColorBackground\\MainPage.xaml.cs\t646\t\r\nОшибка (активно)\tCS0103\tИмя \"PreviewFrame\" не существует в текущем контексте.\tparserColorBackground (net8.0-android), parserColorBackground (net8.0-ios), parserColorBackground (net8.0-maccatalyst), parserColorBackground (net8.0-windows10.0.19041.0)\tD:\\Keys And Documents\\Labs\\4 Course\\parserColorBackground\\MainPage.xaml.cs\t660\t\r\nОшибка (активно)\tCS0103\tИмя \"PreviewFrame\" не существует в текущем контексте.\tparserColorBackground (net8.0-android), parserColorBackground (net8.0-ios), parserColorBackground (net8.0-maccatalyst), parserColorBackground (net8.0-windows10.0.19041.0)\tD:\\Keys And Documents\\Labs\\4 Course\\parserColorBackground\\MainPage.xaml.cs\t663\t\r\nОшибка (активно)\tCS0103\tИмя \"PreviewFrame\" не существует в текущем контексте.\tparserColorBackground (net8.0-android), parserColorBackground (net8.0-ios), parserColorBackground (net8.0-maccatalyst), parserColorBackground (net8.0-windows10.0.19041.0)\tD:\\Keys And Documents\\Labs\\4 Course\\parserColorBackground\\MainPage.xaml.cs\t721\t\r\n.\n\nПопробуйте другое название или проверьте подключение к интернету.",
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
                await DisplayAlert("Ошибка", $"Не удалось добавить цвет: {ex.Message}", "OK");
                _jintService.ExecuteScript($"error('❌ Ошибка добавления цвета: {ex.Message}')");
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
                _jintService.ExecuteScript("log('➕ Добавление новой заставки...')");

                var result = await DisplayPromptAsync(
                    "Новая заставка",
                    "Введите название заставки (например: Деревня, Лес, Пляж, Закат):",
                    "Добавить",
                    "Отмена",
                    placeholder: "Название заставки",
                    maxLength: 50
                );

                if (!string.IsNullOrWhiteSpace(result))
                {
                    _jintService.ExecuteScript($"log('🔍 Проверка названия заставки: {result}')");

                    bool isValid = _jintService.ValidateString(result);

                    if (!isValid)
                    {
                        await DisplayAlert("Ошибка", "Название заставки должно содержать минимум 2 символа", "OK");
                        _jintService.ExecuteScript($"warn('⚠️ Некорректное название: {result}')");
                        return;
                    }

                    _jintService.ExecuteScript($"info('✅ Название прошло валидацию: {result}')");

                    var exists = await _databaseService.SplashExistsAsync(result);

                    if (exists)
                    {
                        await DisplayAlert("Внимание",
                            $"Заставка '{result}' уже существует в базе данных!",
                            "OK");
                        _jintService.ExecuteScript($"warn('⚠️ Заставка уже существует: {result}')");
                        return;
                    }

                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsRunning = true;

                    await _databaseService.AddSplashByNameAsync(result);
                    _jintService.ExecuteScript($"info('✅ Заставка добавлена в БД: {result}')");

                    _jintService.ExecuteScript($"log('🔍 Поиск wallpaper для: {result}')");
                    var urls = await _imageParserService.ParseHighQualityWallpapers(result, 3);

                    if (urls.Count > 0)
                    {
                        _jintService.SetValue("wallpaperCount", urls.Count);
                        _jintService.ExecuteScript("info('🖼️ Найдено wallpaper: ' + wallpaperCount)");

                        var previewImage = new Image
                        {
                            Source = ImageSource.FromUri(new Uri(urls[0])),
                            Aspect = Aspect.AspectFill
                        };
                        PreviewFrame.Content = previewImage;

                        await DisplayAlert("Успех",
                            $"✅ Заставка '{result}' успешно добавлена!\n\n🖼️ Найдено {urls.Count} wallpaper.\n\nТеперь вы можете выбрать её через кнопку 'Выбрать заставку'",
                            "OK");
                    }
                    else
                    {
                        _jintService.ExecuteScript($"warn('⚠️ Wallpaper не найдены для: {result}')");

                        await DisplayAlert("Внимание",
                            $"Заставка '{result}' добавлена, но изображения не найдены.\n\nПопробуйте другое название или проверьте подключение к интернету.",
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
                await DisplayAlert("Ошибка", $"Не удалось добавить заставку: {ex.Message}", "OK");
                _jintService.ExecuteScript($"error('❌ Ошибка добавления заставки: {ex.Message}')");
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