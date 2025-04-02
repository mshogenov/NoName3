using ElementsTypicalFloor.ViewModels;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;

namespace ElementsTypicalFloor.Views
{
    public sealed partial class ElementsTypicalFloorView
    {
        private static readonly Regex _regex = new Regex(@"^[+-]?(\d+([.,]\d*)?)?$");
        private static readonly Regex _regexTypicalFloorsCount = new Regex("[^0-9]+");
        public ElementsTypicalFloorView(ElementsTypicalFloorViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            string fullText = GetFullText(textBox, e.Text);
            e.Handled = !IsTextValid(fullText);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pasteText = (string)e.DataObject.GetData(typeof(string));
                TextBox textBox = sender as TextBox;

                string fullText = GetFullText(textBox, pasteText);
                if (!IsTextValid(fullText))
                {
                    e.CancelCommand(); // Отменяем вставку, если текст не является допустимым числом
                }
            }
            else
            {
                e.CancelCommand(); // Отменяем вставку, если данные не строкового типа
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Запрет на ввод пробела
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }
        // Метод проверки текста на допустимость
        private bool IsTextValid(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true; // Допускаем пустую строку

            // Проверяем соответствие регулярному выражению
            if (!_regex.IsMatch(text))
                return false;
            // Подсчитываем количество точек и запятых
            int dotCount = text.Split('.').Length - 1;
            int commaCount = text.Split(',').Length - 1;

            // Разрешаем только одно разделение (либо точку, либо запятую)
            if (dotCount > 1 || commaCount > 1)
                return false;

            if (dotCount > 0 && commaCount > 0)
                return false; // Не разрешаем использование обоих разделителей одновременно
                              // Если текст состоит только из одного символа '-' или '+', считаем его допустимым
            if (text == "-" || text == "+")
                return true;

            // Получаем текущий десятичный разделитель
            var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            // Если используется запятая как разделитель, заменяем ее на точку для парсинга
            if (decimalSeparator == ",")
            {
                text = text.Replace('.', ',');
            }
            else
            {
                // Иначе, заменяем запятую на точку
                text = text.Replace(',', '.');
            }

            // Попытка парсинга числа
            double temp;
            return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out temp);
        }
        // Метод для формирования полной строки после ввода символа
        private string GetFullText(TextBox textBox, string input)
        {
            if (textBox == null) return input;
            string pre = textBox.Text.Substring(0, textBox.SelectionStart);
            string post = textBox.Text.Substring(textBox.SelectionStart + textBox.SelectionLength);
            return pre + input + post;
        }

        private void TextBox_PreviewTextInput_1(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            string text = GetFullText(textBox, e.Text);
            e.Handled = _regexTypicalFloorsCount.IsMatch(text);

        }
    }
}