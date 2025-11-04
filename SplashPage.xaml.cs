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
                // Загружаем текущую заставку из базы данных
                var currentSplash = await _databaseService.GetCurrentSplashAsync();

                if (currentSplash != null && !string.IsNullOrEmpty(currentSplash.ImageUrl))
                {
                    SplashImage.Source = ImageSource.FromUri(new Uri(currentSplash.ImageUrl));
                }

                // Анимация появления элементов
                await Task.WhenAll(
                    SplashImage.FadeTo(0.3, 500),
                    AppNameLabel.FadeTo(1, 800),
                    LoadingIndicator.FadeTo(1, 1000),
                    LoadingLabel.FadeTo(1, 1200)
                );

                // Задержка для отображения заставки
                await Task.Delay(2000);

                // Анимация исчезновения
                await Task.WhenAll(
                    SplashImage.FadeTo(0, 500),
                    AppNameLabel.FadeTo(0, 500),
                    LoadingIndicator.FadeTo(0, 500),
                    LoadingLabel.FadeTo(0, 500)
                );

                // Переход на главную страницу
                await Shell.Current.GoToAsync("//MainPage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки заставки: {ex.Message}");
                await Shell.Current.GoToAsync("//MainPage");
            }
        }
    }
}