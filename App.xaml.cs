namespace parserColorBackground
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Показываем заставку при запуске
            MainPage = new NavigationPage(new SplashPage());
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            // После заставки переходим на главную страницу
            Task.Delay(3500).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage = new AppShell();
                });
            });

            return window;
        }
    }
}