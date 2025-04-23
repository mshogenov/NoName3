using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NoNameApi.Views.Converters;

 /// <summary>
    /// Конвертер значений, который умножает входящее числовое значение
    /// на число, переданное в параметре.
    /// </summary>
    public class MultiplyConverter : IValueConverter
    {
        /// <summary>
        /// Умножает значение на параметр.
        /// </summary>
        /// <param name="value">Входящее значение (ожидается число).</param>
        /// <param name="targetType">Тип целевого свойства (не используется).</param>
        /// <param name="parameter">Параметр конвертера (ожидается число, на которое нужно умножить).</param>
        /// <param name="culture">Информация о культуре (не используется).</param>
        /// <returns>Результат умножения или DependencyProperty.UnsetValue в случае ошибки.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Пытаемся преобразовать входящее значение в double
            if (!double.TryParse(value?.ToString(), NumberStyles.Any, culture ?? CultureInfo.InvariantCulture, out double numericValue))
            {
                // Если не удалось, возвращаем специальное значение, указывающее на ошибку привязки
                return DependencyProperty.UnsetValue;
            }

            // Пытаемся преобразовать параметр в double
            if (!double.TryParse(parameter?.ToString(), NumberStyles.Any, culture ?? CultureInfo.InvariantCulture, out double multiplier))
            {
                // Если параметр не число, возвращаем ошибку
                return DependencyProperty.UnsetValue;
            }

            // Выполняем умножение
            return numericValue * multiplier;
        }

        /// <summary>
        /// Обратное преобразование не поддерживается.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Обычно для таких конвертеров обратное преобразование не требуется
            throw new NotSupportedException($"{nameof(MultiplyConverter)} не поддерживает обратное преобразование.");
            // Или можно вернуть return DependencyProperty.UnsetValue;
        }
    }