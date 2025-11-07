using Jint;
using System.Text;

namespace parserColorBackground.Services
{
    public class JintService
    {
        private readonly Engine _engine;
        private readonly StringBuilder _consoleOutput;
        public event EventHandler<string> ConsoleOutput;

        public JintService()
        {
            _engine = new Engine();
            _consoleOutput = new StringBuilder();
            InitializeEngine();
        }

        private void InitializeEngine()
        {
            // Расширенная консоль с разными уровнями
            _engine.SetValue("log", new Action<object>(obj =>
            {
                var message = $"[LOG] {obj?.ToString()}";
                Console.WriteLine(message);
                _consoleOutput.AppendLine(message);
                ConsoleOutput?.Invoke(this, message);
            }));

            _engine.SetValue("info", new Action<object>(obj =>
            {
                var message = $"[INFO] {obj?.ToString()}";
                Console.WriteLine(message);
                _consoleOutput.AppendLine(message);
                ConsoleOutput?.Invoke(this, message);
            }));

            _engine.SetValue("warn", new Action<object>(obj =>
            {
                var message = $"[WARN] {obj?.ToString()}";
                Console.WriteLine(message);
                _consoleOutput.AppendLine(message);
                ConsoleOutput?.Invoke(this, message);
            }));

            _engine.SetValue("error", new Action<object>(obj =>
            {
                var message = $"[ERROR] {obj?.ToString()}";
                Console.WriteLine(message);
                _consoleOutput.AppendLine(message);
                ConsoleOutput?.Invoke(this, message);
            }));

            // Вспомогательные функции
            _engine.SetValue("clear", new Action(() =>
            {
                _consoleOutput.Clear();
                ConsoleOutput?.Invoke(this, "[Console cleared]");
            }));

            // Функция для валидации данных
            _engine.SetValue("validate", new Func<string, bool>(input =>
            {
                if (string.IsNullOrWhiteSpace(input))
                    return false;

                if (input.Length < 2)
                    return false;

                return true;
            }));

            // Функция для форматирования массива
            _engine.SetValue("formatArray", new Func<object[], string>(array =>
            {
                return string.Join(", ", array);
            }));
        }

        public string ExecuteScript(string script)
        {
            try
            {
                var result = _engine.Evaluate(script);
                return result?.ToString() ?? "undefined";
            }
            catch (Exception ex)
            {
                var errorMsg = $"Ошибка JINT: {ex.Message}";
                _consoleOutput.AppendLine(errorMsg);
                return errorMsg;
            }
        }

        // Метод для валидации строки через JINT
        public bool ValidateString(string input)
        {
            try
            {
                SetValue("inputToValidate", input);
                var result = ExecuteScript("validate(inputToValidate)");

                // Логируем результат валидации
                ExecuteScript($"log('Валидация \"{input}\": {result}')");

                // Проверяем результат (может быть "true", "True", "1", true и т.д.)
                return result.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                       result == "True" ||
                       result == "1";
            }
            catch (Exception ex)
            {
                ExecuteScript($"error('Ошибка валидации: {ex.Message}')");
                return false;
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

        public string GetConsoleOutput()
        {
            return _consoleOutput.ToString();
        }

        public void ClearConsole()
        {
            _consoleOutput.Clear();
        }
    }
}