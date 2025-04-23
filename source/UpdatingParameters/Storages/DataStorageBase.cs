namespace UpdatingParameters.Storages;

public abstract class DataStorageBase : IDataStorage
{
    private readonly List<Element> _elements = [];
    public abstract void InitializeDefault();
    public void UpdateData()
    {
      LoadData();
    }

    public abstract void LoadData();
    public abstract void Save();
    public List<Element> GetElements()
    {
        return _elements;
    }
    protected Element GetElement()
    {
        return _elements?.FirstOrDefault();
    }

    public void AddElement(Element element)
    {
        if (element != null)
        {
          _elements.Add(element);
        }
    }

       
}