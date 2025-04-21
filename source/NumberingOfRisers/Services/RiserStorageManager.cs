using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Plumbing;
using Newtonsoft.Json;
using NumberingOfRisers.Models;
using NumberingOfRisers.Storages;

namespace NumberingOfRisers.Services;

public class RiserStorageManager
{
    private static readonly Guid SchemaGuid = new("A1B2C3D4-E5F6-7890-1234-567890ABCDEF");
    private static readonly string SchemaName = "RisersStorageSchema";
    private static readonly string SchemaDescription = "Хранилище данных стояков";
    private static readonly string RiserFieldName = "RisersData";

    /// <summary>
    /// Создает или получает схему хранения стояков
    /// </summary>
    private static Schema GetSchema()
    {
        Schema schema = Schema.Lookup(SchemaGuid);

        if (schema != null) return schema;
        // Создаем новую схему для сохранения данных стояков
        SchemaBuilder schemaBuilder = new SchemaBuilder(SchemaGuid);
        schemaBuilder.SetSchemaName(SchemaName);
        schemaBuilder.SetDocumentation(SchemaDescription);

        // Добавляем строковое поле для хранения данных в JSON формате
        FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField(RiserFieldName, typeof(string));
        fieldBuilder.SetDocumentation("Данные о стояках в JSON формате");

        // Создаем схему
        schema = schemaBuilder.Finish();

        return schema;
    }

    /// <summary>
    /// Сохраняет стояки в ExtensibleStorage проекта
    /// </summary>
    public static void SaveRisers(Document doc, RiserDataStorage riserDataStorage)
    {
        using Transaction tx = new Transaction(doc, "Сохранение данных стояков");
        tx.Start();
        try
        {
            // Получаем схему хранения
            Schema schema = GetSchema();

            // Получаем ProjectInfo
            ProjectInfo projectInfo = doc.ProjectInformation;
            if (projectInfo == null)
            {
                throw new Exception("ProjectInfo не найден");
            }

            // Получаем существующие данные
            List<RiserData> existingRiserData = new List<RiserData>();
            Entity existingEntity = projectInfo.GetEntity(schema);
            if (existingEntity != null && existingEntity.IsValid())
            {
                string existingJsonData = existingEntity.Get<string>(RiserFieldName);
                if (!string.IsNullOrEmpty(existingJsonData))
                {
                    existingRiserData = JsonConvert.DeserializeObject<List<RiserData>>(existingJsonData);
                }
            }

            // Создаем список новых данных стояков для сериализации
            var newRiserData = new List<RiserData>();
            foreach (var riser in riserDataStorage.Risers)
            {
                var riserData = new RiserData()
                {
                    Number = riser.Number,
                    ElementIds = riser.ElementIds.Select(id => id.Value).ToList(),
                    Ignored = riser.Ignored
                };
                newRiserData.Add(riserData);
            }

            // Объединяем существующие и новые данные
            var mergedRiserData = MergeRiserData(existingRiserData, newRiserData);

            // Сериализуем объединенные данные в JSON
            string jsonData = JsonConvert.SerializeObject(mergedRiserData, Formatting.None);

            // Создаем Entity для хранения данных
            Entity entity = new Entity(schema);
            entity.Set(RiserFieldName, jsonData);

            // Сохраняем данные в ProjectInfo
            projectInfo.SetEntity(entity);

            tx.Commit();
        }
        catch (Exception ex)
        {
            tx.RollBack();
            throw new Exception($"Ошибка при сохранении данных стояков: {ex.Message}", ex);
        }
    }

    public void SaveSettings()
    {
    }

    public static void ClearRiserData(Document doc)
    {
        using Transaction tx = new Transaction(doc, "Удаление данных стояков");
        tx.Start();
        try
        {
            // Получаем схему хранения
            Schema schema = GetSchema();

            // Получаем ProjectInfo
            ProjectInfo projectInfo = doc.ProjectInformation;
            if (projectInfo == null)
            {
                throw new Exception("ProjectInfo не найден");
            }

            // Проверяем, существуют ли данные для удаления
            Entity existingEntity = projectInfo.GetEntity(schema);
            if (existingEntity != null && existingEntity.IsValid())
            {
                // Создаем новую пустую сущность
                Entity emptyEntity = new Entity(schema);

                // Устанавливаем пустое значение для поля
                emptyEntity.Set(RiserFieldName, string.Empty);

                // Заменяем существующую сущность пустой
                projectInfo.SetEntity(emptyEntity);
            }

            tx.Commit();
        }
        catch (Exception ex)
        {
            tx.RollBack();
            throw new Exception($"Ошибка при удалении данных стояков: {ex.Message}", ex);
        }
    }

    private static List<RiserData> MergeRiserData(List<RiserData> existingData, List<RiserData> newData)
    {
        var mergedData = new List<RiserData>(existingData);

        foreach (var newRiser in newData)
        {
            var existingRiser = mergedData.FirstOrDefault(r => IsIdenticalRiser(r, newRiser));
            if (existingRiser != null)
            {
                // Обновляем существующий стояк
                existingRiser.Number = newRiser.Number;
                existingRiser.ElementIds = newRiser.ElementIds;
                existingRiser.Ignored = newRiser.Ignored;
            }
            else
            {
                // Добавляем новый стояк
                mergedData.Add(newRiser);
            }
        }

        return mergedData;
    }

    private static bool IsIdenticalRiser(RiserData riser1, RiserData riser2, double minMatchPercentage = 50.0)
    {
        if (riser1 == null || riser2 == null ||
            riser1.ElementIds.Count == 0 || riser2.ElementIds.Count == 0)
            return false;

        int matchCount = riser1.ElementIds.Count(id => riser2.ElementIds.Contains(id));
        int minElementCount = Math.Min(riser1.ElementIds.Count, riser2.ElementIds.Count);
        double matchPercentage = (matchCount * 100.0) / minElementCount;

        return matchPercentage >= minMatchPercentage;
    }

    /// <summary>
    /// Загружает стояки из ExtensibleStorage проекта
    /// </summary>
    public static List<RiserData> LoadRisers(Document doc)
    {
        List<RiserData> riserDates;
        // Получаем схему хранения
        Schema schema = GetSchema();
        if (schema == null)
        {
            return new List<RiserData>();
        }

        // Получаем ProjectInfo
        ProjectInfo projectInfo = doc.ProjectInformation;
        if (projectInfo == null)
        {
            return new List<RiserData>();
        }

        // Получаем данные Entity
        Entity entity = projectInfo.GetEntity(schema);
        if (!entity.IsValid())
        {
            return new List<RiserData>();
        }

        try
        {
            // Получаем JSON данные
            string jsonData = entity.Get<string>(RiserFieldName);
            if (string.IsNullOrEmpty(jsonData))
            {
                return new List<RiserData>();
            }

            // Десериализуем данные
            riserDates = JsonConvert.DeserializeObject<List<RiserData>>(jsonData);
            if (riserDates == null)
            {
                return new List<RiserData>();
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при загрузке данных стояков: {ex.Message}", ex);
        }

        return riserDates;
    }

}