namespace NoNameApi.Services
{
    public interface IDataLoader
    {
        T LoadData<T>() where T : class;
        void SaveData<T>(T data) where T : class;
    }
}
