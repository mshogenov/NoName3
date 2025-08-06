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

public class DataStorageFactory
{
    private readonly Dictionary<Type, IDataStorage> _storages = new();
    private readonly Dictionary<Type, Func<IDataStorage>> _creators;

    public DataStorageFactory()
    {
        _creators = new Dictionary<Type, Func<IDataStorage>>
        {
            {
                typeof(ParametersDataStorage),
                () => new ParametersDataStorage(new JsonDataLoader("AllCategoriesDataStorage.json"))
            },
            {
                typeof(PipesWithoutDataStorage),
                () => new PipesWithoutDataStorage(new JsonDataLoader("formulas_without.json"))
            },
            {
                typeof(PipesOuterDiameterDataStorage),
                () => new PipesOuterDiameterDataStorage(new JsonDataLoader("formulas_outerDiameter.json"))
            },
            {
                typeof(PipesInternalDiameterDataStorage),
                () => new PipesInternalDiameterDataStorage(new JsonDataLoader("formulas_internalDiameter.json"))
            },
            {
                typeof(FlexPipeWithoutDataStorage),
                () => new FlexPipeWithoutDataStorage(new JsonDataLoader("formulas_flexPipesOuterDiameter.json"))
            },
            {
                typeof(FlexPipesCorrugationsDataStorage),
                () => new FlexPipesCorrugationsDataStorage(new JsonDataLoader("formulas_flexPipesCorrugations.json"))
            },
            {
                typeof(FlexPipesConnectionsDataStorage),
                () => new FlexPipesConnectionsDataStorage(new JsonDataLoader("formulas_FlexPipesConnections.json"))
            },
            {
                typeof(PipeInsulationCylindersDataStorage),
                () => new PipeInsulationCylindersDataStorage(
                    new JsonDataLoader("formulas_PipeInsulationCylinders.json"))
            },
            {
                typeof(PipeInsulationTubesDataStorage),
                () => new PipeInsulationTubesDataStorage(new JsonDataLoader("formulas_PipeInsulationTubes.json"))
            },
            {
                typeof(PipeInsulationColouredTubesDataStorage),
                () => new PipeInsulationColouredTubesDataStorage(
                    new JsonDataLoader("formulas_PipeInsulationColouredTubesDataStorage.json"))
            },
            { typeof(SettingsDataStorage), () => new SettingsDataStorage(new JsonDataLoader("Settings.json")) },
            {
                typeof(DuctInsulationFireproofingDataStorage),
                () => new DuctInsulationFireproofingDataStorage(
                    new JsonDataLoader("formulas_DuctInsulationFireproofingDataStorage.json"))
            },
            {
                typeof(DuctInsulationThermalDataStorage),
                () => new DuctInsulationThermalDataStorage(
                    new JsonDataLoader("formulas_DuctInsulationThermalDataStorage.json"))
            },
            {
                typeof(DuctConnectionPartsDataStorage),
                () => new DuctConnectionPartsDataStorage(
                    new JsonDataLoader("formulas_DuctConnectionPartsDataStorage.json"))
            },
            {
                typeof(DuctPlasticDataStorage),
                () => new DuctPlasticDataStorage(new JsonDataLoader("formulas_DuctPlasticDataStorage.json"))
            },
            {
                typeof(DuctRectangularDataStorage),
                () => new DuctRectangularDataStorage(new JsonDataLoader("formulas_DuctRectangularDataStorage.json"))
            },
            {
                typeof(DuctRoundDataStorage),
                () => new DuctRoundDataStorage(new JsonDataLoader("formulas_DuctRoundDataStorage.json"))
            },
            {
                typeof(DuctWithoutDataStorage),
                () => new DuctWithoutDataStorage(new JsonDataLoader("formulas_DuctWithoutDataStorage.json"))
            },
            {
                typeof(FlexibleDuctsRoundDataStorage),
                () => new FlexibleDuctsRoundDataStorage(
                    new JsonDataLoader("formulas_FlexibleDuctsRoundDataStorage.json"))
            },
            {
                typeof(DuctParametersDataStorage),
                () => new DuctParametersDataStorage(new JsonDataLoader("DuctParametersDataStorage.json"))
            },
            {
                typeof(SetMarginDataStorage),
                () => new SetMarginDataStorage(new JsonDataLoader("SetMarginDataStorage.json"))
            },
        };
        foreach (var creator in _creators)
        {
            _storages[creator.Key] = creator.Value();
        }
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
        if (!_storages.TryGetValue(type, out var storage))
        {
            throw new KeyNotFoundException($"Storage of type {type.Name} not found");
        }

        return storage;
    }

    // Метод для получения всех хранилищ
    public IEnumerable<IDataStorage> GetAllStorages()
    {
        return _storages.Values;
    }

    public T GetStorage<T>() where T : class, IDataStorage
    {
        var type = typeof(T);

        if (!_storages.TryGetValue(type, out var storage))
        {
            throw new ArgumentException($"Storage of type {type.Name} is not registered");
        }

        return (T)storage;
    }


    // Метод для обновления данных в хранилище
    public void UpdateStorage<T>() where T : class, IDataStorage
    {
        var type = typeof(T);

        // Проверяем, существует ли хранилище
        if (!_storages.TryGetValue(type, out var storage))
        {
            throw new KeyNotFoundException($"Storage of type {type.Name} not found");
        }

        // Обновляем данные в хранилище
        storage.UpdateData();
    }

    public void InitializeAllStorages()
    {
        var allTypes = _creators.Keys.ToArray();
        InitializeStorages(allTypes);
    }
}