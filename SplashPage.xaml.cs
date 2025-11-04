using parserColorBackground.Services;

namespace parserColorBackground
{
    public partial class SplashPage : ContentPage
    {
        private readonly DatabaseService _databaseService;

        public SplashPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await AnimateSplashScreen();
        }

        private async Task AnimateSplashScreen()
        {
            try
            {
                // Проверяем сохраненное изображение заставки
                var savedSplashImageUrl = _databaseService.GetCurrentSplashImageUrl();

                if (!string.IsNullOrEmpty(savedSplashImageUrl))
                {
                    // Загружаем сохраненное изображение
                    SplashImage.Source = ImageSource.FromUri(new Uri(savedSplashImageUrl));
                    this.BackgroundColor = Colors.Transparent; // Делаем фон прозрачным, чтобы видеть изображение
                }
                else
                {
                    // Пробуем загрузить из базы данных
                    var currentSplash = await _databaseService.GetCurrentSplashAsync();

                    if (currentSplash != null && !string.IsNullOrEmpty(currentSplash.ImageUrl))
                    {
                        SplashImage.Source = ImageSource.FromUri(new Uri(currentSplash.ImageUrl));
                        this.BackgroundColor = Colors.Transparent;
                    }
                    else
                    {
                        // Оставляем дефолтный синий фон
                        this.BackgroundColor = Color.FromArgb("#2196F3");
                    }
                }

                // Анимация появления элементов
                await Task.WhenAll(
                    SplashImage.FadeTo(1, 500), // Увеличили непрозрачность до 1
                    AppNameLabel.FadeTo(1, 800),
                    LoadingIndicator.FadeTo(1, 1000),
                    LoadingLabel.FadeTo(1, 1200)
                );

                // Задержка для отображения заставки (2-3 секунды)
                await Task.Delay(2500);

                // Анимация исчезновения
                await Task.WhenAll(
                    SplashImage.FadeTo(0, 500),
                    AppNameLabel.FadeTo(0, 500),
                    LoadingIndicator.FadeTo(0, 500),
                    LoadingLabel.FadeTo(0, 500)
                );

                // Переход на главную страницу
                Application.Current.MainPage = new AppShell();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки заставки: {ex.Message}");
                Application.Current.MainPage = new AppShell();
            }
        }
    }
}