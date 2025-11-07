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

            // Новая функция: формирование Google поискового запроса
            _engine.SetValue("buildGoogleQuery", new Func<string, string, string>((searchTerm, type) =>
            {
                // type: "color" или "wallpaper"
                if (type == "color")
                {
                    return $"{searchTerm} background wallpaper 4k hd";
                }
                else if (type == "wallpaper")
                {
                    return $"{searchTerm} landscape wallpaper 4k hd desktop";
                }
                return searchTerm;
            }));

            // Новая функция: проверка качества поискового запроса
            _engine.SetValue("validateSearchQuery", new Func<string, object>(query =>
            {
                var result = new
                {
                    isValid = query.Length >= 5,
                    hasKeywords = query.Contains("wallpaper") || query.Contains("background"),
                    quality = query.Contains("4k") || query.Contains("hd") ? "high" : "normal"
                };
                return result;
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

                ExecuteScript($"log('Валидация \"{input}\": {result}')");

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

        // Новый метод: построение поискового запроса через JINT
        public string BuildSearchQuery(string searchTerm, string type)
        {
            try
            {
                SetValue("searchTerm", searchTerm);
                SetValue("type", type);

                var script = @"
                    var query = buildGoogleQuery(searchTerm, type);
                    log('🔍 Построен поисковый запрос: ' + query);
                    
                    var validation = validateSearchQuery(query);
                    if (validation.isValid) {
                        info('✅ Запрос валиден. Качество: ' + validation.quality);
                    } else {
                        warn('⚠️ Запрос может быть недостаточно точным');
                    }
                    
                    query;
                ";

                return ExecuteScript(script);
            }
            catch (Exception ex)
            {
                ExecuteScript($"error('Ошибка построения запроса: {ex.Message}')");
                return searchTerm;
            }
        }

        // Новый метод: анализ результатов парсинга
        public string AnalyzeParsingResults(int foundCount, string searchTerm)
        {
            try
            {
                SetValue("foundCount", foundCount);
                SetValue("searchTerm", searchTerm);

                var script = @"
                    var analysis = {
                        success: foundCount > 0,
                        quality: foundCount >= 5 ? 'отличное' : foundCount >= 3 ? 'хорошее' : 'удовлетворительное',
                        recommendation: foundCount === 0 ? 'Попробуйте другое название' : 'Результат найден'
                    };
                    
                    if (analysis.success) {
                        info('✅ Найдено изображений: ' + foundCount + '. Качество: ' + analysis.quality);
                    } else {
                        warn('⚠️ Изображения не найдены. ' + analysis.recommendation);
                    }
                    
                    var message = 'Для запроса «' + searchTerm + '» найдено: ' + foundCount + ' изображений';
                    log(message);
                    message;
                ";

                return ExecuteScript(script);
            }
            catch (Exception ex)
            {
                return $"Ошибка анализа: {ex.Message}";
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