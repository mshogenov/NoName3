using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using UpdatingParameters.Models;

namespace UpdatingParameters.Services
{
    public class FormulaCollectionHandler
    {
        private readonly ObservableCollection<Formula> _collection;
        public Action OnCollectionChangedAction { get; set; }

        public FormulaCollectionHandler(ObservableCollection<Formula> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
           

            // Подписываемся на событие CollectionChanged
            _collection.CollectionChanged += CollectionChanged;

            // Подписываемся на PropertyChanged для уже существующих элементов
            foreach (var item in _collection)
            {
                SubscribeToItem(item);
            }
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Подписываемся на новые элементы
            if (e.NewItems != null)
            {
                foreach (Formula newItem in e.NewItems)
                {
                    SubscribeToItem(newItem);
                }
            }

            // Отписываемся от старых элементов
            if (e.OldItems != null)
            {
                foreach (Formula oldItem in e.OldItems)
                {
                    UnsubscribeFromItem(oldItem);
                }
            }

            // Вызываем действие при изменении коллекции
            OnCollectionChangedAction.Invoke();
        }

        private void SubscribeToItem(Formula item)
        {
            if (item != null)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }
        }

        private void UnsubscribeFromItem(Formula item)
        {
            if (item != null)
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Formula.Prefix) || e.PropertyName == nameof(Formula.Suffix) || e.PropertyName==nameof(Formula.Stockpile) || e.PropertyName == nameof(Formula.MeasurementUnit))
            {
                OnCollectionChangedAction.Invoke();
            }
          
        }

        // Метод для очистки подписок, если это необходимо
        public void Dispose()
        {
            _collection.CollectionChanged -= CollectionChanged;
            foreach (var item in _collection)
            {
                UnsubscribeFromItem(item);
            }
        }
    }
}
