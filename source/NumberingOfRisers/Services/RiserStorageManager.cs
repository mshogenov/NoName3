using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Plumbing;
using Newtonsoft.Json;
using NumberingOfRisers.Models;

namespace NumberingOfRisers.Services;

public class RiserStorageManager 
{
    private static readonly Guid SchemaGuid = new Guid("A1B2C3D4-E5F6-7890-1234-567890ABCDEF");
    private static readonly string SchemaName = "RisersStorageSchema";
    private static readonly string SchemaDescription = "Хранилище данных стояков";
    private static readonly string FieldName = "RisersData";
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
        FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField(FieldName, typeof(string));
        fieldBuilder.SetDocumentation("Данные о стояках в JSON формате");

        // Создаем схему
        schema = schemaBuilder.Finish();

        return schema;
    }
    /// <summary>
    /// Сохраняет стояки в ExtensibleStorage проекта
    /// </summary>
    public static void SaveRisers(Document doc, List<RiserSystemType> riserSystemTypes)
    {
        using (Transaction tx = new Transaction(doc, "Сохранение данных стояков"))
        {
            tx.Start();

            try
            {
                // Получаем схему хранения
                Schema schema = GetSchema();

                // Создаем список данных стояков для сериализации
                var riserDataList = new List<RiserSystemTypeData>();

                foreach (var systemType in riserSystemTypes)
                {
                    var riserSystemData = new RiserSystemTypeData
                    {
                        MepSystemTypeName = systemType.MepSystemTypeName,
                        Risers = systemType.Risers.Select(r => new RiserData
                        {
                            Id = r.Id,
                            Number = r.Number,
                            ElementIds = r.ElementIds.Select(id => id.Value).ToList()
                        }).ToList()
                    };

                    riserDataList.Add(riserSystemData);
                }

                // Сериализуем данные в JSON
                string jsonData = JsonConvert.SerializeObject(riserDataList, Formatting.None);

                // Создаем Entity для хранения данных
                Entity entity = new Entity(schema);
                entity.Set(FieldName, jsonData);

                // Сохраняем данные в ProjectInfo (можно использовать другой элемент)
                ProjectInfo projectInfo = doc.ProjectInformation;
                if (projectInfo != null)
                {
                    projectInfo.SetEntity(entity);
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.RollBack();
                throw new Exception($"Ошибка при сохранении данных стояков: {ex.Message}", ex);
            }
        }
    }
     /// <summary>
    /// Загружает стояки из ExtensibleStorage проекта
    /// </summary>
    public static List<RiserSystemType> LoadRisers(Document doc)
    {
        // Получаем схему хранения
        Schema schema = GetSchema();
        if (schema == null)
        {
            return new List<RiserSystemType>();
        }

        // Получаем ProjectInfo
        ProjectInfo projectInfo = doc.ProjectInformation;
        if (projectInfo == null)
        {
            return new List<RiserSystemType>();
        }

        // Получаем данные Entity
        Entity entity = projectInfo.GetEntity(schema);
        if (!entity.IsValid())
        {
            return new List<RiserSystemType>();
        }

        try
        {
            // Получаем JSON данные
            string jsonData = entity.Get<string>(FieldName);
            if (string.IsNullOrEmpty(jsonData))
            {
                return new List<RiserSystemType>();
            }

            // Десериализуем данные
            var riserDataList = JsonConvert.DeserializeObject<List<RiserSystemTypeData>>(jsonData);
            if (riserDataList == null)
            {
                return new List<RiserSystemType>();
            }

            // Воссоздаем объекты стояков и систем
            var riserSystemTypes = new List<RiserSystemType>();

            foreach (var systemData in riserDataList)
            {
                var risers = new List<Riser>();

                foreach (var riserData in systemData.Risers)
                {
                    // Восстанавливаем ElementIds и получаем трубы
                    var elementIds = new List<ElementId>();
                    var pipes = new List<Pipe>();

                    foreach (long idValue in riserData.ElementIds)
                    {
                        ElementId elementId = new ElementId(idValue);
                        elementIds.Add(elementId);

                        // Получаем трубу по ID
                        Element element = doc.GetElement(elementId);
                        if (element is Pipe pipe)
                        {
                            pipes.Add(pipe);
                        }
                    }

                    // Создаем стояк, используя конструктор с коллекцией труб
                    var riser = new Riser(pipes);

                    // Устанавливаем сохраненные значения
                    riser.Id = riserData.Id;
                    riser.Number = riserData.Number;
                    riser.ElementIds = elementIds;

                    risers.Add(riser);
                }

                var systemType = new RiserSystemType(risers);
                systemType.MepSystemTypeName = systemData.MepSystemTypeName;

                riserSystemTypes.Add(systemType);
            }


            return riserSystemTypes;
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при загрузке данных стояков: {ex.Message}", ex);
        }
    }
}

