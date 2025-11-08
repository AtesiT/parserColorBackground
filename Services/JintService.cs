using Jint;
using System.Text;

namespace parserColorBackground.Services
{

    /// Сервис для выполнения JavaScript кода в .NET приложении с помощью библиотеки JINT.

    public class JintService
    {
        private readonly Engine _engine;              // Движок JINT для выполнения JavaScript
        private readonly StringBuilder _consoleOutput; // Буфер для накопления логов
        public event EventHandler<string> ConsoleOutput; // Событие вывода в консоль

        public JintService()
        {
            _engine = new Engine();
            _consoleOutput = new StringBuilder();
            InitializeEngine();
        }


        /// Инициализация движка JINT.
        /// Регистрирует все JavaScript функции, доступные в скриптах.

        private void InitializeEngine()
        {
            // ═══════════════════════════════════════════════════════════════
            // СЕКЦИЯ 1: ФУНКЦИИ ЛОГИРОВАНИЯ
            // ═══════════════════════════════════════════════════════════════

            /// JavaScript функция: log(message)
            /// Обычное логирование. Выводит сообщение с префиксом [LOG].
            _engine.SetValue("log", new Action<object>(obj =>
            {
                var message = $"[LOG] {obj?.ToString()}";
                Console.WriteLine(message);
                _consoleOutput.AppendLine(message);
                ConsoleOutput?.Invoke(this, message);
            }));

            /// JavaScript функция: info(message)
            /// Информационные сообщения. Выводит с префиксом [INFO].
            _engine.SetValue("info", new Action<object>(obj =>
            {
                var message = $"[INFO] {obj?.ToString()}";
                Console.WriteLine(message);
                _consoleOutput.AppendLine(message);
                ConsoleOutput?.Invoke(this, message);
            }));

            /// JavaScript функция: warn(message)
            /// Предупреждения. Выводит с префиксом [WARN].
            _engine.SetValue("warn", new Action<object>(obj =>
            {
                var message = $"[WARN] {obj?.ToString()}";
                Console.WriteLine(message);
                _consoleOutput.AppendLine(message);
                ConsoleOutput?.Invoke(this, message);
            }));

            /// JavaScript функция: error(message)
            /// Ошибки. Выводит с префиксом [ERROR].
            _engine.SetValue("error", new Action<object>(obj =>
            {
                var message = $"[ERROR] {obj?.ToString()}";
                Console.WriteLine(message);
                _consoleOutput.AppendLine(message);
                ConsoleOutput?.Invoke(this, message);
            }));

            // ═══════════════════════════════════════════════════════════════
            // СЕКЦИЯ 2: ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
            // ═══════════════════════════════════════════════════════════════

            /// JavaScript функция: clear()
            /// Очищает весь буфер логов консоли.
            _engine.SetValue("clear", new Action(() =>
            {
                _consoleOutput.Clear();
                ConsoleOutput?.Invoke(this, "[Console cleared]");
            }));

            // ═══════════════════════════════════════════════════════════════
            // СЕКЦИЯ 3: ФУНКЦИИ ВАЛИДАЦИИ
            // ═══════════════════════════════════════════════════════════════

            /// JavaScript функция: validate(input)
            /// Проверяет корректность строки: не пустая и длина >= 2 символа.
            /// Возвращает: true/false
            _engine.SetValue("validate", new Func<string, bool>(input =>
            {
                if (string.IsNullOrWhiteSpace(input))
                    return false;

                if (input.Length < 2)
                    return false;

                return true;
            }));

            // ═══════════════════════════════════════════════════════════════
            // СЕКЦИЯ 4: ФУНКЦИИ РАБОТЫ С МАССИВАМИ
            // ═══════════════════════════════════════════════════════════════

            /// JavaScript функция: formatArray(array)
            /// Форматирует массив в строку с разделителями через запятую.
            _engine.SetValue("formatArray", new Func<object[], string>(array =>
            {
                return string.Join(", ", array);
            }));

            // ═══════════════════════════════════════════════════════════════
            // СЕКЦИЯ 5: ФУНКЦИИ ДЛЯ ПАРСИНГА GOOGLE IMAGES
            // ═══════════════════════════════════════════════════════════════

            /// JavaScript функция: buildGoogleQuery(searchTerm, type)
            /// Формирует оптимальный поисковый запрос для Google Images.
            /// Для "color": добавляет "background wallpaper 4k hd"
            /// Для "wallpaper": добавляет "landscape wallpaper 4k hd desktop"
            _engine.SetValue("buildGoogleQuery", new Func<string, string, string>((searchTerm, type) =>
            {
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

            /// JavaScript функция: validateSearchQuery(query)
            /// Проверяет качество поискового запроса.
            /// Возвращает объект: { isValid, hasKeywords, quality }
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

        // ═══════════════════════════════════════════════════════════════
        // ПУБЛИЧНЫЕ МЕТОДЫ СЕРВИСА
        // ═══════════════════════════════════════════════════════════════


        /// Выполняет JavaScript код и возвращает результат в виде строки.
        /// Основной метод для выполнения любого JS кода через движок JINT.

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


        /// Валидирует строку через JavaScript функцию validate().
        /// Проверяет что строка не пустая и длина >= 2 символа.

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


        /// Строит оптимальный поисковый запрос для Google Images через JavaScript.
        /// Использует buildGoogleQuery() для формирования и validateSearchQuery() для проверки.

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


        /// Анализирует результаты парсинга изображений через JavaScript.
        /// Оценивает качество: отличное (>=5), хорошее (>=3), удовлетворительное (<3).
        /// Логирует результат и дает рекомендации.
        public string AnalyzeParsingResults(int foundCount, string searchTerm)
        {
            try
            {
                SetValue("foundCount", foundCount);
                SetValue("searchTerm", searchTerm);

                var script = @"
                    var analysis = {
                        success: foundCount > 0,
                        quality: foundCount >= 5 ? 'отличное' 
                               : foundCount >= 3 ? 'хорошее' 
                               : 'удовлетворительное',
                        recommendation: foundCount === 0 
                            ? 'Попробуйте другое название' 
                            : 'Результат найден'
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

        // ═══════════════════════════════════════════════════════════════
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ═══════════════════════════════════════════════════════════════


        /// Устанавливает значение JavaScript переменной.
        /// Передает данные из C# в JavaScript контекст.

        public void SetValue(string name, object value)
        {
            _engine.SetValue(name, value);
        }


        /// Получает значение JavaScript переменной.
        /// Конвертирует JS значение в объект .NET.

        public object GetValue(string name)
        {
            return _engine.GetValue(name).ToObject();
        }

        /// Возвращает весь накопленный вывод консоли.
        /// Полезно для отображения полной истории логов.
        public string GetConsoleOutput()
        {
            return _consoleOutput.ToString();
        }

        /// Очищает весь буфер консоли.
        /// Удаляет все накопленные логи.
        public void ClearConsole()
        {
            _consoleOutput.Clear();
        }
    }
}