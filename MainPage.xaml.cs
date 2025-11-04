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
                // Загружаем сохраненный цвет
                var savedColor = _databaseService.GetCurrentColor();
                if (!string.IsNullOrEmpty(savedColor))
                {
                    var color = GetColorFromName(savedColor);
                    this.BackgroundColor = color;
                    SelectedColorLabel.Text = $"Текущий цвет: {savedColor}";

                    var contrastColor = GetContrastColor(color);
                    UpdateTextColors(contrastColor);
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

                var imageItems = splashes.Select(s => new ImageItem
                {
                    Title = s.SplashName,
                    ImageUrl = s.ImageUrl,
                    Type = "Splash"
                }).ToList();

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

                // Сохраняем выбор
                _databaseService.SetCurrentSplashAsync(splashName);

                var splashes = await _databaseService.GetActiveSplashesAsync();
                var selectedSplash = splashes.FirstOrDefault(s => s.SplashName == splashName);

                if (selectedSplash != null && !string.IsNullOrEmpty(selectedSplash.ImageUrl))
                {
                    var splashImage = new Image
                    {
                        Source = ImageSource.FromUri(new Uri(selectedSplash.ImageUrl)),
                        Aspect = Aspect.AspectFill
                    };
                    PreviewFrame.Content = splashImage;

                    // Автоматически переключаемся на режим просмотра заставок
                    _isShowingSplashes = true;
                    ViewModeButton.Text = "🖼️ Заставки";
                    CollectionTitleLabel.Text = "Доступные заставки";
                    await LoadSplashesPreview();

                    await DisplayAlert("Успех",
                        $"✅ Заставка '{splashName}' сохранена!\n\nОна будет отображаться при следующем запуске приложения",
                        "OK");
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

                // Сохраняем выбранный цвет
                _databaseService.SetCurrentColor(colorName);

                await AnimateBackgroundColor(selectedColor);

                var imageUrls = await _imageParserService.ParseGoogleImages(colorName);

                if (imageUrls.Count > 0)
                {
                    // Создаем ImageItem для отображения
                    var imageItems = imageUrls.Select((url, index) => new ImageItem
                    {
                        Title = $"{colorName} - фон {index + 1}",
                        ImageUrl = url,
                        Type = "Color"
                    }).ToList();

                    ImagesCollectionView.ItemsSource = imageItems;

                    // Переключаемся в режим фонов
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
                        $"✅ Найдено {imageUrls.Count} изображений для цвета '{colorName}'\n\nЦвет сохранен и будет применяться при запуске",
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

        private void OnImageSelected(object sender, SelectionChangedEventArgs e)
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
                        this.BackgroundImageSource = ImageSource.FromUri(new Uri(imageItem.ImageUrl));
                    }
                }
                catch (Exception ex)
                {
                    DisplayAlert("Ошибка", $"Не удалось загрузить изображение: {ex.Message}", "OK");
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