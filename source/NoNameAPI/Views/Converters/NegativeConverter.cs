using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NoNameApi.Views.Converters;

// <summary>
    /// Конвертер значений, который инвертирует знак входящего числового значения (делает его отрицательным).
    /// </summary>
    public class NegationConverter : IValueConverter
    {
        /// <summary>
        /// Инвертирует знак числового значения.
        /// </summary>
        /// <param name="value">Входящее значение (ожидается число).</param>
        /// <param name="targetType">Тип целевого свойства (не используется).</param>
        /// <param name="parameter">Параметр конвертера (не используется).</param>
        /// <param name="culture">Информация о культуре (не используется).</param>
        /// <returns>Число с противоположным знаком или DependencyProperty.UnsetValue в случае ошибки.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Пытаемся преобразовать входящее значение в double
            if (!double.TryParse(value?.ToString(), NumberStyles.Any, culture ?? CultureInfo.InvariantCulture, out double numericValue))
            {
                // Если не удалось, возвращаем специальное значение, указывающее на ошибку привязки
                return DependencyProperty.UnsetValue;
            }

            // Инвертируем знак
            return -numericValue;
        }

        /// <summary>
        /// Обратное преобразование не поддерживается (или можно реализовать как повторное инвертирование).
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
             // Обычно для таких конвертеров обратное преобразование не требуется
            throw new NotSupportedException($"{nameof(NegationConverter)} не поддерживает обратное преобразование.");
             // Или можно вернуть return DependencyProperty.UnsetValue;
             // Или реализовать обратное инвертирование, если это имеет смысл:
             // if (double.TryParse(value?.ToString(), NumberStyles.Any, culture ?? CultureInfo.InvariantCulture, out double numericValue))
             // {
             //     return -numericValue;
             // }
             // return DependencyProperty.UnsetValue;
        }
    }