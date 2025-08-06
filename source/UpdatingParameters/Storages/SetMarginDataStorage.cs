using System.Windows;
using UpdatingParameters.Models;

namespace UpdatingParameters.Storages;

public class SetMarginDataStorage : IDataStorage
{
    private readonly IDataLoader _dataLoader;
    private List<MarginCategory> _marginCategories;
    private readonly Document _document = Context.ActiveDocument;

    public IReadOnlyList<MarginCategory> MarginCategories => _marginCategories.AsReadOnly();
    public static event EventHandler OnSetMarginDataStorageChanged;

    // События для уведомления об изменениях
    public event EventHandler<MarginCategory> CategoryAdded;
    public event EventHandler<MarginCategory> CategoryRemoved;
    public event EventHandler DataChanged;

    public SetMarginDataStorage(IDataLoader dataLoader)
    {
        _dataLoader = dataLoader;
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            // DTO используем ТОЛЬКО при загрузке из файла
            var loadedDto = _dataLoader.LoadData<List<MarginCategoryDto>>();
            if (loadedDto == null)
            {
                _marginCategories = new List<MarginCategory>();
            }
            else
            {
                // Конвертируем DTO → MarginCategory
                _marginCategories = loadedDto
                    .Select(ConvertFromDto)
                    .Where(mc => mc != null)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            _marginCategories = new List<MarginCategory>();
        }
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
                .Select(ConvertToDto)
                .ToList();

            _dataLoader.SaveData(dtoList);
            OnSetMarginDataStorageChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения: {ex.Message}");
        }
    }

    private MarginCategoryDto ConvertToDto(MarginCategory marginCategory)
    {
        return new MarginCategoryDto
        {
            CategoryName = marginCategory.Category?.Name,
            CategoryId = marginCategory.Category?.Id?.Value ?? -1,
            Margin = marginCategory.Margin,
            IsChecked = marginCategory.IsChecked,
            FromParameter = ConvertParameterToDto(marginCategory.FromParameter),
            InParameter = ConvertParameterToDto(marginCategory.InParameter)
        };
    }

    private MarginCategory ConvertFromDto(MarginCategoryDto dto)
    {
        try
        {
            return new MarginCategory
            {
                Category = GetCategoryById(dto.CategoryId),
                Margin = dto.Margin,
                IsChecked = dto.IsChecked,
                FromParameter = GetParameterById(dto.FromParameter, dto.CategoryId),
                InParameter = GetParameterById(dto.InParameter, dto.CategoryId),
                OriginalFromParameterName = dto.FromParameter?.Name,
                OriginalInParameterName = dto.InParameter?.Name
            };
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    private ParameterDto ConvertParameterToDto(Parameter parameter)
    {
        if (parameter == null) return null;

        return new ParameterDto
        {
            Name = parameter.Definition?.Name,
            Id = parameter.Id?.Value ?? -1,
            Definition = parameter.Definition?.Name,
            StorageType = parameter.StorageType.ToString()
        };
    }

    private Category GetCategoryById(long categoryId)
    {
        if (categoryId == -1) return null;
        return Category.GetCategory(_document, new ElementId(categoryId));
    }

    private Parameter GetParameterById(ParameterDto dtoFromParameter, long categoryId)
    {
        Category category = GetCategoryById(categoryId);
        FilteredElementCollector collector = new FilteredElementCollector(_document);
        Element element = collector.OfCategory(category.BuiltInCategory)
            .WhereElementIsNotElementType()
            .FirstElement();
        return element?.FindParameter(dtoFromParameter.Name);
    }
}