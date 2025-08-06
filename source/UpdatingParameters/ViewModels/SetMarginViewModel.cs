using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using UpdatingParameters.Models;
using UpdatingParameters.Storages;
using UpdatingParameters.Views;

namespace UpdatingParameters.ViewModels;

public partial class SetMarginViewModel : ViewModelBase
{
    private readonly SetMarginDataStorage _marginDataStorage;
    private readonly Dispatcher _uiDispatcher;
    [ObservableProperty] private ObservableCollection<MarginCategory> _marginCategories;


    public SetMarginViewModel(SetMarginDataStorage marginDataStorage)
    {
        _marginDataStorage = marginDataStorage;
        // Сохраняем dispatcher UI потока
        _uiDispatcher = Dispatcher.CurrentDispatcher;
        MarginCategories = new ObservableCollection<MarginCategory>(_marginDataStorage.MarginCategories);
        _marginDataStorage.CategoryAdded += OnCategoryAdded;
        _marginDataStorage.CategoryRemoved += OnCategoryRemoved;
        _marginDataStorage.DataChanged += OnDataChanged;
    }

    [RelayCommand]
    private void AddCategory()
    {
        AddCategoryWindow addCategoryWindow = new AddCategoryWindow();
        AddCategoryVM addCategoryVM = new AddCategoryVM();
        addCategoryWindow.DataContext = addCategoryVM;
        // Показываем диалог и проверяем результат
        if (addCategoryWindow.ShowDialog() == true && addCategoryVM.IsConfirmed)
        {
            _marginDataStorage.AddCategory(addCategoryVM.Result);
            _marginDataStorage.Save();
        }
    }

    [RelayCommand]
    private void RemoveCategory(MarginCategory category)
    {
        _marginDataStorage.RemoveCategory(category);
        _marginDataStorage.Save();
    }

    // Обработчики событий storage
    private void OnCategoryAdded(object sender, MarginCategory category)
    {
        if (_uiDispatcher.CheckAccess())
        {
            MarginCategories.Add(category);
        }
        else
        {
            _uiDispatcher.Invoke(() => MarginCategories.Add(category));
        }
    }

    private void OnCategoryRemoved(object sender, MarginCategory category)
    {
        if (_uiDispatcher.CheckAccess())
        {
            MarginCategories.Remove(category);
        }
        else
        {
            _uiDispatcher.Invoke(() => MarginCategories.Remove(category));
        }
    }

    private void OnDataChanged(object sender, EventArgs e)
    {
        if (_uiDispatcher.CheckAccess())
        {
            RefreshCategories();
        }
        else
        {
            _uiDispatcher.Invoke(RefreshCategories);
        }
    }

   
    private void RefreshCategories()
    {
        MarginCategories.Clear();
        foreach (var category in _marginDataStorage.MarginCategories)
        {
            MarginCategories.Add(category);
        }
    }
}