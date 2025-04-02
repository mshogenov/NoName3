using Autodesk.Revit.DB.Plumbing;
using DesignationOfRisers.Services;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using Autodesk.Revit.DB;


namespace DesignationOfRisers.Models
{
    public class DataLoader
    {

        const string FilePath = @"C:\Users\mshog\AppData\Roaming\NoNameData\DesignationOfRisersCommandData.json";
        const string FolderPath = @"C:\Users\mshog\AppData\Roaming\NoNameData";
        public ObservableCollection<PipingSystemMdl> LoadDataWithSync(ObservableCollection<PipingSystemMdl> currentCollection, Document document)
        {


            if (!Directory.Exists(Path.GetDirectoryName(FilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            }

            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return currentCollection ?? CreateCurrentData(document);
                    }

                    List<PipingSystemMdlSerializable> serializedSystems = JsonConvert.DeserializeObject<List<PipingSystemMdlSerializable>>(json);
                    var savedSystems = serializedSystems.Select(ss => PipingSystemMdl.FromSerializable(ss, document)).ToList();

                    var currentData = CreateCurrentData(document);

                    // Создаем словарь по ID текущих систем для быстрого поиска
                    var currentDataMap = currentData.ToDictionary(item => item.PipingSystem.Id.ToString());

                    // Обновляем текущие данные или добавляем новые
                    foreach (var savedSystem in savedSystems)
                    {
                        if (currentDataMap.TryGetValue(savedSystem.PipingSystem.Id.ToString(), out var currentSystem))
                        {
                            // Обновляем текущую систему сохраненными значениями
                            currentSystem.IsChecked = savedSystem.IsChecked;
                            currentSystem.SelectedMark = savedSystem.SelectedMark;

                            // Обновляем Marks, убирая отсутствующие и добавляя новые
                            //var savedMarksIds = savedSystem.Marks.Select(mark => mark.Id.ToString()).ToHashSet();
                            //var currentMarksIds = currentSystem.Marks.Select(mark => mark.Id.ToString()).ToHashSet();

                            // Удаляем те Marks, которые отсутствуют в сохраненной модели
                            //for (int i = currentSystem.Marks.Count - 1; i >= 0; i--)
                            //{
                            //    if (!savedMarksIds.Contains(currentSystem.Marks[i].Id.ToString()))
                            //    {
                            //        currentSystem.Marks.RemoveAt(i);
                            //    }
                            //}

                            //// Добавляем новые Marks из сохраненной модели
                            //foreach (var savedMark in savedSystem.Marks)
                            //{
                            //    if (!currentMarksIds.Contains(savedMark.Id.ToString()))
                            //    {
                            //        currentSystem.Marks.Add(savedMark);
                            //    }
                            //}
                        }
                        else
                        {
                            // Добавляем систему, которой нет в текущих данных
                            currentData.Add(savedSystem);
                        }
                    }

                    // Удаляем системы, которых нет в сохраненных данных
                    var savedSystemsIds = savedSystems.Select(system => system.PipingSystem.Id.ToString()).ToHashSet();
                    for (int i = currentData.Count - 1; i >= 0; i--)
                    {
                        if (!savedSystemsIds.Contains(currentData[i].PipingSystem.Id.ToString()))
                        {
                            currentData.RemoveAt(i);
                        }
                    }

                    return currentData;
                }

                return currentCollection ?? CreateCurrentData(document);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
                return currentCollection ?? CreateCurrentData(document);
            }
        }
        private ObservableCollection<PipingSystemMdl> CreateCurrentData(Document document)
        {
            // Реализация получения текущих данных из документа
            var systems = new FilteredElementCollector(document)
                                .OfClass(typeof(PipingSystemType))
                                .Cast<PipingSystemType>()
                                .ToList();

            return new ObservableCollection<PipingSystemMdl>(
                systems.Select(system => new PipingSystemMdl(system)).ToList()
            );
        }

        public void SaveData(ObservableCollection<PipingSystemMdl> data)
        {
            List<PipingSystemMdlSerializable> serializedSystems = data
       .Select<PipingSystemMdl, PipingSystemMdlSerializable>(item => PipingSystemMdl.ToSerializable(item))
       .ToList();

            string json = JsonConvert.SerializeObject(serializedSystems, Formatting.Indented);

            if (!Directory.Exists(Path.GetDirectoryName(FilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            }

            File.WriteAllText(FilePath, json);
        }


    }
}
