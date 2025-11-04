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

            LoadSavedSettings();
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
                // Загружаем сохраненный фон с изображением
                var savedBackground = await _databaseService.GetCurrentBackgroundAsync();

                if (savedBackground != null && !string.IsNullOrEmpty(savedBackground.ImageUrl))
                {
                    // Устанавливаем изображение как фон
                    this.BackgroundImageSource = ImageSource.FromUri(new Uri(savedBackground.ImageUrl));
                    SelectedColorLabel.Text = $"Текущий фон: {savedBackground.ColorName}";
                    _currentSelectedImageUrl = savedBackground.ImageUrl;
                }
                else
                {
                    // Загружаем только цвет, если нет изображения
                    var savedColor = _databaseService.GetCurrentColor();
                    if (!string.IsNullOrEmpty(savedColor))
                    {
                        var color = GetColorFromName(savedColor);
                        this.BackgroundColor = color;
                        SelectedColorLabel.Text = $"Текущий цвет: {savedColor}";

                        var contrastColor = GetContrastColor(color);
                        UpdateTextColors(contrastColor);
                    }
                }

                // Загружаем сохраненную заставку
                var savedSplashName = _databaseService.GetCurrentSplashName();
                if (!string.IsNullOrEmpty(savedSplashName))
                {
                    SelectedSplashLabel.Text = $"Текущая заставка: {savedSplashName}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        private void OnClearSqlClicked(object sender, EventArgs e)
        {
            SqlQueryEditor.Text = string.Empty;
            SqlResultFrame.IsVisible = false;
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

                var result = await _databaseService.ExecuteRawSqlAsync(sql);

                SqlResultLabel.Text = result;
                SqlResultFrame.IsVisible = true;

                _jintService.SetValue("sqlQuery", sql);
                _jintService.SetValue("sqlResult", result);
                _jintService.ExecuteScript("log('SQL выполнен: ' + sqlQuery)");
            }
            catch (Exception ex)
            {
                SqlResultLabel.Text = $"Ошибка: {ex.Message}";
                SqlResultFrame.IsVisible = true;
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnViewModeClicked(object sender, EventArgs e)
        {
            _isShowingSplashes = !_isShowingSplashes;

            if (_isShowingSplashes)
            {
                ViewModeButton.Text = "🖼️ Заставки";
                CollectionTitleLabel.Text = "Доступные заставки";
                await LoadSplashesPreview();
            }
            else
            {
                ViewModeButton.Text = "📸 Фоны";
                CollectionTitleLabel.Text = "Изображения фонов";
                ImagesCollectionView.ItemsSource = null;
            }
        }

        private async Task LoadSplashesPreview()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                var splashes = await _databaseService.GetActiveSplashesAsync();
                var imageItems = new List<ImageItem>();

                foreach (var splash in splashes)
                {
                    // Парсим изображения для каждой заставки
                    var urls = await _imageParserService.ParseSplashImages(splash.SplashName, 3);

                    foreach (var url in urls)
                    {
                        imageItems.Add(new ImageItem
                        {
                            Title = splash.SplashName,
                            ImageUrl = url,
                            Type = "Splash"
                        });
                    }
                }

                ImagesCollectionView.ItemsSource = imageItems;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить заставки: {ex.Message}", "OK");
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
                var colors = await _databaseService.GetActiveColorsAsync();

                if (colors.Count == 0)
                {
                    await DisplayAlert("Ошибка", "Нет доступных цветов в базе данных", "OK");
                    return;
                }

                _jintService.SetValue("colors", colors.Select(c => c.ColorName).ToArray());

                var script = @"
                    var colorList = colors.join(', ');
                    'Доступные цвета: ' + colorList;
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
                    await LoadColorBackground(action);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void OnSplashButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var splashes = await _databaseService.GetActiveSplashesAsync();

                if (splashes.Count == 0)
                {
                    await DisplayAlert("Ошибка", "Нет доступных заставок в базе данных", "OK");
                    return;
                }

                _jintService.SetValue("splashes", splashes.Select(s => s.SplashName).ToArray());

                var script = @"
                    var splashList = splashes.join(', ');
                    'Доступные заставки: ' + splashList;
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
                    await LoadSplashScreen(action);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async Task LoadSplashScreen(string splashName)
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                SelectedSplashLabel.Text = $"Выбрана заставка: {splashName}";

                _databaseService.SetCurrentSplashAsync(splashName);

                var splashes = await _databaseService.GetActiveSplashesAsync();
                var selectedSplash = splashes.FirstOrDefault(s => s.SplashName == splashName);

                if (selectedSplash != null)
                {
                    // Парсим изображения для выбранной заставки
                    var urls = await _imageParserService.ParseSplashImages(splashName, 5);

                    if (urls.Count > 0)
                    {
                        // Сохраняем первое изображение как текущую заставку
                        _databaseService.SetCurrentSplashImageUrl(urls[0]);

                        var splashImage = new Image
                        {
                            Source = ImageSource.FromUri(new Uri(urls[0])),
                            Aspect = Aspect.AspectFill
                        };
                        PreviewFrame.Content = splashImage;

                        // Автоматически переключаемся на режим просмотра заставок
                        _isShowingSplashes = true;
                        ViewModeButton.Text = "🖼️ Заставки";
                        CollectionTitleLabel.Text = "Доступные заставки";

                        // Показываем все найденные варианты
                        var imageItems = urls.Select((url, index) => new ImageItem
                        {
                            Title = $"{splashName} - вариант {index + 1}",
                            ImageUrl = url,
                            Type = "Splash"
                        }).ToList();

                        ImagesCollectionView.ItemsSource = imageItems;

                        await DisplayAlert("Успех",
                            $"✅ Заставка '{splashName}' сохранена!\n\n📸 Найдено {urls.Count} вариантов изображений.\n\n💡 Выберите понравившееся из списка ниже.\n\nОна будет отображаться при следующем запуске приложения",
                            "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить заставку: {ex.Message}", "OK");
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

                _jintService.SetValue("selectedColor", colorName);
                _jintService.ExecuteScript("log('Загрузка изображений для цвета: ' + selectedColor)");

                var selectedColor = GetColorFromName(colorName);

                _databaseService.SetCurrentColor(colorName);

                await AnimateBackgroundColor(selectedColor);

                var imageUrls = await _imageParserService.ParseGoogleImages(colorName);

                if (imageUrls.Count > 0)
                {
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
                    var backgroundImage = new Image
                    {
                        Source = ImageSource.FromUri(new Uri(imageItem.ImageUrl)),
                        Aspect = Aspect.AspectFill
                    };

                    PreviewFrame.Content = backgroundImage;

                    if (imageItem.Type == "Color")
                    {
                        // Сохраняем выбранное изображение как текущий фон
                        this.BackgroundImageSource = ImageSource.FromUri(new Uri(imageItem.ImageUrl));
                        _currentSelectedImageUrl = imageItem.ImageUrl;

                        var colorName = _databaseService.GetCurrentColor();
                        await _databaseService.SaveCurrentBackgroundAsync(imageItem.ImageUrl, colorName);

                        await DisplayAlert("Сохранено",
                            $"✅ Фон установлен!\n\nЭтот фон будет отображаться при следующем запуске приложения",
                            "OK");
                    }
                    else if (imageItem.Type == "Splash")
                    {
                        // Сохраняем выбранное изображение как заставку
                        _databaseService.SetCurrentSplashImageUrl(imageItem.ImageUrl);

                        await DisplayAlert("Сохранено",
                            $"✅ Заставка установлена!\n\n🎬 Эта заставка будет показываться при запуске приложения (2-3 секунды)",
                            "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось загрузить изображение: {ex.Message}", "OK");
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