using System.Windows;
using UpdatingParameters.Models;

namespace UpdatingParameters.Storages;

public class SetMarginDataStorage : IDataStorage
{
    private readonly IDataLoader _dataLoader;
    private List<MarginCategory> _marginCategories;
    private readonly Document _document = Context.ActiveDocument;
    private readonly List<ParameterWrp> _allParameters;

    public IReadOnlyList<MarginCategory> MarginCategories => _marginCategories.AsReadOnly();
    public static int MarginUpdateCallCount { get; set; }

    public static event EventHandler OnSetMarginDataStorageChanged;

    // События для уведомления об изменениях
    public event EventHandler<MarginCategory> CategoryAdded;
    public event EventHandler<MarginCategory> CategoryRemoved;
    public event EventHandler DataChanged;

    public SetMarginDataStorage(IDataLoader dataLoader)
    {
        _dataLoader = dataLoader;
        _allParameters = GetAllDocumentParameters(_document);
        LoadData();
      MarginUpdateCallCount++;
    }

    private void LoadData()
    {
        try
        {
            var loadedDto = _dataLoader.LoadData<List<MarginCategoryDto>>();
            if (loadedDto == null)
            {
                _marginCategories = [];
            }
            else
            {
                _marginCategories = loadedDto.Select(dto => new MarginCategory
                    {
                        Category = GetCategoryById(dto.CategoryId),
                        OriginalFromParameterName = dto.FromParameterName,
                        OriginalInParameterName = dto.InParameterName,
                        IsChecked = dto.IsChecked,
                        Margin = dto.Margin,
                        IsCopyInParameter = dto.IsCopyInParameter,
                        IsFromParameterValid = IsParameterValid(dto.FromParameterName),
                        IsInParameterValid = IsParameterValid(dto.InParameterName)
                    })
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            _marginCategories = [];
        }
    }

    private bool IsParameterValid(string parameterName)
    {
        return _allParameters.Any(x => x.Name == parameterName);
    }

    public void AddCategory(MarginCategory category)
    {
        if (category == null) return;
        _marginCategories.Add(category);
        CategoryAdded?.Invoke(this, category);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveCategory(MarginCategory category)
    {
        if (category == null) return;

        if (_marginCategories.Remove(category))
        {
            CategoryRemoved?.Invoke(this, category);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void UpdateCategory(MarginCategory oldCategory, MarginCategory newCategory)
    {
        var index = _marginCategories.IndexOf(oldCategory);
        if (index >= 0)
        {
            _marginCategories[index] = newCategory;
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void InitializeDefault()
    {
        _marginCategories = new List<MarginCategory>();
    }

    public void UpdateData()
    {
        LoadData();
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Save()
    {
        try
        {
            // DTO используем ТОЛЬКО при сохранении в файл
            var dtoList = _marginCategories
                .Select(x => new MarginCategoryDto()
                {
                    CategoryId = x.Category.Id.Value,
                    FromParameterName = x.FromParameterName,
                    InParameterName = x.InParameterName,
                    IsChecked = x.IsChecked,
                    Margin = x.Margin,
                    IsCopyInParameter = x.IsCopyInParameter
                })
                .ToList();

            _dataLoader.SaveData(dtoList);
            OnSetMarginDataStorageChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения: {ex.Message}");
        }
    }

    private Category GetCategoryById(long categoryId)
    {
        if (categoryId == -1) return null;
        return Category.GetCategory(Context.ActiveDocument, new ElementId(categoryId));
    }

    public List<ParameterWrp> GetAllDocumentParameters(Document doc)
    {
        List<ParameterWrp> allParams = new List<ParameterWrp>();

        BindingMap bindingMap = doc.ParameterBindings;
        DefinitionBindingMapIterator iterator = bindingMap.ForwardIterator();

        while (iterator.MoveNext())
        {
            Definition def = iterator.Key;
            ElementBinding binding = iterator.Current as ElementBinding;

            ParameterWrp paramWrp = new ParameterWrp
            {
                Name = def.Name,
                IsInstance = binding is InstanceBinding,

                Categories = new List<string>()
            };

            // Проверяем, является ли shared parameter
            if (def is ExternalDefinition extDef)
            {
                paramWrp.IsShared = true;
                paramWrp.Guid = extDef.GUID;
            }

            // Получаем категории
            foreach (Category cat in binding.Categories)
            {
                paramWrp.Categories.Add(cat.Name);
            }

            allParams.Add(paramWrp);
        }

        return allParams;
    }
}