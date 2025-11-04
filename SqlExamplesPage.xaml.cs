namespace parserColorBackground
{
    public partial class SqlExamplesPage : ContentPage
    {
        public SqlExamplesPage()
        {
            InitializeComponent();
        }

        private async void OnCopySql(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string sql)
            {
                try
                {
                    await Clipboard.Default.SetTextAsync(sql);

                    // Визуальная обратная связь
                    var originalText = button.Text;
                    var originalColor = button.BackgroundColor;

                    button.Text = "✅ Скопировано!";
                    button.BackgroundColor = Colors.Green;

                    await Task.Delay(1000);

                    button.Text = originalText;
                    button.BackgroundColor = originalColor;

                    // Закрываем страницу и вставляем SQL в редактор
                    MessagingCenter.Send(this, "SqlCopied", sql);
                    await Navigation.PopModalAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось скопировать: {ex.Message}", "OK");
                }
            }
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}