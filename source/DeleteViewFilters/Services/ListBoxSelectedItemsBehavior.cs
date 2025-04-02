using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Collections.Specialized;
using System.Collections;

namespace DeleteViewFilters.Services
{
    public class ListBoxSelectedItemsBehavior : Behavior<ListBox>
    {
        public static readonly DependencyProperty SelectedItemsProperty =
             DependencyProperty.Register(
                 "SelectedItems",
                 typeof(IList),
                 typeof(ListBoxSelectedItemsBehavior),
                 new PropertyMetadata(null, OnSelectedItemsChanged));

        public IList SelectedItems
        {
            get => (IList)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBoxSelectedItemsBehavior behavior)
            {
                if (e.OldValue is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= behavior.OnCollectionChanged;
                }

                if (e.NewValue is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged += behavior.OnCollectionChanged;
                }
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SynchronizeSelectedItems();
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged += OnListBoxSelectionChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SelectionChanged -= OnListBoxSelectionChanged;
        }

        private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedItems == null) return;
            foreach (var item in e.RemovedItems)
            {
                SelectedItems.Remove(item);
            }

            foreach (var item in e.AddedItems)
            {
                SelectedItems.Add(item);
            }
        }

        private void SynchronizeSelectedItems()
        {
            if (AssociatedObject == null || SelectedItems == null) return;

            if (AssociatedObject.SelectedItems == null) return;

            AssociatedObject.SelectedItems.Clear();
            foreach (var item in SelectedItems)
            {
                AssociatedObject.SelectedItems.Add(item);
            }
        }
    }
}
