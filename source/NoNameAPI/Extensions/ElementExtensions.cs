using Autodesk.Revit.DB;

namespace NoNameApi.Extensions;

public static class ElementExtensions
{
    public static Parameter? FetchParameter(this Element element, BuiltInParameter parameter)
    {
        // Получаем тип элемента
        var elementTypeId = element.GetTypeId();
        if (elementTypeId == ElementId.InvalidElementId)
            return null;

        // Получаем элемент типа и проверяем на null
        var elementType = element.Document.GetElement(elementTypeId);
        if (elementType == null)
            return null;

        // Получаем параметр из типа
        var typeParameter = elementType.get_Parameter(parameter);

        // Важно: проверяем, что параметр существует и имеет значение
        if (typeParameter is not null)
            return typeParameter;

        // Проверяем параметр у экземпляра
        var instanceParameter = element.get_Parameter(parameter);
        if (instanceParameter is not null)
            return instanceParameter;

        return null;
    }

    public static Parameter? FetchParameter(this Element element, string parameter)
    {
      // Получаем тип элемента
        var elementTypeId = element.GetTypeId();
        if (elementTypeId == ElementId.InvalidElementId)
            return null;

        // Получаем элемент типа и проверяем на null
        var elementType = element.Document.GetElement(elementTypeId);
        if (elementType == null)
            return null;

        // Получаем параметр из типа
        var typeParameter = elementType.LookupParameter(parameter);

        // Важно: проверяем, что параметр существует и имеет значение
        if (typeParameter is not null)
            return typeParameter;
        
        // Проверяем параметр у экземпляра
        var instanceParameter = element.LookupParameter(parameter);
        if (instanceParameter is not null)
            return instanceParameter;

        return null;
    }

    public static Parameter? FetchParameter(this Element element, Definition definition)
    {
    
        // Получаем тип элемента
        var elementTypeId = element.GetTypeId();
        if (elementTypeId == ElementId.InvalidElementId)
            return null;

        // Получаем элемент типа и проверяем на null
        var elementType = element.Document.GetElement(elementTypeId);
        if (elementType == null)
            return null;

        // Получаем параметр из типа
        var typeParameter = elementType.get_Parameter(definition);

        // Важно: проверяем, что параметр существует и имеет значение
        if (typeParameter is not null)
            return typeParameter;
        
        // Проверяем параметр у экземпляра
        var instanceParameter = element.get_Parameter(definition);
        if (instanceParameter is not null)
            return instanceParameter;

        return null;
    }
}