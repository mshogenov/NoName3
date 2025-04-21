using UpdatingParameters.Models;
using UpdatingParameters.Storages;
using UpdatingParameters.Storages.DuctInsulation;
using UpdatingParameters.Storages.Ducts;
using UpdatingParameters.Storages.FlexPipes;
using UpdatingParameters.Storages.Parameters;
using UpdatingParameters.Storages.PipeInsulationMtl;
using UpdatingParameters.Storages.Pipes;
using UpdatingParameters.Storages.Settings;

namespace UpdatingParameters.Services;

public  class DataStorageFactory
{
    private readonly Dictionary<Type, Lazy<IDataStorage>> _storages = new();
    private readonly Dictionary<Type, Func<IDataStorage>> _creators;
    
    public  DataStorageFactory()
    {
        _creators = new Dictionary<Type, Func<IDataStorage>>
        {
            { typeof(ParametersDataStorage), () => new ParametersDataStorage(new JsonDataLoader("AllCategoriesDataStorage.json")) },
            { typeof(PipesWithoutDataStorage), () => new PipesWithoutDataStorage(new JsonDataLoader("formulas_without.json")) },
            { typeof(PipesOuterDiameterDataStorage), () => new PipesOuterDiameterDataStorage(new JsonDataLoader("formulas_outerDiameter.json")) },
            { typeof(PipesInternalDiameterDataStorage), () => new PipesInternalDiameterDataStorage(new JsonDataLoader("formulas_internalDiameter.json")) },
            { typeof(FlexPipeWithoutDataStorage), () => new FlexPipeWithoutDataStorage(new JsonDataLoader("formulas_flexPipesOuterDiameter.json")) },
            { typeof(FlexPipesCorrugationsDataStorage), () => new FlexPipesCorrugationsDataStorage(new JsonDataLoader("formulas_flexPipesCorrugations.json")) },
            { typeof(FlexPipesConnectionsDataStorage), () => new FlexPipesConnectionsDataStorage(new JsonDataLoader("formulas_FlexPipesConnections.json")) },
            { typeof(PipeInsulationCylindersDataStorage), () => new PipeInsulationCylindersDataStorage(new JsonDataLoader("formulas_PipeInsulationCylinders.json")) },
            { typeof(PipeInsulationTubesDataStorage), () => new PipeInsulationTubesDataStorage(new JsonDataLoader("formulas_PipeInsulationTubes.json")) },
            { typeof(PipeInsulationColouredTubesDataStorage), () => new PipeInsulationColouredTubesDataStorage(new JsonDataLoader("formulas_PipeInsulationColouredTubesDataStorage.json")) },
            { typeof(SettingsDataStorage), () => new SettingsDataStorage(new JsonDataLoader("Settings.json")) },
            { typeof(DuctInsulationFireproofingDataStorage), () => new DuctInsulationFireproofingDataStorage(new JsonDataLoader("formulas_DuctInsulationFireproofingDataStorage.json")) },
            { typeof(DuctInsulationThermalDataStorage), () => new DuctInsulationThermalDataStorage(new JsonDataLoader("formulas_DuctInsulationThermalDataStorage.json")) },
            { typeof(DuctConnectionPartsDataStorage), () => new DuctConnectionPartsDataStorage(new JsonDataLoader("formulas_DuctConnectionPartsDataStorage.json")) },
            { typeof(DuctPlasticDataStorage), () => new DuctPlasticDataStorage(new JsonDataLoader("formulas_DuctPlasticDataStorage.json")) },
            { typeof(DuctRectangularDataStorage), () => new DuctRectangularDataStorage(new JsonDataLoader("formulas_DuctRectangularDataStorage.json")) },
            { typeof(DuctRoundDataStorage), () => new DuctRoundDataStorage(new JsonDataLoader("formulas_DuctRoundDataStorage.json")) },
            { typeof(DuctWithoutDataStorage), () => new DuctWithoutDataStorage(new JsonDataLoader("formulas_DuctWithoutDataStorage.json")) },
            { typeof(FlexibleDuctsRoundDataStorage), () => new FlexibleDuctsRoundDataStorage(new JsonDataLoader("formulas_FlexibleDuctsRoundDataStorage.json")) },
            { typeof(DuctParametersDataStorage), () => new DuctParametersDataStorage(new JsonDataLoader("DuctParametersDataStorage.json")) },
        };
    }
    public void InitializeStorages(params Type[] types)
    {
        foreach (var type in types)
        {
            GetStorageByType(type);
        }
    }
    private IDataStorage GetStorageByType(Type type)
    {
        if (_storages.TryGetValue(type, out var lazyStorage)) return lazyStorage.Value;
        if (!_creators.TryGetValue(type, out var creator))
        {
            throw new KeyNotFoundException($"Creator for storage type {type} not found");
        }
        lazyStorage = new Lazy<IDataStorage>(creator);
        _storages[type] = lazyStorage;

        return lazyStorage.Value;
    }
    public IEnumerable<IDataStorage> GetAllStorages()
    {
        return _storages.Values.Select(x => x.Value);
    }
    public T GetStorage<T>() where T : class, IDataStorage
    {
        var type = typeof(T);

        if (_storages.TryGetValue(type, out var lazyStorage)) return lazyStorage.Value as T;
        if (!_creators.TryGetValue(type, out var creator))
        {
            throw new KeyNotFoundException($"Creator for storage type {type} not found");
        }
        lazyStorage = new Lazy<IDataStorage>(creator);
        _storages[type] = lazyStorage;

        return lazyStorage.Value as T;
    }
   
   
    public void UpdateStorage<T>() where T : class, IDataStorage
    {
        var type = typeof(T);

        // Проверяем, существует ли хранилище
        if (_storages.TryGetValue(type, out var storage))
        {
            // Перезагружаем данные
            storage.Value.UpdateData();
        }
        else
        {
            // Если хранилище не инициализировано, можно создать его
            // или выбросить исключение, если это нежелательно
            // Здесь я предполагаю, что мы создаем и инициализируем хранилище
            GetStorage<T>();
        }
    }
   
    public void InitializeAllStorages()
    {
        var allTypes = _creators.Keys.ToArray();
        InitializeStorages(allTypes);
    }
}