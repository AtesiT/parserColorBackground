using Jint;

namespace parserColorBackground.Services
{
    public class JintService
    {
        private readonly Engine _engine;

        public JintService()
        {
            _engine = new Engine();
            InitializeEngine();
        }

        private void InitializeEngine()
        {
            _engine.SetValue("log", new Action<object>(obj =>
            {
                Console.WriteLine(obj?.ToString());
            }));
        }

        public string ExecuteScript(string script)
        {
            try
            {
                // Исправлено: используем правильный метод для получения результата
                var result = _engine.Evaluate(script);
                return result?.ToString() ?? "undefined";
            }
            catch (Exception ex)
            {
                return $"Ошибка: {ex.Message}";
            }
        }

        public void SetValue(string name, object value)
        {
            _engine.SetValue(name, value);
        }

        public object GetValue(string name)
        {
            return _engine.GetValue(name).ToObject();
        }
    }
}