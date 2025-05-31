using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using NoNameApi.Views;
using SystemModelingCommands.Filters;
using SystemModelingCommands.Models;
using SystemModelingCommands.Views;

namespace SystemModelingCommands.Services
{
    public class SystemModelingServices
    {
        private readonly Document _doc = Context.ActiveDocument;
        private readonly UIDocument _uiDoc = Context.ActiveUiDocument;

        public void InsertPipe()
        {
            // Создаем фильтр для проверки
            FittingAndAccessorySelectionFilter filter = new FittingAndAccessorySelectionFilter();
            ElementWrapper selectedElement = null;
            // Проверка, есть ли выбранный элемент до запуска скрипта
            var selectedIds = _uiDoc.Selection.GetElementIds();
            try
            {
                if (selectedIds is { Count: 1 })
                {
                    // Получить первый выбранный элемент
                    if (_doc != null)
                    {
                        Element preSelectedElement = _doc.GetElement(selectedIds.First());
                        // Проверка, проходит ли выбранный элемент фильтр
                        if (filter.AllowElement(preSelectedElement))
                        {
                            selectedElement = new ElementWrapper(preSelectedElement);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", ex.Message);
            }

            // Если элемент не был предварительно выбран или не соответствует фильтру, запустить выбор элемента пользователем
            if (selectedElement == null)
            {
                try
                {
                    Reference reference = _uiDoc?.Selection.PickObject(ObjectType.Element, filter);
                    selectedElement = new ElementWrapper(_doc?.GetElement(reference));
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return; // Пользователь отменил выбор
                }
            }

            MEPCurveType mepCurveType = selectedElement.DeterminingTypeOfPipeByFitting();
            if (mepCurveType == null)
            {
                BloomView view = new BloomView(_doc, selectedElement.Element);
                view.ShowDialog();
                if (view.MepCurveType != null)
                {
                    mepCurveType = view.MepCurveType;
                }
                else
                {
                    return;
                }
            }

            using Transaction transaction = new(_doc, "Расширение");
            transaction.Start();
            try
            {
                Bloom(_doc, selectedElement, mepCurveType);
                transaction.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                transaction.RollBack();
                TaskDialog.Show("Ошибка", ex.Message);
            }
        }

        public void ThreeDeeBranchAlignLite()
        {
            Transaction transaction = new Transaction(_doc, "Выровнить");
            try
            {
                transaction.Start();
                MepCurveSelectionFilter curveSelectionFilter = new MepCurveSelectionFilter
                {
                    PreviousElementId = null
                };
                Reference reference = _uiDoc.Selection.PickObject((ObjectType)1, curveSelectionFilter);
                Element element1 = _doc.GetElement(reference);
                curveSelectionFilter.PreviousElementId = element1.Id;
                Element element2 = _doc.GetElement(_uiDoc.Selection.PickObject((ObjectType)1, curveSelectionFilter));

                XYZ globalPoint = reference.GlobalPoint;
                Connector[] connectorArray1 = ConnectorArray(element1);
                Connector[] connectorArray2 = ConnectorArray(element2);
                Connector stationaryElementClosestConnector = NearestConnector(connectorArray1, globalPoint);
                Connector movingElementClosestConnector = ClosestConnectorOfDomain(connectorArray2, connectorArray1,
                    stationaryElementClosestConnector.Domain);
                Connector stationaryElementFarthestConnector = FarthestConnector(connectorArray1, globalPoint);
                Connector movingElementFarthestConnector =
                    FarthestConnector(connectorArray2, movingElementClosestConnector.Origin);

                AlignIntersectingMepElements(element2, stationaryElementClosestConnector, movingElementClosestConnector,
                    stationaryElementFarthestConnector, movingElementFarthestConnector, _doc);
                transaction.Commit();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                transaction.RollBack();
            }
        }

        public void ElbowUp()
        {
            Reference selectedReference = GetSelectedReference();
            if (selectedReference == null) return;
            ElementWrapper selectedElement = new ElementWrapper(selectedReference, _doc);
            var closestConnector = selectedElement.FindClosestFreeConnector(selectedReference.GlobalPoint);
            if (closestConnector == null)
                return;
            ConnectorWrapper connector = new ConnectorWrapper(closestConnector);

            ElementId level = GetLevel(_doc, connector.Origin.Z);
            if (level == null)
                return;
            double extensionLength = GetExtensionLength(connector.Connector);
            var basisZ = connector.BasisZ;

            // Проверяем условия для определения направления смещения
            bool hasHorizontalComponents = basisZ.X != 0.0 || basisZ.Y != 0.0;
            bool isPointingDown = basisZ.Z <= 0.0;
            bool isPointingUp = basisZ.Z >= 0.0;

            XYZ end;

            if (hasHorizontalComponents || isPointingDown)
            {
                if (hasHorizontalComponents || isPointingUp)
                {
                    // Смещение вверх по Z
                    end = new XYZ(
                        connector.Origin.X, // X без изменений
                        connector.Origin.Y, // Y без изменений
                        connector.Origin.Z + extensionLength // Положительное смещение по Z
                    );
                }
                else
                {
                    // Смещение вперед по Y
                    end = new XYZ(
                        connector.Origin.X, // X без изменений
                        connector.Origin.Y + extensionLength, // Положительное смещение по Y
                        connector.Origin.Z // Z без изменений
                    );
                }
            }
            else
            {
                // Смещение назад по Y
                end = new XYZ(
                    connector.Origin.X, // X без изменений
                    connector.Origin.Y - extensionLength, // Отрицательное смещение по Y
                    connector.Origin.Z // Z без изменений
                );
            }

            Transaction transaction = new Transaction(_doc, "Поворот вверх");
            try
            {
                transaction.Start();
                switch (selectedElement.BuiltInCategory)
                {
                    case BuiltInCategory.OST_DuctCurves:
                        DrawDuct(_doc, selectedElement, connector, end);
                        break;
                    case BuiltInCategory.OST_PipeCurves:
                        DrawPipeWithElbow(_doc, selectedElement, connector, end, true);
                        break;
                }

                transaction.Commit();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Ошибка", $"Произошла ошибка: {e.Message}");
            }
        }

        public void ElbowRight()
        {
            Reference selectedReference = GetSelectedReference();
            if (selectedReference == null) return;
            ElementWrapper selectedElement = new ElementWrapper(selectedReference, _doc);
            var closestConnector = selectedElement.FindClosestFreeConnector(selectedReference.GlobalPoint);
            if (closestConnector == null)
                return;
            ConnectorWrapper connector = new ConnectorWrapper(closestConnector);

            ElementId level = GetLevel(_doc, connector.Origin.Z);
            if (level == null)
                return;
            double extensionLength = GetExtensionLength(connector.Connector);
            // Получаем вектор направления из коннектора
            var basisZ = connector.BasisZ;

            // Нормализация компонентов вектора
            double normalizedX = 1.0 * basisZ.X;
            double normalizedY = 1.0 * basisZ.Y;

            // Получаем начальную точку из коннектора
            XYZ origin = connector.Origin;

            // Проверки направления вектора
            bool hasHorizontalComponents = Math.Round(basisZ.X, 3) != 0.0 || Math.Round(basisZ.Y, 3) != 0.0;
            bool isPointingDown = basisZ.Z <= 0.0;
            bool isPointingUp = basisZ.Z >= 0.0;

            XYZ end;

            // Определяем конечную точку на основе направления вектора
            if (hasHorizontalComponents || isPointingDown)
            {
                if (hasHorizontalComponents || isPointingUp)
                {
                    // Если есть горизонтальные компоненты или вектор направлен вверх
                    end = new XYZ(
                        origin.X + extensionLength * normalizedY, // Смещение по X с учетом Y компоненты
                        origin.Y - extensionLength * normalizedX, // Смещение по Y с учетом X компоненты
                        origin.Z + basisZ.Z * extensionLength // Пропорциональное смещение по Z
                    );
                }
                else
                {
                    // Если вектор направлен вниз без горизонтальных компонент
                    end = new XYZ(
                        origin.X + extensionLength, // Положительное смещение по X
                        origin.Y, // Без смещения по Y
                        origin.Z // Без смещения по Z
                    );
                }
            }
            else
            {
                // В остальных случаях
                end = new XYZ(
                    origin.X - extensionLength, // Отрицательное смещение по X
                    origin.Y, // Без смещения по Y
                    origin.Z // Без смещения по Z
                );
            }

            Transaction transaction = new Transaction(_doc, "Поворот вправо");
            try
            {
                transaction.Start();
                switch (selectedElement.BuiltInCategory)
                {
                    case BuiltInCategory.OST_DuctCurves:
                        DrawDuct(_doc, selectedElement, connector, end);
                        break;
                    case BuiltInCategory.OST_PipeCurves:
                        DrawPipeWithElbow(_doc, selectedElement, connector, end, true);
                        break;
                }

                transaction.Commit();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Ошибка", $"Произошла ошибка: {e.Message}");
            }
        }

        public void ElbowLeft()
        {
            Reference selectedReference = GetSelectedReference();
            if (selectedReference == null) return;
            ElementWrapper selectedElement = new ElementWrapper(selectedReference, _doc);
            var closestConnector = selectedElement.FindClosestFreeConnector(selectedReference.GlobalPoint);
            if (closestConnector == null)
                return;
            ConnectorWrapper connector = new ConnectorWrapper(closestConnector);

            ElementId level = GetLevel(_doc, connector.Origin.Z);
            if (level == null)
                return;
            double extensionLength = GetExtensionLength(connector.Connector);
            var basisZ = connector.BasisZ;
            // Нормализация компонентов вектора направления
            double normalizedX = 1.0 * basisZ.X;
            double normalizedY = 1.0 * basisZ.Y;

            // Получаем точку начала из коннектора
            XYZ origin = connector.Origin;

            // Округляем значения для более точного сравнения
            bool hasXComponent = Math.Round(basisZ.X, 3) != 0.0;
            bool hasYComponent = Math.Round(basisZ.Y, 3) != 0.0;
            bool isPointingDown = basisZ.Z <= 0.0;
            bool isPointingUp = basisZ.Z >= 0.0;

            XYZ end;

            // Определяем конечную точку на основе направления вектора
            if (hasXComponent || hasYComponent || isPointingDown)
            {
                if (hasXComponent || hasYComponent || isPointingUp)
                {
                    // Если вектор имеет горизонтальные компоненты или направлен вверх
                    end = new XYZ(
                        origin.X - extensionLength * normalizedY, // Смещение по X
                        origin.Y + extensionLength * normalizedX, // Смещение по Y
                        origin.Z + basisZ.Z * extensionLength // Смещение по Z пропорционально компоненте Z
                    );
                }
                else
                {
                    // Если вектор направлен вниз без горизонтальных компонент
                    end = new XYZ(
                        origin.X - extensionLength, // Отрицательное смещение по X
                        origin.Y, // Без смещения по Y
                        origin.Z // Без смещения по Z
                    );
                }
            }
            else
            {
                // В остальных случаях
                end = new XYZ(
                    origin.X + extensionLength, // Положительное смещение по X
                    origin.Y, // Без смещения по Y
                    origin.Z // Без смещения по Z
                );
            }

            Transaction transaction = new Transaction(_doc, "Поворот влево");
            try
            {
                transaction.Start();

                switch (selectedElement.BuiltInCategory)
                {
                    case BuiltInCategory.OST_DuctCurves:
                        DrawDuct(_doc, selectedElement, connector, end);
                        break;
                    case BuiltInCategory.OST_PipeCurves:
                        DrawPipeWithElbow(_doc, selectedElement, connector, end, true);
                        break;
                }

                transaction.Commit();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Ошибка", $"Произошла ошибка: {e.Message}");
            }
        }

        public void ElbowDownFortyFive()
        {
            Reference selectedReference = GetSelectedReference();
            if (selectedReference == null) return;
            ElementWrapper selectedElement = new ElementWrapper(selectedReference, _doc);
            var closestConnector = selectedElement.FindClosestFreeConnector(selectedReference.GlobalPoint);
            if (closestConnector == null)
                return;
            ConnectorWrapper connector = new ConnectorWrapper(closestConnector);

            ElementId level = GetLevel(_doc, connector.Origin.Z);
            if (level == null)
                return;
            double extensionLength = GetExtensionLength(connector.Connector);
            var basisZ = connector.BasisZ;
            XYZ end;
            bool hasHorizontalComponent = basisZ.X != 0.0 || basisZ.Y != 0.0 || basisZ.Z >= 0.0;

            if (hasHorizontalComponent)
            {
                end = new XYZ(
                    connector.Origin.X + basisZ.X * extensionLength,
                    connector.Origin.Y + basisZ.Y * extensionLength,
                    connector.Origin.Z - extensionLength);
            }
            else
            {
                end = new XYZ(
                    connector.Origin.X,
                    connector.Origin.Y + extensionLength,
                    connector.Origin.Z - extensionLength);
            }

            Transaction transaction = new Transaction(_doc, "Поворот вниз на 45°");
            try
            {
                transaction.Start();

                switch (selectedElement.BuiltInCategory)
                {
                    case BuiltInCategory.OST_DuctCurves:
                        DrawDuct(_doc, selectedElement, connector, end);
                        break;
                    case BuiltInCategory.OST_PipeCurves:
                        DrawPipeWithElbow(_doc, selectedElement, connector, end, true);
                        break;
                }

                transaction.Commit();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Ошибка", $"Произошла ошибка: {e.Message}");
            }
        }

        public void Tap()
        {
            MepCurveSelectionFilter selectionFilter = new MepCurveSelectionFilter();
            Connector connector;
            MEPCurve mepCurve;
            try
            {
                // Пытаемся выбрать первый элемент
                var targetElement =
                    _doc.GetElement(_uiDoc.Selection.PickObject(ObjectType.Element, selectionFilter));
                selectionFilter.PreviousElementId = targetElement.Id;

                // Пытаемся выбрать второй элемент
                var attachElement =
                    _doc.GetElement(_uiDoc.Selection.PickObject(ObjectType.Element, selectionFilter));

                // Обработка коннекторов после успешного выбора
                var cA2 = ConnectorArray(targetElement);
                connector = ClosestConnector(ConnectorArray(attachElement), cA2);
                mepCurve = targetElement as MEPCurve;
            }
            catch (OperationCanceledException)
            {
                // Пользователь отменил выбор - выходим из метода
                return;
            }
            catch (Exception)
            {
                // Обработка других исключений при выборе
                return;
            }

            // Если выбор элементов прошел успешно, начинаем транзакцию
            using Transaction transaction = new Transaction(_doc, "Tap");
            try
            {
                transaction.Start();
                _doc.Create.NewTakeoffFitting(connector, mepCurve);
                transaction.Commit();
            }

            catch (Exception ex)
            {
                // Если произошла ошибка, откатываем транзакцию
                if (transaction.HasStarted())
                    transaction.RollBack();

                TaskDialog.Show("Ошибка при создании", ex.Message);
            }
        }

        public void ElbowDown()
        {
            Reference selectedReference = GetSelectedReference();
            if (selectedReference == null) return;
            ElementWrapper selectedElement = new ElementWrapper(selectedReference, _doc);
            var closestConnector = selectedElement.FindClosestFreeConnector(selectedReference.GlobalPoint);
            if (closestConnector == null)
                return;
            ConnectorWrapper connector = new ConnectorWrapper(closestConnector);

            ElementId level = GetLevel(_doc, connector.Origin.Z);
            if (level == null)
                return;
            double extensionLength = GetExtensionLength(connector.Connector);
            var basisZ = connector.BasisZ;
            XYZ end;
            if (basisZ.X != 0.0 || basisZ.Y != 0.0 || basisZ.Z <= 0.0)
            {
                if (basisZ.X != 0.0 || basisZ.Y != 0.0 || basisZ.Z >= 0.0)
                {
                    // Смещение вниз по Z
                    end = new XYZ(connector.Origin.X,
                        connector.Origin.Y,
                        connector.Origin.Z - extensionLength);
                }
                else
                {
                    // Смещение вверх по Y
                    end = new XYZ(connector.Origin.X,
                        connector.Origin.Y + extensionLength,
                        connector.Origin.Z);
                }
            }
            else
            {
                // Смещение вниз по Y
                end = new XYZ(connector.Origin.X,
                    connector.Origin.Y - extensionLength,
                    connector.Origin.Z);
            }

            Transaction transaction = new(_doc, "Поворот вниз");
            try
            {
                transaction.Start();

                switch (selectedElement.BuiltInCategory)
                {
                    case BuiltInCategory.OST_DuctCurves:
                        DrawDuct(_doc, selectedElement, connector, end);
                        break;
                    case BuiltInCategory.OST_PipeCurves:
                        DrawPipeWithElbow(_doc, selectedElement, connector, end, true);
                        break;
                }

                transaction.Commit();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Ошибка", $"Произошла ошибка: {e.Message}");
            }
        }

        private static Reference GetSelectedReference()
        {
            Reference selectedReference;
            try
            {
                selectedReference = Context.ActiveUiDocument?.Selection.PickObject(ObjectType.PointOnElement,
                    new MepCurveSelectionFilter(), "Пожалуйста, выберите точку на воздуховоде, трубе или кабеле.");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }

            return selectedReference;
        }

        public void ElbowUpFortyFive()
        {
            Reference selectedReference = GetSelectedReference();
            if (selectedReference == null) return;
            ElementWrapper selectedElement = new ElementWrapper(selectedReference, _doc);
            var closestConnector = selectedElement.FindClosestFreeConnector(selectedReference.GlobalPoint);
            if (closestConnector == null)
                return;
            ConnectorWrapper connector = new ConnectorWrapper(closestConnector);

            ElementId level = GetLevel(_doc, connector.Origin.Z);
            if (level == null)
                return;
            double extensionLength = GetExtensionLength(connector.Connector);
            var basisZ = connector.BasisZ;
            XYZ endPoint;
            // Если есть отклонение по X или Y, или Z направлен вверх
            bool hasHorizontalComponent = basisZ.X != 0.0 || basisZ.Y != 0.0 || basisZ.Z >= 0.0;

            if (hasHorizontalComponent)
            {
                endPoint = new XYZ(
                    connector.Origin.X + basisZ.X * extensionLength,
                    connector.Origin.Y + basisZ.Y * extensionLength,
                    connector.Origin.Z + extensionLength
                );
            }
            else
            {
                // В противном случае просто смещаем по Y и Z
                endPoint = new XYZ(
                    connector.Origin.X,
                    connector.Origin.Y + extensionLength,
                    connector.Origin.Z + extensionLength
                );
            }

            using Transaction transaction = new Transaction(_doc, "Поворот вверх на 45°");
            try
            {
                transaction.Start();
                switch (selectedElement.BuiltInCategory)
                {
                    case BuiltInCategory.OST_DuctCurves:
                        DrawDuct(_doc, selectedElement, connector, endPoint);
                        break;
                    case BuiltInCategory.OST_PipeCurves:
                        DrawPipeWithElbow(_doc, selectedElement, connector, endPoint, true);
                        break;
                }

                transaction.Commit();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Произошла ошибка: {ex.Message}");
            }
        }

        private XYZ CalculateEndPoint(XYZ origin, XYZ basisZ, double extensionLength)
        {
            // Вычисление конечной точки в зависимости от направления BasisZ
            if (basisZ.X != 0.0 || basisZ.Y != 0.0 || basisZ.Z >= 0.0)
            {
                return new XYZ(
                    origin.X + basisZ.X * extensionLength,
                    origin.Y + basisZ.Y * extensionLength,
                    origin.Z + extensionLength);
            }

            return new XYZ(
                origin.X,
                origin.Y + extensionLength,
                origin.Z + extensionLength);
        }

        public void MoveConnectAlign()
        {
            if (!TryBuildContext(out AlignContext ctx))
                return;
            using Transaction tr = new(_doc, "Соединить");
            // FailureHandlingOptions options = tr.GetFailureHandlingOptions();
            // options.SetFailuresPreprocessor(new CustomFailurePreprocessor());
            // tr.SetFailureHandlingOptions(options);
            try
            {
                tr.Start();
                // Шаг 1: Выбор целевого элемента и получение точки
                if (AreOpposite(ctx.TargetConn.Connector, ctx.AttachConn.Connector, 0.001))
                {
                    if (!HandleOppositeConnectors(ctx))
                    {
                        tr.RollBack();
                        return; // Пользователь нажал Cancel
                    }
                }
                else
                {
                    AlignAndConnect(ctx);
                }

                tr.Commit();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Ошибка", e.Message);
                tr.RollBack();
            }
        }

        private bool HandleOppositeConnectors(AlignContext ctx)
        {
            return ctx.Attach.Element is Pipe or Duct ? HandlePipeOrDuctOpposite(ctx) : HandleGenericOpposite(ctx);
        }

        private bool HandlePipeOrDuctOpposite(AlignContext ctx)
        {
            var choice = CustomDialogWindow.ShowDialog(
                "Соединить",
                "Выберите действие",
                ("Переместить с удлинением/укорочением трубы", 1),
                ("Переместить все элементы", 2));
            // Сначала проверяем на отмену
            if (choice == 0) // или то значение, которое вы определили для отмены
            {
                return false;
            }

            switch (choice)
            {
                case 1: // Удлинить-укоротить

                    LengthenCurve(ctx.AttachConn.Connector, ctx.TargetConn.Connector);
                    XYZ newMove = ctx.TargetConn.Origin - ctx.AttachConn.Origin;
                    ElementTransformUtils.MoveElement(_doc, ctx.Attach.Id, newMove);

                    if (ctx.Attach.Element is Pipe or Duct && ctx.Target.Element is Pipe or Duct)
                    {
                        if (DrainPipes(ctx)) return true;
                    }

                    break;

                case 2: // Переместить
                    AlignAndConnect(ctx);
                    break;

                default: return false; // Cancel
            }

            return true;
        }

        private bool DrainPipes(AlignContext ctx)
        {
            switch (ctx.Attach.Element)
            {
                case Pipe pipe:
                {
                    if (!ArePipesSimilar(pipe, ctx.Target.Element as Pipe)) return false;
                }
                    break;
                case Duct duct:
                {
                    if (!AreDuctSimilar(duct, ctx.Target.Element as Duct)) return false;
                }
                    break;
                default: return false;
            }

            if ((ctx.TargetConn.Origin - ctx.AttachConn.Origin).GetLength() >= 0.01)
            {
                XYZ newMove = ctx.TargetConn.Origin - ctx.AttachConn.Origin;
                ElementTransformUtils.MoveElement(_doc, ctx.Attach.Id, newMove);
            }

            Connector connectedConnector = ctx.Attach.Connectors.FirstOrDefault(x => x.ConnectedConnector != null)
                ?.ConnectedConnector;
            if (connectedConnector == null)
            {
                connectedConnector = ctx.Attach.Connectors.FirstOrDefault(x => x.Id != ctx.AttachConn.Id)
                    ?.Connector;

                LengthenCurve(ctx.TargetConn.Connector, connectedConnector);
                _doc.Delete(ctx.Attach.Element.Id);
            }
            else
            {
                LengthenCurve(ctx.TargetConn.Connector, connectedConnector);
                _doc.Delete(ctx.Attach.Element.Id);
                if (connectedConnector is { IsValidObject: true })
                {
                    connectedConnector?.ConnectTo(ctx.TargetConn.Connector);
                }

                return true;
            }

            return false;
        }

        private bool ArePipesSimilar(Pipe pipe1, Pipe pipe2)
        {
            // Проверяем тип трубы
            if (pipe1.PipeType.Id != pipe2.PipeType.Id)
                return false;

            // Проверяем диаметр
            Parameter diameter1 = pipe1.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            Parameter diameter2 = pipe2.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);

            if (diameter1 == null || diameter2 == null)
                return false;

            // Сравниваем значения диаметров с небольшой погрешностью
            const double tolerance = 0.001;
            return Math.Abs(diameter1.AsDouble() - diameter2.AsDouble()) < tolerance;
        }

        private bool AreDuctSimilar(Duct duct1, Duct duct2)
        {
            // Проверяем тип воздуховода
            if (duct1.DuctType.Id != duct2.DuctType.Id)
                return false;

            // Получаем форму воздуховода через коннекторы
            ConnectorManager cm1 = duct1.ConnectorManager;
            ConnectorManager cm2 = duct2.ConnectorManager;

            if (cm1 == null || cm2 == null)
                return false;

            // Берем первый коннектор для определения формы
            Connector c1 = cm1.Connectors.Cast<Connector>().FirstOrDefault();
            Connector c2 = cm2.Connectors.Cast<Connector>().FirstOrDefault();

            if (c1 == null || c2 == null)
                return false;

            // Проверяем форму воздуховода
            if (c1.Shape != c2.Shape)
                return false;

            // В зависимости от формы проверяем размеры
            switch (c1.Shape)
            {
                case ConnectorProfileType.Round:
                    // Для круглого - проверяем диаметр
                    Parameter diameter1 = duct1.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                    Parameter diameter2 = duct2.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);

                    if (diameter1 == null || diameter2 == null)
                        return false;

                    return AreValuesEqual(diameter1.AsDouble(), diameter2.AsDouble());

                case ConnectorProfileType.Rectangular:
                case ConnectorProfileType.Oval:
                    // Для прямоугольного - проверяем высоту и ширину
                    Parameter height1 = duct1.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
                    Parameter height2 = duct2.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
                    Parameter width1 = duct1.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
                    Parameter width2 = duct2.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);

                    if (height1 == null || height2 == null || width1 == null || width2 == null)
                        return false;

                    return AreValuesEqual(height1.AsDouble(), height2.AsDouble()) &&
                           AreValuesEqual(width1.AsDouble(), width2.AsDouble());
                default:
                    return false;
            }
        }

        private bool AreValuesEqual(double value1, double value2)
        {
            const double tolerance = 0.001;
            return Math.Abs(value1 - value2) < tolerance;
        }

        private bool HandleGenericOpposite(AlignContext ctx)
        {
            XYZ translationVector = ctx.TargetConn.Origin - ctx.AttachConn.Origin;
            var choice = CustomDialogWindow.ShowDialog(
                "Соединить",
                "Выберите действие",
                ("Переместить выбранный элемент", 1),
                ("Переместить все элементы", 2));
            // Сначала проверяем на отмену
            if (choice == 0) // или то значение, которое вы определили для отмены
            {
                return false;
            }

            switch (choice)
            {
                case 1:
                    ElementTransformUtils.MoveElement(_doc, ctx.Attach.Id, translationVector);
                    // 3. Соединяем элементы
                    ctx.AttachConn.Connector.ConnectTo(ctx.TargetConn.Connector);
                    return true;

                case 2:
                    AlignAndConnect(ctx); // Пользовательская логика
                    return true;

                default:
                    return false; // Cancel
            }
        }

        private static int ShowChoiceDialog(
            string title,
            string instruction,
            params (string text, int id)[] commands)
        {
            var dlg = new TaskDialog(title)
            {
                MainInstruction = instruction,
                MainIcon = TaskDialogIcon.TaskDialogIconNone,
                CommonButtons = TaskDialogCommonButtons.Cancel
            };

            foreach (var cmd in commands)
                dlg.AddCommandLink((TaskDialogCommandLinkId)cmd.id, cmd.text);

            TaskDialogResult res = dlg.Show();

            // Проверяем, была ли нажата кнопка отмены
            if (res == TaskDialogResult.Cancel)
            {
                return 0; // или любое другое значение, которое вы хотите использовать для отмены
            }

            return (int)res;
        }

        private void AlignAndConnect(AlignContext ctx)
        {
            // Шаг 5: Отключение существующих соединений и сохранение их для восстановления
            var existingConnections = DisconnectExistingConnections(ctx.Attach);
            // Шаг 6: Выравнивание соединителей
            AlignConnectors(ctx.TargetConn.Connector, ctx.AttachConn.Connector, ctx.Attach.Element);
            var translationVector = ctx.TargetConn.Origin - ctx.AttachConn.Origin;
            ElementTransformUtils.MoveElement(_doc, ctx.Attach.Id, translationVector);
            if (!(ctx.Attach.Element is MEPCurve && ctx.Target.Element is MEPCurve))
            {
                // Соединение после вращения
                ctx.AttachConn.Connector.ConnectTo(ctx.TargetConn.Connector);
            }

            // Шаг 8: Восстановление предыдущих соединений
            if (existingConnections.Any())
            {
                RestoreConnections(existingConnections);
            }

            DrainPipes(ctx);
        }

        /// <summary>
        /// Выравнивает коннекторы элементов для соединения
        /// </summary>
        private void AlignConnectors(Connector targetConnector, Connector attachingConnector, Element attachingElement)
        {
            if (targetConnector == null || attachingConnector == null || attachingElement == null)
                throw new ArgumentNullException($"Null arguments are not allowed");

            var (angle, rotationAxis) = CalculateRotationParameters(targetConnector, attachingConnector);

            if (ShouldRotate(angle))
            {
                RotateElement(attachingElement, attachingConnector.Origin, rotationAxis, angle);
            }
        }

        /// <summary>
        /// Вычисляет параметры вращения для выравнивания коннекторов
        /// </summary>
        private (double angle, XYZ rotationAxis) CalculateRotationParameters(Connector targetConnector,
            Connector attachingConnector)
        {
            var targetDirection = targetConnector.CoordinateSystem.BasisZ.Normalize();
            var attachingDirection = attachingConnector.CoordinateSystem.BasisZ.Normalize();
            var desiredDirection = -targetDirection;

            // Вычисляем скалярное произведение и угол
            var dotProduct = ClampDotProduct(attachingDirection.DotProduct(desiredDirection));
            var angle = Math.Acos(dotProduct);

            // Вычисляем ось вращения
            var rotationAxis = attachingDirection.CrossProduct(desiredDirection);

            // Обрабатываем случай параллельных векторов
            return rotationAxis.IsZeroLength()
                ? HandleParallelVectors(dotProduct, attachingConnector, angle)
                : (angle, rotationAxis.Normalize());
        }

        /// <summary>
        /// Обрабатывает случай параллельных векторов
        /// </summary>
        private (double angle, XYZ rotationAxis) HandleParallelVectors(double dotProduct, Connector attachingConnector,
            double angle)
        {
            if (angle < 0) throw new ArgumentOutOfRangeException(nameof(angle));
            const double PARALLEL_THRESHOLD = -0.9999;

            if (dotProduct < PARALLEL_THRESHOLD)
            {
                // Векторы сонаправлены, нужен разворот на 180 градусов
                angle = Math.PI;
                return (angle, GetPerpendicularAxis(attachingConnector));
            }

            // Векторы противонаправлены, вращение не требуется
            return (0, XYZ.Zero);
        }

        /// <summary>
        /// Получает перпендикулярную ось для вращения
        /// </summary>
        private XYZ GetPerpendicularAxis(Connector connector)
        {
            var basisX = connector.CoordinateSystem.BasisX;
            return !basisX.IsZeroLength() ? basisX : connector.CoordinateSystem.BasisY;
        }

        /// <summary>
        /// Ограничивает значение скалярного произведения в диапазоне [-1, 1]
        /// </summary>
        private double ClampDotProduct(double dotProduct)
        {
            return Math.Min(Math.Max(dotProduct, -1.0), 1.0);
        }

        /// <summary>
        /// Определяет, нужно ли выполнять вращение
        /// </summary>
        private bool ShouldRotate(double angle)
        {
            const double ROTATION_THRESHOLD = 1e-6;
            return angle > ROTATION_THRESHOLD;
        }

        /// <summary>
        /// Выполняет вращение элемента
        /// </summary>
        private void RotateElement(Element element, XYZ origin, XYZ axis, double angle)
        {
            var rotationLine = Line.CreateUnbound(origin, axis);
            ElementTransformUtils.RotateElement(_doc, element.Id, rotationLine, angle);
        }

        /// <summary>
        /// Проверяет, являются ли два коннектора противоположно направленными.
        /// </summary>
        /// <param name="targetConnector">Первый коннектор для проверки.</param>
        /// <param name="attachConnector">Второй коннектор для проверки.</param>
        /// <param name="tolerance">Погрешность вычислений</param>
        /// <returns>
        /// true - если коннекторы направлены противоположно друг другу (угол между их осями Z равен 180 градусов);
        /// false - в противном случае.
        /// </returns>
        /// <remarks>
        /// Метод использует скалярное произведение векторов направления (BasisZ) коннекторов.
        /// </remarks>
        private static bool AreOpposite(Connector targetConnector, Connector attachConnector, double tolerance)
        {
            double dot = targetConnector.CoordinateSystem.BasisZ
                .DotProduct(attachConnector.CoordinateSystem.BasisZ);
            const double oppositeThreshold = -1.0;
            return Math.Abs(Math.Round(dot, 10) - Math.Round(oppositeThreshold, 10)) < tolerance;
        }

        private bool TryBuildContext(out AlignContext ctx)
        {
            ctx = default;


            if (!TryPickElement(
                    "Выберите точку на элементе, к которому хотите присоединить",
                    out var target, out var targetPt))
                return false;


            if (!TryPickElement(
                    "Выберите точку на присоединяемом элементе",
                    out var attach, out var attachPt,
                    target))
                return false;

            ctx = new AlignContext(target, attach, targetPt, attachPt);
            return true;
        }

        private bool TryPickElement(string prompt, out Element element, out XYZ pickedPoint,
            Element elementToExclude = null)
        {
            element = null;
            pickedPoint = null;

            ISelectionFilter filter = new CategorySelectionFilter
            {
                SelectedElement = elementToExclude
            };

            try
            {
                Reference r = _uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    filter,
                    prompt);

                pickedPoint = r?.GlobalPoint;
                element = r == null ? null : _doc.GetElement(r);

                return element != null;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // Пользователь нажал Esc
                return false;
            }
        }

        /// <summary>
        /// Если присоединяемый элемент является кривой, то ее длина удлиняется по направлению к соединяемому элементу
        /// </summary>
        /// <param name="attachingConnector"></param>
        /// <param name="targetConnector"></param>
        private static void LengthenCurve(Connector attachingConnector, Connector targetConnector)
        {
            if (attachingConnector.Owner.Location is not LocationCurve locationCurve) return;
            // 1. Удлиняем трубу
            XYZ startPoint = locationCurve.Curve.GetEndPoint(0);
            XYZ endPoint = locationCurve.Curve.GetEndPoint(1);

            double startDistance = startPoint.DistanceTo(attachingConnector.Origin);
            double endDistance = endPoint.DistanceTo(attachingConnector.Origin);

            // Определяем, какой конец трубы ближе к соединителю элемента
            XYZ pointToExtend;
            XYZ otherPoint;
            XYZ pipeDirection;
            XYZ extensionPoint;
            Line newCurve;

            if (startDistance < endDistance)
            {
                // Удлиняем от startPoint
                pointToExtend = startPoint;
                otherPoint = endPoint;
                pipeDirection = (startPoint - endPoint).Normalize();

                // Вектор от точки удлинения до коннектора целевого элемента
                XYZ vectorToTarget = targetConnector.Origin - pointToExtend;

                // Расстояние вдоль направления pipeDirection
                double extensionLength = vectorToTarget.DotProduct(pipeDirection);

                // Вычисляем новую точку начала трубы
                extensionPoint = pointToExtend + pipeDirection * extensionLength;

                // Создаем новую линию (трубу) от extensionPoint до endPoint (otherPoint)
                newCurve = Line.CreateBound(extensionPoint, otherPoint);
            }
            else
            {
                // Удлиняем от endPoint
                pointToExtend = endPoint;
                otherPoint = startPoint;
                pipeDirection = (endPoint - startPoint).Normalize();

                // Вектор от точки удлинения до коннектора целевого элемента
                XYZ vectorToTarget = targetConnector.Origin - pointToExtend;

                // Расстояние вдоль направления pipeDirection
                double extensionLength = vectorToTarget.DotProduct(pipeDirection);
                if (extensionLength <= 0)
                {
                    return;
                }

                // Вычисляем новую конечную точку трубы
                extensionPoint = pointToExtend + pipeDirection * extensionLength;

                // Создаем новую линию (трубу) от startPoint (otherPoint) до extensionPoint
                newCurve = Line.CreateBound(otherPoint, extensionPoint);
            }

            locationCurve.Curve = newCurve;
        }

        private List<ConnectorConnection> DisconnectExistingConnections(ElementWrapper element)
        {
            var connectorConnections = new List<ConnectorConnection>();

            foreach (ConnectorWrapper connector in element.Connectors)
            {
                if (!connector.IsConnected)
                {
                    continue;
                }

                // Собираем подключенные коннекторы перед отключением
                var connectedConnectors = new List<Connector>();

                connectedConnectors.Add(connector.ConnectedConnector);

                var connectorConnection = new ConnectorConnection(connector.Connector);
                // Отключаем и записываем подключенные коннекторы
                foreach (Connector connectedConnector in connectedConnectors)
                {
                    try
                    {
                        // Сохраняем подключенный коннектор
                        connectorConnection.AddConnectedConnector(connectedConnector);

                        // Отключаем коннекторы
                        connector.Connector.DisconnectFrom(connectedConnector);
                    }
                    catch (Exception ex)
                    {
                        // ignored
                    }
                }


                if (connectorConnection.ConnectedConnectors.Count > 0)
                {
                    connectorConnections.Add(connectorConnection);
                }
            }

            return connectorConnections;
        }


        private List<Connector> GetConnectedConnectors(ConnectorManager connectorManager)
        {
            // Собираем подключенные коннекторы перед отключением
            var connectedConnectors = new List<Connector>();

            foreach (Connector connector in connectorManager.Connectors)
            {
                foreach (Connector connectedConnector in connector.AllRefs)
                {
                    if (!IsPhysicalDomain(connectedConnector.Domain))
                        continue;
                    if (!connectedConnector.IsConnected) continue;

                    connectedConnectors.Add(connectedConnector);
                }
            }

            return connectedConnectors;
        }

        private bool IsPhysicalDomain(Domain domain)
        {
            return domain == Domain.DomainHvac ||
                   domain == Domain.DomainPiping ||
                   domain == Domain.DomainElectrical;
        }

        private void RestoreConnections(List<ConnectorConnection> connections)
        {
            const int maxIterations = 20;
            int iteration = 0;
            while (connections.Count > 0 && iteration < maxIterations)
            {
                var newConnections = new List<ConnectorConnection>();
                // Копируем список, чтобы избежать изменения коллекции во время итерации
                foreach (var connectorConnection in connections.ToList())
                {
                    var targetConnector = connectorConnection.TargetConnector;
                    var connectedConnectorInfos = connectorConnection.ConnectedConnectors;
                    if (targetConnector == null || connectedConnectorInfos == null ||
                        connectedConnectorInfos.Count == 0)
                    {
                        // Удаляем некорректные или пустые подключения из списка для обработки
                        connections.Remove(connectorConnection);
                        continue;
                    }

                    foreach (var connectedInfo in connectedConnectorInfos)
                    {
                        var attachingConnector = connectedInfo.TargetConnector;
                        var attachingElement = new ElementWrapper(connectedInfo.Element);
                        if (attachingConnector == null || attachingElement == null)
                        {
                            // Пропускаем некорректные соединения
                            continue;
                        }


                        // Отключаем существующие подключения присоединяемого элемента
                        var existingConnections = DisconnectExistingConnections(attachingElement);

                        // Добавляем отключённые соединения в список для последующей обработки
                        newConnections.AddRange(existingConnections);

                        // Вычисляем вектор перемещения
                        var translationVector = targetConnector.Origin - attachingConnector.Origin;

                        // Перемещаем присоединяемый элемент
                        ElementTransformUtils.MoveElement(_doc, attachingElement.Id, translationVector);
                        // Выравниваем коннекторы
                        AlignConnectors(targetConnector, attachingConnector, attachingElement.Element);
                        attachingConnector.ConnectTo(targetConnector);
                    }

                    // Удаляем обработанный объект из списка для обработки
                    connections.Remove(connectorConnection);
                }

                // Добавляем новые соединения для обработки в следующей итерации
                connections.AddRange(newConnections);

                iteration++;
            }
        }

        private ConnectorManager GetConnectorManager(Element element)
        {
            switch (element)
            {
                // Проверяем тип элемента
                case MEPCurve pipe:
                {
                    // Логика для Pipe
                    return pipe.ConnectorManager;
                }
                case FamilyInstance familyInstance:
                {
                    // Логика для FamilyInstance
                    // Получаем MEPModel, только если это MEP-тип семейства
                    MEPModel mepModel = familyInstance.MEPModel;
                    {
                        return mepModel.ConnectorManager;
                    }
                }
                default: return null;
            }
        }


        private bool IsValidForConnection(ConnectorWrapper attachingConnector, ConnectorWrapper targetConnectorWrapper)
        {
            return attachingConnector?.Connector != null &&
                   targetConnectorWrapper?.Connector != null &&
                   attachingConnector.Owner != null;
        }


        private void ConnectElements(ConnectorWrapper attachingConnector,
            ConnectorWrapper targetConnectorWrapper,
            ElementWrapper attachingElement)
        {
            using (Transaction tx = new Transaction(_doc, "Connect Elements"))
            {
                tx.Start();

                // Перемещение
                var translationVector = targetConnectorWrapper.Origin - attachingConnector.Origin;
                ElementTransformUtils.MoveElement(_doc, attachingElement.Id, translationVector);

                // Выравнивание
                AlignConnectors(targetConnectorWrapper.Connector,
                    attachingConnector.Connector,
                    attachingElement.Element);

                // Соединение
                attachingConnector.Connector.ConnectTo(targetConnectorWrapper.Connector);

                tx.Commit();
            }
        }


        private void MergeDictionaries(Dictionary<Connector, Element> target, Dictionary<Connector, Element> source)
        {
            foreach (var kvp in source.Where(kvp => !target.ContainsKey(kvp.Key)))
            {
                target.Add(kvp.Key, kvp.Value);
            }
        }

        private List<Element> GetIsConnectedElements(Document doc, Element element)
        {
            List<Element> elements = [];
            switch (element)
            {
                case FamilyInstance familyInstance:
                {
                    MEPModel mepModel = familyInstance.MEPModel;
                    ConnectorManager connectorManager = mepModel.ConnectorManager;
                    foreach (Connector connector in connectorManager.Connectors)
                    {
                        // Список подключённых объектов
                        foreach (Connector refConn in connector.AllRefs)
                        {
                            // Пропустить циклическую ссылку на самого себя
                            if (refConn.Owner.Id == element.Id) continue;
                            // Получить присоединённый элемент
                            Element connectedElement = doc.GetElement(refConn.Owner.Id);
                            elements.Add(connectedElement);
                        }
                    }
                }
                    break;
                case Pipe pipe:
                {
                    ConnectorManager connectorManager = pipe.ConnectorManager;
                    foreach (Connector connector in connectorManager.Connectors)
                    {
                        // Список подключённых объектов
                        foreach (Connector refConn in connector.AllRefs)
                        {
                            // Пропустить циклическую ссылку на самого себя
                            if (refConn.Owner.Id == element.Id) continue;
                            // Получить присоединённый элемент
                            Element connectedElement = doc.GetElement(refConn.Owner.Id);
                            elements.Add(connectedElement);
                        }
                    }
                }
                    break;
            }

            return elements;
        }


// Вспомогательный метод для поиска ближайшего соединителя
        private Connector FindClosestConnector(ConnectorManager connectorManager, XYZ pickedPoint)
        {
            Connector closestConnector = null;

            // Все соединители элемента
            var connectors = connectorManager.Connectors.Cast<Connector>();

            double closestDistance = double.MaxValue;

            foreach (Connector connector in connectors)
            {
                // Координаты текущего соединителя
                XYZ connectorOrigin = connector.Origin;

                // Расстояние между выбранной точкой и соединителем
                double distance = pickedPoint.DistanceTo(connectorOrigin);

                // Проверяем, является ли это расстояние минимальным
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestConnector = connector;
                }
            }

            return closestConnector;
        }

        public static void DrawConduit(Document doc, Reference selectedReference, XYZ selectedPoint,
            Connector closestConnector1, XYZ start, XYZ end, ElementId levelId)
        {
            try
            {
                if (doc.GetElement(selectedReference) is not Conduit element) return;
                ElementId typeId = element.GetTypeId();
                doc.GetElement(typeId);
                double diameter = element.Diameter;
                Conduit newPipe = Conduit.Create(doc, typeId, start, end, levelId);
                Connector[] cA = ConnectorArray(newPipe);
                foreach (Connector connector in cA)
                    connector.Radius = diameter / 2.0;
                Connector connector1 = NearestConnector(cA, selectedPoint);

                FamilyInstance newElbow = doc.Create.NewElbowFitting(closestConnector1, connector1);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Произошла ошибка: {ex.Message}");
            }
        }

        public static void DrawDuct(Document doc, ElementWrapper element,
            ConnectorWrapper closestConnector, XYZ end)
        {
            if (doc == null || closestConnector == null || element == null || end == null)
            {
                throw new ArgumentNullException("Один или несколько входных параметров равны null.");
            }

            if (element.Element is not Duct duct)
            {
                TaskDialog.Show("Ошибка", "Выбранный элемент не является воздуховодом.");
                return;
            }

            try
            {
                // Вычисляем вектор направления от start до end
                XYZ directionVector = end - closestConnector.Origin;
                double length = directionVector.GetLength();

                if (length <= 0)
                {
                    TaskDialog.Show("Предупреждение", "Начальная и конечная точки совпадают. Воздуховод не создан.");
                    return;
                }

                // Нормализуем вектор направления
                XYZ unitDirection = directionVector.Normalize();

                // Определяем новую конечную точку увеличив длину на дополнительную длину
                XYZ adjustedEnd = end + unitDirection;

                DuctType ductType = duct.DuctType;
                MechanicalSystemType mechanicalSystem = GetMechanicalSystem(doc, closestConnector.Connector);
                var selectedPoint = element.GetGlobalPoint();
                ElementId level = GetLevel(doc, selectedPoint.Z);

                Duct newDuct = Duct.Create(doc, mechanicalSystem.Id, ductType.Id, level, closestConnector.Origin,
                    adjustedEnd);

                if (newDuct == null)
                {
                    TaskDialog.Show("Ошибка", "Не удалось создать новый воздуховод.");
                    return;
                }

                // Получаем коннекторы нового воздуховода
                ConnectorSet connectorsNewDuctSet = newDuct.ConnectorManager.Connectors;
                List<Connector> connectorsNewDuct = connectorsNewDuctSet.Cast<Connector>().ToList();
                Connector closestConnectorNewDuct = NearestConnector(connectorsNewDuct.ToArray(), selectedPoint);

                // Настройка параметров нового воздуховода
                switch (closestConnector.Shape)
                {
                    case ConnectorProfileType.Rectangular:
                    case ConnectorProfileType.Oval:
                        double width = duct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble();
                        double height = duct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble();

                        if (width <= 0 || height <= 0)
                        {
                            TaskDialog.Show("Ошибка", "Некорректные размеры исходного воздуховода.");
                            return;
                        }

                        Parameter widthParamNewDuct = newDuct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
                        Parameter heightParamNewDuct = newDuct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
                        if (widthParamNewDuct is { IsReadOnly: false })
                            widthParamNewDuct.Set(width);

                        if (heightParamNewDuct is { IsReadOnly: false })
                            heightParamNewDuct.Set(height);

                        break;

                    case ConnectorProfileType.Round:
                        double diameter = duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM).AsDouble();
                        if (diameter <= 0)
                        {
                            TaskDialog.Show("Ошибка", "Некорректный диаметр исходного воздуховода.");
                            return;
                        }

                        Parameter diameterParam = newDuct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                        if (diameterParam is { IsReadOnly: false })
                        {
                            diameterParam.Set(diameter);
                        }

                        break;

                    default:
                        TaskDialog.Show("Предупреждение", "Неподдерживаемая форма воздуховода.");
                        return;
                }

                if (Math.Abs(closestConnector.Origin.X - end.X) < 0.001 &&
                    Math.Abs(closestConnector.Origin.Y - end.Y) < 0.001)
                {
                    XYZ basisZ = closestConnector.CoordinateSystem.BasisZ;
                    XYZ source = new XYZ(0.0, 1.0, 0.0);
                    double angle = basisZ.AngleTo(source);
                    if (basisZ.DotProduct(source) > 0.0 && basisZ.X > 1.0)
                        angle = -angle;
                    Line bound = Line.CreateBound(closestConnector.Origin, end);
                    if (closestConnector.Origin.Z > end.Z)
                        bound = Line.CreateBound(end, closestConnector.Origin);
                    ElementTransformUtils.RotateElement(doc, newDuct.Id, bound, angle);
                }

                // Создаём отвод между коннекторами
                FamilyInstance newElbow =
                    doc.Create.NewElbowFitting(closestConnector.Connector, closestConnectorNewDuct);
                if (newElbow == null)
                {
                    TaskDialog.Show("Ошибка", "Не удалось создать отвод между воздуховодами.");
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Произошла ошибка: {ex.Message}");
            }
        }

        public static void DrawCableTray(Document doc, Reference selectedReference,
            XYZ selectedPoint, Connector closestConnector1, XYZ start, XYZ end, ElementId levelId)
        {
            try
            {
                if (doc.GetElement(selectedReference) is CableTray element)
                {
                    ElementId typeId = element.GetTypeId();
                    doc.GetElement(typeId);
                    double num1 = element.get_Parameter((BuiltInParameter)1140122L).AsDouble();
                    double num2 = element.get_Parameter((BuiltInParameter)1140121L).AsDouble();
                    CableTray newPipe = CableTray.Create(doc, typeId, start, end, levelId);
                    Connector[] cA = ConnectorArray(newPipe);
                    newPipe.get_Parameter((BuiltInParameter)1140122L).Set(num1);
                    newPipe.get_Parameter((BuiltInParameter)1140121L).Set(num2);
                    XYZ startPoint = selectedPoint;
                    Connector connector = NearestConnector(cA, startPoint);
                    FamilyInstance newElbow = doc.Create.NewElbowFitting(closestConnector1, connector);
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Произошла ошибка: {ex.Message}");
            }
        }

        private static void DrawPipeWithElbow(Document doc, ElementWrapper selectedElement,
            ConnectorWrapper closestConnector, XYZ end, bool includeElbow)
        {
            try
            {
                if (selectedElement.Element is not Pipe element) return;
                PipeType pipeType = element.PipeType;
                double diameter = element.Diameter;
                const double scalingFactor = 2; // Этот коэффициент можно настроить по необходимости

                // Вычисляем дополнительную длину на основе диаметра
                double extraLength = diameter * scalingFactor;

                // Вычисляем вектор направления от start до end
                XYZ direction = end - closestConnector.Origin;
                double length = direction.GetLength();

                // Проверяем, чтобы длина не была нулевой
                if (length == 0)
                {
                    // Можно обработать этот случай по-другому, если требуется
                    return;
                }

                // Нормализуем вектор направления
                XYZ unitDirection = direction.Normalize();

                // Определяем новую конечную точку, увеличив длину на дополнительную длину
                XYZ adjustedEnd = end + unitDirection * extraLength;
                PipingSystemType pipeSystem = GetPipeSystem(doc, closestConnector.Connector);
                ElementId level = GetLevel(doc, selectedElement.GetGlobalPoint().Z);
                Pipe newPipe = Pipe.Create(doc, pipeSystem.Id, pipeType.Id, level, closestConnector.Origin,
                    adjustedEnd);
                newPipe.LookupParameter("Диаметр").Set(diameter);
                Connector[] connectors = ConnectorArray(newPipe);
                if (!includeElbow) return;
                Connector connector = NearestConnector(connectors, selectedElement.GetGlobalPoint());
                FamilyInstance newElbow = doc.Create.NewElbowFitting(closestConnector.Connector, connector);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", ex.Message);
            }
        }

        public static Connector ClosestConnector(Connector[] cA1, Connector[] cA2)
        {
            Connector connector1 = null;
            double num1 = double.MaxValue;
            int index = 0;
            foreach (Connector connector2 in cA1)
            {
                if (cA1[index].Origin != cA2[0].Origin)
                {
                    double num2 = Math.Sqrt(Math.Pow(cA1[index].Origin.X - cA2[0].Origin.X, 2.0) +
                                            Math.Pow(cA1[index].Origin.Y - cA2[0].Origin.Y, 2.0) +
                                            Math.Pow(cA1[index].Origin.Z - cA2[0].Origin.Z, 2.0));
                    if (num2 < num1)
                    {
                        num1 = num2;
                        connector1 = cA1[index];
                    }
                }

                ++index;
            }

            return connector1;
        }

        public static void AlignIntersectingMepElements(Element movingElement,
            Connector stationaryElementClosestConnector, Connector movingElementClosestConnector,
            Connector stationaryElementFarthestConnector,
            Connector movingElementFarthestConnector, Document doc)
        {
            XYZ basisZ = stationaryElementClosestConnector.CoordinateSystem.BasisZ;
            XYZ origin1 = stationaryElementClosestConnector.Origin;
            XYZ origin2 = movingElementClosestConnector.Origin;
            XYZ pX = IntersectionTwoVectors(movingElementFarthestConnector.Origin, movingElementClosestConnector.Origin,
                stationaryElementFarthestConnector.Origin, stationaryElementClosestConnector.Origin);
            XYZ p0 = PerpIntersection(origin1, origin1 + basisZ, pX);
            XYZ xyz = PerpIntersection(p0, p0 + movingElementFarthestConnector.CoordinateSystem.BasisZ, origin2) -
                      origin2;
            if (xyz.GetLength() == 0.0)
                return;
            ElementTransformUtils.MoveElement(doc, movingElement.Id, xyz);
        }

        public static XYZ IntersectionTwoVectors(XYZ upstreamBranch, XYZ downstreamBranch, XYZ downstreamMain,
            XYZ upstreamMain)
        {
            XYZ xyz1 = downstreamMain;
            XYZ xyz2 = downstreamBranch;
            XYZ xyz3 = new XYZ(upstreamMain.X - downstreamMain.X, upstreamMain.Y - downstreamMain.Y,
                upstreamMain.Z - downstreamMain.Z);
            XYZ xyz4 = new XYZ(upstreamBranch.X - downstreamBranch.X, upstreamBranch.Y - downstreamBranch.Y,
                upstreamBranch.Z - downstreamBranch.Z);
            XYZ xyz5 = new XYZ(downstreamMain.X - downstreamBranch.X, downstreamMain.Y - downstreamBranch.Y,
                downstreamMain.Z - downstreamBranch.Z);
            double num1 = xyz3.DotProduct(xyz3);
            double num2 = xyz3.DotProduct(xyz4);
            double num3 = xyz4.DotProduct(xyz4);
            double num4 = xyz3.DotProduct(xyz5);
            double num5 = xyz4.DotProduct(xyz5);
            double num6 = num1 * num3 - num2 * num2;
            double num7;
            double num8;
            if (num6 < 1E-08)
            {
                num7 = 0.0;
                num8 = num2 <= num3 ? num5 / num3 : num4 / num2;
            }
            else
            {
                num7 = (num2 * num5 - num3 * num4) / num6;
                num8 = (num1 * num5 - num2 * num4) / num6;
            }

            XYZ xyz6 = new XYZ(xyz1.X + num7 * xyz3.X, xyz1.Y + num7 * xyz3.Y, xyz1.Z + num7 * xyz3.Z);
            XYZ xyz7 = new XYZ(xyz2.X + num8 * xyz4.X, xyz2.Y + num8 * xyz4.Y, xyz2.Z + num8 * xyz4.Z);
            return new XYZ((xyz7.X - xyz6.X) / 2.0 + xyz6.X, (xyz7.Y - xyz6.Y) / 2.0 + xyz6.Y,
                (xyz7.Z - xyz6.Z) / 2.0 + xyz6.Z);
        }

        private static void Bloom(Document doc, ElementWrapper selectedElement, MEPCurveType elementType)
        {
            List<ConnectorWrapper> connectorUnused = selectedElement.Connectors
                .Where(x => !x.IsConnected)
                .ToList();
            if (connectorUnused.Count == 0)

                return;
            foreach (ConnectorWrapper connector in connectorUnused)
            {
                if (connector.Domain is not (Domain.DomainPiping or Domain.DomainHvac)) continue;
                switch (connector.Domain)
                {
                    case Domain.DomainPiping:
                    {
                        PipingSystemType pipeSystem = GetPipeSystem(doc, connector.Connector);
                        double extensionLength = GetExtensionLength(connector.Connector);

                        XYZ xyz1 = connector.Origin + extensionLength * connector.CoordinateSystem.BasisZ;
                        ElementId level = GetLevel(doc, connector.Origin.Z);
                        if (elementType != null)
                        {
                            CreatePipe(doc, pipeSystem, elementType as PipeType, connector, level, xyz1);
                        }

                        break;
                    }
                    case Domain.DomainHvac:
                    {
                        MechanicalSystemType mechanicalSystem = GetMechanicalSystem(doc, connector.Connector);
                        double extensionLength = GetExtensionLength(connector.Connector);
                        XYZ endPoint = connector.Origin + extensionLength * connector.CoordinateSystem.BasisZ;
                        ElementId level = GetLevel(doc, connector.Origin.Z);
                        CreateDuct(doc, mechanicalSystem, elementType as DuctType, connector, level, endPoint);
                        break;
                    }
                }
            }
        }


        public static DuctType DeterminingTypeOfDuctByFitting(Document doc, Element element)
        {
            ConnectorSet connectors = (element as FamilyInstance)?.MEPModel.ConnectorManager.Connectors;
            if (connectors != null)
            {
                foreach (Connector connector in connectors)
                {
                    // Перебираем соединения
                    foreach (Connector connectedConnector in connector.AllRefs)
                    {
                        if (connectedConnector.Owner is Duct connectedPipe)
                        {
                            // Получаем тип присоединенной трубы
                            return doc.GetElement(connectedPipe.GetTypeId()) as DuctType;
                        }
                    }
                }
            }

            return null;
        }

        private static void CreatePipe(Document doc, PipingSystemType pipeSystem, PipeType pipeType,
            ConnectorWrapper connector, ElementId level, XYZ xyz1)
        {
            Element pipe = Pipe.Create(doc, pipeSystem.Id, pipeType.Id, level, connector.Origin, xyz1);
            Parameter diameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            var diameterConnectorSelected = connector.Radius * 2;
            diameter.Set(diameterConnectorSelected);
            Connector[] cA = ConnectorArrayUnused(pipe);
            NearestConnector(cA, connector.Origin).ConnectTo(connector.Connector);
        }

        private static void CreateDuct(Document doc, MechanicalSystemType mechanicalSystemType, DuctType ductType,
            ConnectorWrapper connector, ElementId level, XYZ endPoint)
        {
            Element duct = Duct.Create(doc, mechanicalSystemType.Id, ductType.Id, level, connector.Origin, endPoint);
            switch (connector.Shape)
            {
                case ConnectorProfileType.Round:
                {
                    Parameter diameter = duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                    var diameterConnectorSelected = connector.Radius * 2;
                    diameter.Set(diameterConnectorSelected);
                    break;
                }
                case ConnectorProfileType.Rectangular:
                {
                    Parameter width = duct.FindParameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
                    Parameter height = duct.FindParameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
                    ElementWrapper elem = new ElementWrapper(connector.Owner);
                    var c = elem.Connectors.FirstOrDefault(x => x.Id != connector.Id);
                    bool shouldSwapDimensions = false;
                    if (IsConnectorsHorizontal(elem.Connectors))
                    {
                        shouldSwapDimensions = false;
                    }
                    // Проверка ориентации коннектора по векторам
                    else if (IsVerticalUpward(connector)) 
                    {
                        shouldSwapDimensions = false;
                    }
                    else if (IsVerticalDownward(connector)) // Вертикально вниз
                    {
                        shouldSwapDimensions = false;
                    }
                    else if (IsHorizontalRight(connector)) // Горизонтально вправо
                    {
                        shouldSwapDimensions = true;
                    }
                    else if (IsHorizontalLeft(connector)) // Горизонтально влево
                    {
                        shouldSwapDimensions = false;
                    }
                    else if (IsForward(connector)) // Вперед (от наблюдателя)
                    {
                        shouldSwapDimensions = true;
                    }
                    else if (IsBackward(connector)) // Назад (к наблюдателю)
                    {
                        shouldSwapDimensions = false;
                    }
                    else if (IsPositiveInclinedUpward(connector)) // Наклон вверх положительный
                    {
                        shouldSwapDimensions = false;
                    }
                    else if (IsNegativeInclinedUpward(connector)) // Наклон вверх отрицательный
                    {
                        shouldSwapDimensions = false;
                    }
                    else if (IsPositiveInclinedDownward(connector)) // Наклон вниз положительный
                    {
                        shouldSwapDimensions = true;
                    }
                    else if (IsNegativeInclinedDownward(connector)) // Наклон вниз отрицательный
                    {
                        shouldSwapDimensions = true;
                    }
                    else
                    {
                        shouldSwapDimensions = true;
                    }

                    // Установка размеров с учетом необходимости их замены
                    if (shouldSwapDimensions)
                    {
                        width?.Set(connector.Height);
                        height?.Set(connector.Width);
                    }
                    else
                    {
                        width?.Set(connector.Width);
                        height?.Set(connector.Height);
                    }

                    break;
                }
                case ConnectorProfileType.Oval:
                    return;
            }

            // if (Math.Abs(connector.Origin.X - endPoint.X) < 0.001 &&
            //     Math.Abs(connector.Origin.Y - endPoint.Y) < 0.001)
            // {
            //     XYZ basisZ = connector.CoordinateSystem.BasisZ;
            //     XYZ source = new XYZ(0.0, 1.0, 0.0);
            //     double angle = basisZ.AngleTo(source);
            //     if (basisZ.DotProduct(source) > 0.0 && basisZ.X > 1.0)
            //         angle = -angle;
            //     Line bound = Line.CreateBound(connector.Origin, endPoint);
            //     if (connector.Origin.Z > endPoint.Z)
            //         bound = Line.CreateBound(endPoint, connector.Origin);
            //     ElementTransformUtils.RotateElement(doc, duct.Id, bound, angle);
            // }
            Connector[] cA = ConnectorArrayUnused(duct);
            NearestConnector(cA, connector.Origin).ConnectTo(connector.Connector);
        }

        private static bool IsConnectorsHorizontal(List<ConnectorWrapper> elemConnectors)
        {
            const double epsilon = 1.0e-9;

            foreach (var connector in elemConnectors)
            {
                // Если Z-компонента направления близка к 1 или -1, значит коннектор вертикальный
                if (Math.Abs(connector.BasisZ.Z) != 0)
                {
                    return false; // Нашли вертикальный коннектор
                }
            }

            return true; // Все коннекторы горизонтальные
        }

        // Вспомогательные методы для проверки ориентации
        private static bool IsVerticalUpward(ConnectorWrapper connector)
        {
            return Math.Abs(connector.BasisZ.Z - 1) < Constants.Epsilon;
        }

        private static bool IsVerticalDownward(ConnectorWrapper connector)
        {
            return Math.Abs(connector.BasisZ.Z + 1) < Constants.Epsilon;
        }

        private static bool IsHorizontalRight(ConnectorWrapper connector)
        {
            return Math.Abs(connector.BasisZ.X - 1) < Constants.Epsilon;
        }

        private static bool IsHorizontalLeft(ConnectorWrapper connector)
        {
            return Math.Abs(connector.BasisZ.X + 1) < Constants.Epsilon;
        }

        private static bool IsForward(ConnectorWrapper connector)
        {
            return Math.Abs(connector.BasisZ.Y - 1) < Constants.Epsilon;
        }

        private static bool IsBackward(ConnectorWrapper connector)
        {
            return Math.Abs(connector.BasisZ.Y + 1) < Constants.Epsilon;
        }

        private static bool IsPositiveInclinedUpward(ConnectorWrapper connector)
        {
            return connector.BasisZ.X > 0 &&
                   connector.BasisZ.Y > 0 &&
                   connector.BasisZ.Z > 0 &&
                   connector.BasisY.Y > 0;
        }

        private static bool IsNegativeInclinedUpward(ConnectorWrapper connector)
        {
            return connector.BasisZ.X > 0 &&
                   connector.BasisZ.Y > 0 &&
                   connector.BasisZ.Z > 0 &&
                   connector.BasisY.Y < 0;
        }

        private static bool IsPositiveInclinedDownward(ConnectorWrapper connector)
        {
            return connector.BasisZ.X > 0 &&
                   connector.BasisZ.Y > 0 &&
                   connector.BasisZ.Z < 0 &&
                   connector.BasisY.Y > 0;
        }

        private static bool IsNegativeInclinedDownward(ConnectorWrapper connector)
        {
            return connector.BasisZ.X > 0 &&
                   connector.BasisZ.Y > 0 &&
                   connector.BasisZ.Z < 0 &&
                   connector.BasisY.Y < 0;
        }

        private struct Constants
        {
            public const double Epsilon = 1.0e-9;
        }

        public static void AlignColinearMEPElements(Element movingElement, Connector stationaryElementClosestConnector,
            Connector movingElementClosestConnector, Document doc)
        {
            XYZ origin1 = stationaryElementClosestConnector.Origin;
            XYZ origin2 = movingElementClosestConnector.Origin;
            XYZ xyz1 = PerpIntersection(origin1, origin1 + stationaryElementClosestConnector.CoordinateSystem.BasisZ,
                origin2);
            XYZ xyz2 = xyz1 - origin2;
            if (!xyz2.IsZeroLength())
                ElementTransformUtils.MoveElement(doc, movingElement.Id, xyz2);
            XYZ basisZ1 = stationaryElementClosestConnector.CoordinateSystem.BasisZ;
            XYZ basisZ2 = movingElementClosestConnector.CoordinateSystem.BasisZ;
            double num1 = basisZ1.DotProduct(basisZ2);
            double num2 = -1.0;
            if (Math.Round(num1, 10) == Math.Round(num2, 10))
                return;
            XYZ xyz3 = basisZ1.CrossProduct(basisZ2);
            double num3 = 1.0;
            if (Math.Round(num1, 10) == Math.Round(num3, 10))
                xyz3 = stationaryElementClosestConnector.CoordinateSystem.BasisY;
            Line bound = Line.CreateBound(xyz1, xyz1 + 1E+16 * xyz3);
            double num4 = Math.PI - basisZ1.AngleTo(basisZ2);
            ElementTransformUtils.RotateElement(doc, movingElement.Id, bound, num4);
        }

        public static XYZ PerpIntersection(XYZ p0, XYZ p1, XYZ pX)
        {
            double num1 = (pX.X - p0.X) * (p1.X - p0.X) + (pX.Y - p0.Y) * (p1.Y - p0.Y) + (pX.Z - p0.Z) * (p1.Z - p0.Z);
            double num2 = p0.DistanceTo(p1);
            double num3 = num1 / (num2 * num2);
            return new XYZ(p0.X + num3 * (p1.X - p0.X), p0.Y + num3 * (p1.Y - p0.Y), p0.Z + num3 * (p1.Z - p0.Z));
        }

        public static void WriteJournalData(ExternalCommandData commandData)
        {
            IDictionary<string, string> journalData = commandData.JournalData;
            journalData.Clear();
            journalData.Add("Name", "Autodesk.Revit");
            journalData.Add("Information", "This is an example.");
            journalData.Add("Greeting", "Hello Everyone.");
        }

        public static Connector[] ClosestConnectors(Element element1, Element element2, bool align)
        {
            Connector[] connectorArray = new Connector[2];
            Connector[] connectorArray1 = ConnectorArrayUnused(element1);
            if (connectorArray1 == null)
                return null;
            Connector[] connectorArray2 = ConnectorArrayUnused(element2);
            if (connectorArray2 == null)
                return null;
            Connector c = !OnlyOneDomain(connectorArray2)
                ? ClosestAvailableConnector(connectorArray1, connectorArray2)
                : ClosestConnectorOfDomain(connectorArray1, connectorArray2, connectorArray2[0].Domain);
            if (c == null)
                return null;
            Connector connector = !align
                ? ClosestConnectorOfDomainAndAngle(connectorArray2, c)
                : ClosestConnectorOfDomain(connectorArray2, connectorArray1, c.Domain);
            if (connector == null)
                return null;
            connectorArray[0] = c;
            connectorArray[1] = connector;
            return connectorArray;
        }

        public static Connector ClosestConnectorOfDomain(Connector[] cA1, Connector[] cA2, Domain domain)
        {
            Connector connector1 = null;
            double num1 = double.MaxValue;
            int index = 0;
            foreach (Connector connector2 in cA1)
            {
                if (connector2.IsConnected)
                {
                    ++index;
                }
                else
                {
                    if (cA1[index].Origin != cA2[0].Origin)
                    {
                        double num2 = Math.Sqrt(Math.Pow(cA1[index].Origin.X - cA2[0].Origin.X, 2.0) +
                                                Math.Pow(cA1[index].Origin.Y - cA2[0].Origin.Y, 2.0) +
                                                Math.Pow(cA1[index].Origin.Z - cA2[0].Origin.Z, 2.0));
                        if (num2 < num1 && cA1[index].Domain == domain)
                        {
                            num1 = num2;
                            connector1 = cA1[index];
                        }
                    }

                    ++index;
                }
            }

            return connector1;
        }

        public static Connector ClosestConnectorOfDomainAndAngle(Connector[] cA1, Connector c)
        {
            Connector connector1 = null;
            double num1 = double.MaxValue;
            int index = 0;
            foreach (Connector connector2 in cA1)
            {
                if (connector2.IsConnected)
                {
                    ++index;
                }
                else
                {
                    if (cA1[index].Origin != c.Origin)
                    {
                        double num2 = Math.Sqrt(Math.Pow(cA1[index].Origin.X - c.Origin.X, 2.0) +
                                                Math.Pow(cA1[index].Origin.Y - c.Origin.Y, 2.0) +
                                                Math.Pow(cA1[index].Origin.Z - c.Origin.Z, 2.0));
                        if (num2 < num1 && cA1[index].Domain == c.Domain &&
                            c.CoordinateSystem.BasisZ.DotProduct(cA1[index].CoordinateSystem.BasisZ) < -0.9)
                        {
                            num1 = num2;
                            connector1 = cA1[index];
                        }
                    }

                    ++index;
                }
            }

            return connector1;
        }

        public static Connector ClosestAvailableConnector(Connector[] cA1, Connector[] cA2)
        {
            Connector connector1 = null;
            double num1 = double.MaxValue;
            int index = 0;
            foreach (Connector connector2 in cA1)
            {
                if (connector2.IsConnected)
                {
                    ++index;
                }
                else
                {
                    if (cA1[index].Origin == cA2[0].Origin)
                        return connector2;
                    double num2 = Math.Sqrt(Math.Pow(cA1[index].Origin.X - cA2[0].Origin.X, 2.0) +
                                            Math.Pow(cA1[index].Origin.Y - cA2[0].Origin.Y, 2.0) +
                                            Math.Pow(cA1[index].Origin.Z - cA2[0].Origin.Z, 2.0));
                    if (num2 < num1)
                    {
                        num1 = num2;
                        connector1 = cA1[index];
                    }

                    ++index;
                }
            }

            return connector1;
        }

        public static bool OnlyOneDomain(Connector[] cA)
        {
            if (cA.Count() == 1)
                return true;
            List<string> source = new List<string>();
            foreach (Connector connector in cA)
                source.Add(connector.Domain.ToString());
            return source.Distinct().Count() == 1;
        }

        /// <summary>
        /// Находит и возвращает коннектор из заданного массива, который находится дальше всего от указанной точки.
        /// </summary>
        /// <param name="connectors">Массив коннекторов для поиска.</param>
        /// <param name="startPoint">Точка, от которой измеряется расстояние.</param>
        /// <returns>
        /// Коннектор, наиболее удалённый от точки <paramref name="startPoint"/>.
        /// Возвращает <c>null</c>, если массив коннекторов пустой или равен <c>null</c>.
        /// </returns>
        public static Connector FarthestConnector(Connector[] connectors, XYZ startPoint)
        {
            // Проверяем, что входные данные не равны null
            if (connectors == null || connectors.Length == 0 || startPoint == null)
                return null;
            Connector farthestConnector = null;
            double maxDistance = double.MinValue;
            foreach (Connector connector in connectors)
            {
                // Вычисляем расстояние между коннектором и заданной точкой
                double distance = connector.Origin.DistanceTo(startPoint);
                // Если это расстояние больше текущего максимального, обновляем значения
                if (!(distance > maxDistance)) continue;
                maxDistance = distance;
                farthestConnector = connector;
            }

            return farthestConnector;
        }


        /// <summary>
        /// Находит и возвращает коннектор из заданного массива, который находится ближе всего к указанной точке.
        /// </summary>
        /// <param name="connectors">Массив коннекторов для поиска.</param>
        /// <param name="startPoint">Точка, к которой ищется ближайший коннектор.</param>
        /// <returns>
        /// Ближайший к заданной точке коннектор.
        /// Если массив коннекторов пустой или равен null, возвращает null.
        /// </returns>
        public static Connector NearestConnector(Connector[] connectors, XYZ startPoint)
        {
            if (connectors == null)
                return null;
            if (connectors.Length == 1)
                return connectors[0];
            Connector connector = null;
            double num1 = double.MaxValue;
            for (int index = 0; index < connectors.Count(); ++index)
            {
                double num2 = connectors[index].Origin.DistanceTo(startPoint);
                if (!(num2 < num1)) continue;
                num1 = num2;
                connector = connectors[index];
            }

            return connector;
        }

        private static MechanicalSystemType GetMechanicalSystem(Document doc, Connector connector)
        {
            if (connector.Domain != Domain.DomainHvac)
                return null;
            try
            {
                ElementId typeId = (connector.MEPSystem as MechanicalSystem)?.GetTypeId();
                return doc.GetElement(typeId) as MechanicalSystemType;
            }
            catch
            {
                // ignored
            }

            FilteredElementCollector source = new FilteredElementCollector(doc).OfClass(typeof(MechanicalSystemType));
            connector.DuctSystemType.ToString();
            if (connector.DuctSystemType == (DuctSystemType.UndefinedSystemType) ||
                connector.DuctSystemType == DuctSystemType.Fitting || connector.DuctSystemType == DuctSystemType.Global)
                return source.First() as MechanicalSystemType;
            if (connector.DuctSystemType == (DuctSystemType.SupplyAir))
            {
                foreach (var element in source)
                {
                    var mechanicalSystem = (MechanicalSystemType)element;
                    string str = mechanicalSystem.get_Parameter((BuiltInParameter)1140325L).AsString();
                    if (str.Contains("Supply Air") || str.Contains("Zuluft") || str.Contains("Soufflage") ||
                        str.Contains("Aria di mandata") || str.Contains("給気") || str.Contains("Powietrze nawiewane") ||
                        str.Contains("Приточный воздух") || str.Contains("공급 공기") ||
                        str.Contains("Suministro de aire") || str.Contains("Suprimento de ar") || str.Contains("送风") ||
                        str.Contains("進氣") || str.Contains("Přívod vzduchu"))
                        return mechanicalSystem;
                }
            }
            else if (connector.DuctSystemType == (DuctSystemType.ReturnAir))
            {
                foreach (var element in source)
                {
                    var mechanicalSystem = (MechanicalSystemType)element;
                    string str = mechanicalSystem.get_Parameter((BuiltInParameter)1140325L).AsString();
                    if (str.Contains("Return Air") || str.Contains("Umluft") || str.Contains("Reprise") ||
                        str.Contains("Aria di ritorno") || str.Contains("還気") ||
                        str.Contains("Powietrze recyrkulac.") || str.Contains("Рециркулирующий воздух") ||
                        str.Contains("순환 공기") || str.Contains("Aire de retorno") || str.Contains("Ar de retorno") ||
                        str.Contains("回风") || str.Contains("回氣") || str.Contains("Zpětný vzduch"))
                        return mechanicalSystem;
                }
            }
            else if (connector.DuctSystemType == (DuctSystemType.ExhaustAir))
            {
                foreach (var element in source)
                {
                    var mechanicalSystem = (MechanicalSystemType)element;
                    string str = mechanicalSystem.get_Parameter((BuiltInParameter)1140325L).AsString();
                    if (str.Contains("Exhaust Air") || str.Contains("Abluft") || str.Contains("Extraction d'air") ||
                        str.Contains("Aria di scarico") || str.Contains("排気") || str.Contains("Powietrze zwracane") ||
                        str.Contains("Отработанный воздух") || str.Contains("배기") || str.Contains("Aire viciado") ||
                        str.Contains("Ar de exaustão") || str.Contains("排风") || str.Contains("排出氣") ||
                        str.Contains("Odváděný vzduch"))
                        return mechanicalSystem;
                }
            }

            return source.First() as MechanicalSystemType;
        }

        /// <summary>
        /// Получает ElementId уровня, соответствующего заданной высоте.
        /// </summary>
        /// <param name="doc">Документ Revit.</param>
        /// <param name="z">Высота (в внутренних единицах), для которой требуется найти уровень.</param>
        /// <returns>
        /// ElementId уровня, находящегося на высоте, равной или непосредственно ниже заданной.
        /// Если таких уровней нет, возвращается ближайший уровень выше заданной высоты.
        /// </returns>
        public static ElementId GetLevel(Document doc, double z)
        {
            // Проверяем, имеет ли активный вид связанный уровень
            if (doc.ActiveView?.GenLevel != null)
            {
                return doc.ActiveView.GenLevel.Id;
            }

            // Получаем все уровни в документе
            FilteredElementCollector levelCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(Level));
            List<Level> levels = levelCollector.Cast<Level>().ToList();

            if (levels.Count == 0)
            {
                throw new InvalidOperationException("В документе не найдено ни одного уровня.");
            }

            // Ищем уровень на высоте, равной или непосредственно ниже 'z'
            Level closestLevel = levels
                .Where(lvl => lvl.Elevation <= z)
                .OrderByDescending(lvl => lvl.Elevation)
                // Если не найден уровень ниже 'z', берем ближайший уровень выше
                .FirstOrDefault() ?? levels
                .OrderBy(lvl => lvl.Elevation - z)
                .FirstOrDefault();

            return closestLevel?.Id;
        }

        /// <summary>
        /// Вычисляет и возвращает длину удлинения для заданного коннектора на основе его домена и формы.
        /// </summary>
        /// <param name="connector">Коннектор, для которого необходимо определить длину удлинения.</param>
        /// <returns>
        /// Длина удлинения для коннектора.
        /// Значение зависит от домена и формы коннектора и используется для корректного расположения элементов систем.
        /// </returns>
        private static double GetExtensionLength(Connector connector)
        {
            double extensionLength = connector.Domain switch
            {
                Domain.DomainHvac when connector.Shape == ConnectorProfileType.Rectangular => 2.0 * connector.Width,
                Domain.DomainHvac when connector.Shape == ConnectorProfileType.Round => 3.0 * connector.Radius,
                Domain.DomainHvac when connector.Shape == ConnectorProfileType.Oval => 2.0 * connector.Width,
                Domain.DomainCableTrayConduit => 3.0,
                Domain.DomainPiping => 4.0 * connector.Radius,
                _ => 1.0
            };
            return extensionLength;
        }

        private static PipingSystemType GetPipeSystem(Document doc, Connector connector)
        {
            if (connector.Domain != Domain.DomainPiping)
                return null;
            if (connector.MEPSystem != null)
            {
                ElementId typeId = (connector.MEPSystem as PipingSystem)?.GetTypeId();
                return doc.GetElement(typeId) as PipingSystemType;
            }

            var defaultPipingSystemType = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .FirstOrDefault(x => x.Name == "Прочее");
            return defaultPipingSystemType as PipingSystemType;
        }

        public static Connector[] ConnectorArrayUnused(Element element)
        {
            return element switch
            {
                // Проверяем, поддерживает ли элемент MEPModel и ConnectorManager
                MEPCurve mepCurve => mepCurve.ConnectorManager.UnusedConnectors.Cast<Connector>().ToArray(),
                FamilyInstance { MEPModel: not null } familyInstance => familyInstance.MEPModel.ConnectorManager
                    .UnusedConnectors.Cast<Connector>()
                    .ToArray(),
                _ => null
            };
        }

        public static Connector[] ConnectorArray(Element element)
        {
            if (element == null)
                return null;
            ConnectorSet connectorSet = null;
            if (element is FamilyInstance familyInstance && familyInstance.MEPModel != null)
                connectorSet = familyInstance.MEPModel.ConnectorManager.Connectors;
            if (element is MEPCurve mepCurve)
                connectorSet = mepCurve.ConnectorManager.Connectors;
            if (element is FabricationPart fabricationPart)
                connectorSet = fabricationPart.ConnectorManager.Connectors;
            if (connectorSet == null)
                return null;
            List<Connector> connectorList = new List<Connector>();
            foreach (Connector connector in connectorSet)
            {
                if (connector.Domain != (Domain)2)
                    connectorList.Add(connector);
            }

            return connectorList.ToArray();
        }
    }
}