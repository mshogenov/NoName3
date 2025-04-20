using System.Collections.ObjectModel;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using NumberingOfRisers.Models;
using NumberingOfRisers.Services;
using NumberingOfRisers.Storages;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI.Selection;
using Nice3point.Revit.Toolkit.External.Handlers;
using NoNameApi.Utils;
using NumberingOfRisers.Filters;
using Transaction = Autodesk.Revit.DB.Transaction;
using Visibility = System.Windows.Visibility;

namespace NumberingOfRisers.ViewModels;

public partial class NumberingOfRisersViewModel : ObservableObject
{
    private readonly Document _doc = Context.ActiveDocument;
    private readonly ActionEventHandler _actionEvent = new();
    private readonly UIDocument _uiDoc = Context.ActiveUiDocument;
    private readonly SettingsDataStorage _settingsDataStorage;
    private UserControl _settingsWindow;
    [ObservableProperty] private NumberingStrategy _selectedStrategy;

    public ObservableCollection<NumberingStrategy> NumberingStrategies { get; } = [];
    [ObservableProperty] private string _setNumberRiserValue;

    public UserControl SettingsWindow
    {
        get => _settingsWindow;
        set
        {
            _settingsWindow = value;
            OnPropertyChanged();
        }
    }

    private bool _manualFillingIsChecked;

    public bool ManualFillingIsChecked
    {
        get => _manualFillingIsChecked;
        set
        {
            if (_manualFillingIsChecked != value)
            {
                _manualFillingIsChecked = value;

                OnPropertyChanged();
                _settingsDataStorage.ManualFillingIsChecked = value;
            }
        }
    }

    private bool _automaticFillingIsChecked;

    public bool AutomaticFillingIsChecked
    {
        get => _automaticFillingIsChecked;
        set
        {
            if (_automaticFillingIsChecked != value)
            {
                _automaticFillingIsChecked = value;

                OnPropertyChanged();
                _settingsDataStorage.AutomaticFillingIsChecked = value;
            }
        }
    }

    private double _minimumPipeLength = 2000;

    public double MinimumPipeLength
    {
        get => _minimumPipeLength;
        set
        {
            _minimumPipeLength = value;
            OnPropertyChanged();
            _settingsDataStorage.MinimumPipeLengthRiserPipe = value;
        }
    }

    private int _minimumCountPipes;

    public int MinimumCountPipes
    {
        get => _minimumCountPipes;
        set
        {
            _minimumCountPipes = value;
            OnPropertyChanged();
            _settingsDataStorage.MinimumNumberPipesRiserPipe = value;
        }
    }

    private readonly NumberingOfRisersServices _numberingOfRisersServices;
    [ObservableProperty] private ObservableCollection<RiserSystemType> _riserSystemTypes = [];

    private readonly DataLoader _dataLoader = new(fileName: "DataNumberingOfRisers");
    [ObservableProperty] private bool _isVisibilityMissingParameterParamRiserId;
    [ObservableProperty] private bool _isVisibilityMissingParameters;
    private readonly RiserDataStorage _riserDataStorage;

    public NumberingOfRisersViewModel()
    {
        _numberingOfRisersServices = new NumberingOfRisersServices();
        _riserDataStorage = new RiserDataStorage(_doc);

        // Загружаем данные из ExtensibleStorage
        InitializeFromStorage();

        // Инициализация списка стратегий нумерации
        InitializeStrategies();

        // Установка стратегии по умолчанию
        SelectedStrategy = NumberingStrategies[0];
    }

    private void InitializeStrategies()
    {
        NumberingStrategies.Add(new NumberingStrategy("Снизу вверх, слева направо",
            NumberingDirection.LeftToRight, NumberingDirection.BottomToTop, SortDirection.Y));
        NumberingStrategies.Add(new NumberingStrategy("Снизу вверх, справа налево",
            NumberingDirection.RightToLeft, NumberingDirection.BottomToTop, SortDirection.Y));
        NumberingStrategies.Add(new NumberingStrategy("Сверху вниз, слева направо",
            NumberingDirection.LeftToRight, NumberingDirection.TopToBottom, SortDirection.Y));
        NumberingStrategies.Add(new NumberingStrategy("Сверху вниз, справа налево",
            NumberingDirection.RightToLeft, NumberingDirection.TopToBottom, SortDirection.Y));
        NumberingStrategies.Add(new NumberingStrategy("Слева направо, снизу вверх",
            NumberingDirection.LeftToRight, NumberingDirection.BottomToTop, SortDirection.X));
        NumberingStrategies.Add(new NumberingStrategy("Слева направо, сверху вниз",
            NumberingDirection.LeftToRight, NumberingDirection.TopToBottom, SortDirection.X));
        NumberingStrategies.Add(new NumberingStrategy("Справа налево, снизу вверх",
            NumberingDirection.RightToLeft, NumberingDirection.BottomToTop, SortDirection.X));
        NumberingStrategies.Add(new NumberingStrategy("Справа налево, сверху вниз",
            NumberingDirection.RightToLeft, NumberingDirection.TopToBottom, SortDirection.X));
    }


    /// <summary>
    /// Сохраняет данные стояков в ExtensibleStorage
    /// </summary>
    [RelayCommand]
    private void SaveRisersData()
    {
        _actionEvent.Raise(_ =>
        {
            try
            {
                RiserStorageManager.SaveRisers(_doc, _riserDataStorage);
                MessageBox.Show("Данные стояков успешно сохранены в файле Revit.",
                    "Сохранение данных", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _actionEvent.Cancel();
            }
        });
    }

    /// <summary>
    /// Загружает данные при инициализации
    /// </summary>
    private void InitializeFromStorage()
    {
        try
        {
            List<RiserData> loadRisersDataShema = RiserStorageManager.LoadRisers(_doc);
            // Список для хранения стояков, которые нужно удалить
            var risersToRemove = new List<Riser>();
            // Список для хранения новых стояков, которые нужно добавить
            var risersToAdd = new List<Riser>();
            // Сначала проверяем существующие стояки на совпадение с игнорируемыми
            foreach (Riser riser in _riserDataStorage.Risers)
            {
                bool shouldBeRemoved = false;
                foreach (RiserData loadRiserData in loadRisersDataShema)
                {
                    // Если найден идентичный игнорируемый стояк, отмечаем его на удаление
                    if (IsIdenticalRisers(riser, loadRiserData) && loadRiserData.Ignored)
                    {
                        shouldBeRemoved = true;
                        break;
                    }
                }
                if (shouldBeRemoved)
                {
                    risersToRemove.Add(riser);
                }
            }
            // Удаляем отмеченные стояки из хранилища
            foreach (var riser in risersToRemove)
            {
                _riserDataStorage.Risers.Remove(riser);
            }
            // Теперь добавляем новые неигнорируемые стояки
            foreach (RiserData loadRiserData in loadRisersDataShema)
            {
                // Пропускаем игнорируемые стояки
                if (loadRiserData.Ignored)
                    continue;

                // Проверяем, есть ли уже такой стояк в хранилище
                bool riserExists = _riserDataStorage.Risers.Any(r => IsIdenticalRisers(r, loadRiserData));

                // Если стояка нет, создаем новый и добавляем
                if (!riserExists)
                {
                    // Восстанавливаем ElementIds и получаем трубы
                    var pipes = new List<Pipe>();
                    foreach (long idValue in loadRiserData.ElementIds)
                    {
                        ElementId elementId = new ElementId(idValue);
                        // Получаем трубу по ID
                        Element element = _doc.GetElement(elementId);
                        if (element is Pipe pipe)
                        {
                            pipes.Add(pipe);
                        }
                    }

                    if (pipes.Count > 0)
                    {
                        risersToAdd.Add(new Riser(pipes));
                    }
                }
            }
            // Добавляем новые стояки в хранилище
            foreach (var riser in risersToAdd)
            {
                _riserDataStorage.Risers.Add(riser);
            }
            
            RiserSystemTypes = new ObservableCollection<RiserSystemType>(_riserDataStorage.Risers
                .GroupBy(r => r.MepSystemType?.Name ?? "Без системы")
                .Select(group => new RiserSystemType(group.ToList()))
                .ToList());
            OnPropertyChanged(nameof(RiserSystemTypes));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при инициализации данных: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Проверяет, идентичны ли два стояка на основе процента совпадающих ElementIds.
    /// </summary>
    /// <param name="riser1">Первый стояк для сравнения</param>
    /// <param name="riser2">Второй стояк для сравнения</param>
    /// <param name="minMatchPercentage">Минимальный процент совпадающих ElementIds для признания идентичности (по умолчанию 50%)</param>
    /// <returns>True, если стояки считаются идентичными</returns>
    public bool IsIdenticalRisers(Riser riser1, Riser riser2, double minMatchPercentage = 50.0)
    {
        if (riser1 == null || riser2 == null ||
            riser1.ElementIds.Count == 0 || riser2.ElementIds.Count == 0)
            return false;

        // Подсчитываем количество совпадающих ElementIds
        int matchCount = riser1.ElementIds.Count(elementId =>
            riser2.ElementIds.Any(otherElementId =>
                otherElementId.Value == elementId.Value));

        // Находим меньшее из количеств ElementIds двух стояков
        int minElementCount = Math.Min(riser1.ElementIds.Count, riser2.ElementIds.Count);

        // Вычисляем процент совпадений относительно меньшего количества ElementIds
        double matchPercentage = (matchCount * 100.0) / minElementCount;

        return matchPercentage >= minMatchPercentage;
    }

    public bool IsIdenticalRisers(Riser riser1, RiserData riserData, double minMatchPercentage = 50.0)
    {
        if (riser1 == null || riserData == null ||
            riser1.ElementIds.Count == 0 || riserData.ElementIds.Count == 0)
            return false;

        // Подсчитываем количество совпадающих ElementIds
        int matchCount = riser1.ElementIds.Count(elementId =>
            riserData.ElementIds.Any(otherElementId =>
                otherElementId == elementId.Value));

        // Находим меньшее из количеств ElementIds двух стояков
        int minElementCount = Math.Min(riser1.ElementIds.Count, riserData.ElementIds.Count);

        // Вычисляем процент совпадений относительно меньшего количества ElementIds
        double matchPercentage = (matchCount * 100.0) / minElementCount;

        return matchPercentage >= minMatchPercentage;
    }

    /// <summary>
    /// Получает процент совпадающих ElementIds между двумя стояками.
    /// </summary>
    /// <param name="riser1">Первый стояк для сравнения</param>
    /// <param name="riser2">Второй стояк для сравнения</param>
    /// <returns>Процент совпадающих ElementIds относительно меньшего количества ElementIds</returns>
    public double GetMatchPercentage(Riser riser1, Riser riser2)
    {
        if (riser1 == null || riser2 == null ||
            riser1.ElementIds.Count == 0 || riser2.ElementIds.Count == 0)
            return 0;

        int matchCount = riser1.ElementIds.Count(elementId =>
            riser2.ElementIds.Any(otherElementId =>
                otherElementId.Value == elementId.Value));

        int minElementCount = Math.Min(riser1.ElementIds.Count, riser2.ElementIds.Count);

        return (matchCount * 100.0) / minElementCount;
    }

    [RelayCommand]
    private void NumberingOfRisers(Window window)
    {
        _actionEvent.Raise(_ =>
        {
            try
            {
                using (Transaction tr = new(Context.ActiveDocument, "Нумерация стояков"))
                {
                    tr.Start();

                    // Проверка, что есть выбранная стратегия
                    if (SelectedStrategy == null)
                        return;

                    // Используем выбранную стратегию для нумерации
                    var numberingService = new RiserNumberingService(
                        SelectedStrategy.PrimarySortDirection,
                        SelectedStrategy.XDirection,
                        SelectedStrategy.YDirection
                    );
                    foreach (var riserSystemType in RiserSystemTypes)
                    {
                        var numberedRisers = numberingService.NumberRisers(riserSystemType.Risers.ToList());
                    }


                    // Обновляем данные в UI или применяем номера...


                    tr.Commit();
                }

                TaskDialog.Show("Успешно", "Номера стояков записаны");
            }

            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Неожиданная ошибка: {ex.Message}");
            }
            finally
            {
                _actionEvent.Cancel();
            }
        });
    }

    [RelayCommand]
    private void AddRiser(object parameter)
    {
        if (parameter is Window window)
        {
            window.Visibility = Visibility.Hidden;
            try
            {
                // Выбор трубы пользователем
                var selectionFilter = new VerticalPipeSelectionFilter();
                var pickedRef =
                    _uiDoc.Selection.PickObject(ObjectType.Element, selectionFilter, "Выберите вертикальную трубу");

                // Получаем выбранную трубу
                if (_doc.GetElement(pickedRef.ElementId) is not Pipe selectedPipe)
                {
                    window.Visibility = Visibility.Visible;
                    return;
                }

                // Получаем координаты выбранной трубы
                XYZ selectedPipeLocation = GetPipeLocationXY(selectedPipe);

                // Получаем все вертикальные трубы
                List<Pipe> verticalPipes = _numberingOfRisersServices.GetVerticalPipes(_doc).ToList();

                // Определяем допустимое расстояние для группировки труб
                const double tolerance = 0.1; // в футах, настройте в соответствии с вашими требованиями

                // Находим трубы, расположенные рядом по X и Y
                List<Pipe> nearbyPipes = verticalPipes
                    .Where(pipe => IsNearbyInXY(pipe, selectedPipeLocation, tolerance))
                    .ToList();

                // Добавляем выбранную трубу, если ее нет в списке
                if (nearbyPipes.All(p => p.Id.Value != selectedPipe.Id.Value))
                {
                    nearbyPipes.Add(selectedPipe);
                }

                // Создаем новый стояк из найденных труб
                if (nearbyPipes.Any())
                {
                    // Проверяем, не входят ли найденные трубы уже в существующие стояки
                    bool pipeAlreadyInRiser = false;
                    foreach (var systemType in RiserSystemTypes)
                    {
                        if (systemType.Risers.Any(r =>
                                nearbyPipes.Any(p => r.ElementIds.Contains(p.Id))))
                        {
                            pipeAlreadyInRiser = true;
                            break;
                        }
                    }

                    if (pipeAlreadyInRiser)
                    {
                        window.Visibility = Visibility.Visible;
                        return;
                    }

                    if (nearbyPipes.Count > 0)
                    {
                        var newRiser = new Riser(nearbyPipes);
                        // Добавляем стояк в соответствующий RiserSystemType
                        string systemTypeName = newRiser.MepSystemType?.Name ?? "Без системы";
                        RiserSystemType targetSystemType =
                            RiserSystemTypes.FirstOrDefault(st => st.MepSystemTypeName == systemTypeName);

                        if (targetSystemType == null)
                        {
                            // Создаем новый тип системы, если такого еще нет
                            targetSystemType = new RiserSystemType(new List<Riser> { newRiser });
                            RiserSystemTypes.Add(targetSystemType);
                        }

                        bool riserAdded = false;

                        foreach (Riser riser in _riserDataStorage.Risers)
                        {
                            long matchCount = riser.ElementIds.Count(elementId =>
                                newRiser.ElementIds.Any(newElementId => newElementId.Value == elementId.Value));

                            if (matchCount > 0)
                            {
                                targetSystemType.Risers.Add(riser);
                                riserAdded = true;
                                break;
                            }
                        }

                        if (!riserAdded)
                        {
                            targetSystemType.Risers.Add(newRiser);
                            _riserDataStorage.Risers.Add(newRiser);
                        }
                    }
                }
                else
                {
                    TaskDialog.Show("Ошибка", "Не найдены трубы, расположенные рядом с выбранной.");
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // Пользователь отменил выбор
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Произошла ошибка при добавлении стояка: {ex.Message}");
            }

            window.Visibility = Visibility.Visible;
        }
    }

    // Проверка, находится ли труба рядом с указанной точкой в плоскости XY
    private bool IsNearbyInXY(Pipe pipe, XYZ referencePoint, double tolerance)
    {
        XYZ pipeLocation = GetPipeLocationXY(pipe);

        // Вычисляем расстояние только по X и Y (игнорируем Z)
        double distance = Math.Sqrt(
            Math.Pow(pipeLocation.X - referencePoint.X, 2) +
            Math.Pow(pipeLocation.Y - referencePoint.Y, 2));

        return distance <= tolerance;
    }

// Получение XY-координат трубы (игнорируя Z)
    private XYZ GetPipeLocationXY(Pipe pipe)
    {
        if (pipe.Location is not LocationCurve locationCurve) return XYZ.Zero;
        // Берем среднюю точку трубы
        Curve curve = locationCurve.Curve;
        XYZ midPoint = curve.Evaluate(0.5, true);
        // Возвращаем только X и Y координаты, игнорируя Z
        return new XYZ(midPoint.X, midPoint.Y, 0);
    }

    [RelayCommand]
    private void HighlightAll()
    {
    }

    [RelayCommand]
    private void DeleteRiser(object obj)
    {
        if (obj is not Riser riser) return;

        // Создаем копию списка, чтобы избежать проблем с модификацией коллекции во время итерации
        var riserSystemTypesToRemove = new List<RiserSystemType>();

        foreach (var riserSystemType in RiserSystemTypes.ToList())
        {
            var deleteRiser = _riserDataStorage.Risers.FirstOrDefault(x => x.Id == riser.Id);
            if (deleteRiser == null) continue;
            deleteRiser.Ignored = true;
            // Удаляем стояк из коллекции
            riserSystemType.Risers.Remove(deleteRiser);
            // Если система стала пустой, отмечаем ее для удаления
            if (riserSystemType.Risers.Count == 0)
            {
                riserSystemTypesToRemove.Add(riserSystemType);
            }

            break;
        }

        // Удаляем пустые системы
        foreach (var systemToRemove in riserSystemTypesToRemove)
        {
            RiserSystemTypes.Remove(systemToRemove);
        }
    }

    [RelayCommand]
    private void HighlightRiser(object obj)
    {
        if (obj is not Riser riser) return;
        _uiDoc.Selection.SetElementIds(riser.ElementIds);
        _uiDoc.ShowElements(riser.ElementIds);
    }

    [RelayCommand]
    private void NumberSelectedRiser(object obj)
    {
        if (obj is not Riser riser) return;
        if (riser.NewNumberRiser == string.Empty) return;
        _actionEvent.Raise(_ =>
        {
            using Transaction tr = new Transaction(_doc, "Нумеровать выбранный стояк");
            try
            {
                tr.Start();
                foreach (var pipe in riser.Pipes)
                {
                    Parameter parameter = pipe.FindParameter("ADSK_Номер стояка");
                    if (parameter != null && parameter.AsValueString() != riser.NewNumberRiser)
                    {
                        parameter.Set(riser.NewNumberRiser);
                    }
                }

                tr.Commit();
                riser.GetNumberRiser();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Ошибка", e.Message);
            }
            finally
            {
                _actionEvent.Cancel();
            }
        });
    }

    /// <summary>
    /// Обновляет стояки с учетом новых труб, добавленных в Revit
    /// </summary>
    [RelayCommand]
    private void RefreshRisers()
    {
        try
        {
            // Получаем все вертикальные трубы в проекте
            List<Pipe> allVerticalPipes = _numberingOfRisersServices.GetVerticalPipes(_doc).ToList();
            // Словарь для хранения труб без идентификатора стояка
            Dictionary<XYZ, List<Pipe>> unassignedPipesByLocation = new Dictionary<XYZ, List<Pipe>>();

            // Создаем карту существующих стояков и находим трубы без стояков
            foreach (var pipe in allVerticalPipes)
            {
                // Получаем параметр идентификатора стояка
                Parameter riserIdParam = pipe.FindParameter("ADSK_Идентификатор стояка");
                string riserId;

                if (riserIdParam != null && !string.IsNullOrEmpty(riserIdParam.AsString()))
                {
                    riserId = riserIdParam.AsString();

                    // Ищем соответствующий стояк
                    Riser assignedRiser = null;
                    foreach (var systemType in RiserSystemTypes)
                    {
                        assignedRiser = systemType.Risers.FirstOrDefault(r => r.Id.ToString() == riserId);
                        if (assignedRiser != null) break;
                    }

                    // Если нашли стояк, убеждаемся, что труба добавлена
                    if (assignedRiser != null)
                    {
                        // Проверяем, есть ли уже эта труба в стояке
                        if (assignedRiser.ElementIds.Any(id => id.Value == pipe.Id.Value)) continue;
                        // Добавляем трубу в стояк
                        assignedRiser.Pipes.Add(pipe);
                        assignedRiser.ElementIds.Add(pipe.Id);
                    }
                    else
                    {
                        // Стояк с этим ID не найден (возможно, удален)
                        // Сбрасываем параметр ID стояка
                        using (Transaction tx = new Transaction(_doc, "Сброс идентификатора стояка"))
                        {
                            tx.Start();
                            riserIdParam.Set(string.Empty);
                            tx.Commit();
                        }

                        // Обрабатываем как неназначенную трубу
                        XYZ location = GetPipeLocationXY(pipe);
                        if (!unassignedPipesByLocation.ContainsKey(location))
                        {
                            unassignedPipesByLocation[location] = new List<Pipe>();
                        }

                        unassignedPipesByLocation[location].Add(pipe);
                    }
                }
                else
                {
                    // Это труба без идентификатора стояка
                    XYZ location = GetPipeLocationXY(pipe);
                    if (!unassignedPipesByLocation.ContainsKey(location))
                    {
                        unassignedPipesByLocation[location] = new List<Pipe>();
                    }

                    unassignedPipesByLocation[location].Add(pipe);
                }
            }

            // Обработка не назначенных труб
            foreach (var locationGroup in unassignedPipesByLocation)
            {
                XYZ location = locationGroup.Key;
                List<Pipe> unassignedPipes = locationGroup.Value;

                // Находим ближайший стояк
                Riser nearestRiser = FindNearestRiser(location);

                if (nearestRiser != null &&
                    IsLocationCloseToRiser(location, nearestRiser, 0.1)) // 0.5 фута - настраиваемая величина
                {
                    // Добавляем не назначенные трубы в ближайший стояк
                    using Transaction tx = new Transaction(_doc, "Добавление труб в стояк");
                    tx.Start();
                    foreach (var pipe in unassignedPipes)
                    {
                        // Проверяем, не добавлена ли уже эта труба
                        if (nearestRiser.ElementIds.Any(id => id.Value == pipe.Id.Value)) continue;
                        // Добавляем трубу в стояк
                        nearestRiser.Pipes.Add(pipe);
                        nearestRiser.ElementIds.Add(pipe.Id);

                        // Обновляем параметры трубы
                        Parameter riserIdParam = pipe.FindParameter("ADSK_Идентификатор стояка");
                        riserIdParam?.Set(nearestRiser.Id.ToString());

                        Parameter riserNumberParam = pipe.FindParameter("ADSK_Номер стояка");
                        riserNumberParam?.Set(nearestRiser.Number.ToString());
                    }

                    tx.Commit();
                }
                else if (unassignedPipes.Count >= 1) // Можно создать новый стояк, если труб много
                {
                    // Спрашиваем пользователя, хочет ли он создать новый стояк
                    var result = MessageBox.Show(
                        $"Найдены {unassignedPipes.Count} вертикальных труб без стояка. Создать новый стояк?",
                        "Создание стояка", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Создаем новый стояк из неназначенных труб
                        var pipesGroup = unassignedPipes.GroupBy(p => p, new PipeIEqualityComparer()).FirstOrDefault();
                        if (pipesGroup == null) continue;
                        Riser newRiser = new Riser(pipesGroup);

                        // Присваиваем номер стояка
                        int maxNumber = 0;
                        foreach (var systemType in RiserSystemTypes)
                        {
                            if (!systemType.Risers.Any()) continue;
                            int sysMaxNumber = systemType.Risers.Max(r => r.Number);
                            maxNumber = Math.Max(maxNumber, sysMaxNumber);
                        }

                        newRiser.Number = maxNumber + 1;

                        // Записываем параметры
                        using (Transaction tx = new Transaction(_doc, "Создание нового стояка"))
                        {
                            tx.Start();
                            foreach (var pipe in newRiser.Pipes)
                            {
                                Parameter riserIdParam = pipe.FindParameter("ADSK_Идентификатор стояка");
                                riserIdParam?.Set(newRiser.Id.ToString());

                                Parameter riserNumberParam = pipe.FindParameter("ADSK_Номер стояка");
                                riserNumberParam?.Set(newRiser.Number.ToString());
                            }

                            tx.Commit();
                        }

                        // Добавляем стояк в соответствующую систему
                        string systemTypeName = newRiser.MepSystemType?.Name ?? "Без системы";
                        RiserSystemType targetSystemType =
                            RiserSystemTypes.FirstOrDefault(st => st.MepSystemTypeName == systemTypeName);

                        if (targetSystemType == null)
                        {
                            targetSystemType = new RiserSystemType([newRiser]);
                            RiserSystemTypes.Add(targetSystemType);
                        }
                        else
                        {
                            targetSystemType.Risers.Add(newRiser);
                        }
                    }
                }
            }

            // Обновляем UI
            OnPropertyChanged(nameof(RiserSystemTypes));

            // Уведомление пользователя
            MessageBox.Show("Стояки успешно обновлены", "Обновление стояков", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при обновлении стояков: {ex.Message}", "Ошибка", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


// Метод для нахождения ближайшего стояка к заданной точке
    private Riser FindNearestRiser(XYZ location)
    {
        Riser nearest = null;
        double minDistance = double.MaxValue;

        foreach (var systemType in RiserSystemTypes)
        {
            foreach (var riser in systemType.Risers)
            {
                // Используем первую трубу стояка для определения его расположения
                if (riser.Pipes.Any())
                {
                    XYZ riserLocation = GetPipeLocationXY(riser.Pipes.First());
                    double distance = riserLocation.DistanceTo(location);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = riser;
                    }
                }
            }
        }

        return nearest;
    }

// Метод для проверки, находится ли точка близко к стояку
    private bool IsLocationCloseToRiser(XYZ location, Riser riser, double toleranceFeet)
    {
        if (!riser.Pipes.Any()) return false;

        // Проверяем все трубы стояка
        foreach (var pipe in riser.Pipes)
        {
            XYZ pipeLocation = GetPipeLocationXY(pipe);
            if (pipeLocation.DistanceTo(location) <= toleranceFeet)
            {
                return true;
            }
        }

        return false;
    }
}