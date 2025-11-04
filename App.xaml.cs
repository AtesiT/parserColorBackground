namespace parserColorBackground
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Всегда показываем заставку при запуске
            MainPage = new SplashPage();
        }
    }
}