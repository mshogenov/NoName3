using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using SystemModelingCommands.Filters;
using SystemModelingCommands.Model;
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
            Element selectedElement = null;
            // Проверка, есть ли выбранный элемент до запуска скрипта
            var selectedIds = Context.ActiveUiDocument?.Selection.GetElementIds();
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
                            selectedElement = preSelectedElement;
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
                    selectedElement = _doc?.GetElement(reference);
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return; // Пользователь отменил выбор
                }
            }

            MEPCurveType pipeType = DeterminingTypeOfPipeByFitting(_doc, selectedElement);
            if (pipeType == null)
            {
                BloomView view = new BloomView(_doc, selectedElement);
                view.ShowDialog();
                if (view.MepCurveType != null)
                {
                    pipeType = view.MepCurveType;
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
                Bloom(_doc, selectedElement, pipeType);
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
            Element element = _doc.GetElement(selectedReference);

            XYZ globalPoint = selectedReference.GlobalPoint;
            Connector[] cA = ConnectorArrayUnused(element);
            if (cA == null)
                return;
            Connector connector = NearestConnector(cA, globalPoint);
            if (connector.IsConnected)
                connector = FarthestConnector(cA, globalPoint);
            if (connector.IsConnected)
                return;
            ElementId level = GetLevel(_doc, connector.Origin.Z);
            if (level == null)
                return;
            XYZ origin = connector.Origin;
            XYZ xyz = new XYZ(0.0, 0.0, 0.0);
            XYZ basisZ = connector.CoordinateSystem.BasisZ;
            double extensionLength = GetExtensionLength(connector);
            XYZ end = basisZ.X != 0.0 || basisZ.Y != 0.0 || basisZ.Z <= 0.0
                ? basisZ.X != 0.0 || basisZ.Y != 0.0 || basisZ.Z >= 0.0
                    ? new XYZ(origin.X, origin.Y, origin.Z + extensionLength)
                    : new XYZ(origin.X, origin.Y + extensionLength, origin.Z)
                : new XYZ(origin.X, origin.Y - extensionLength, origin.Z);
            Transaction transaction = new Transaction(_doc, "Поворот вверх");
            try
            {
                transaction.Start();

                if (element != null && element.Category.Id.Value == -2008130)
                    DrawCableTray(_doc, selectedReference, globalPoint, connector, origin, end,
                        level);
                else if (element != null && element.Category.Id.Value == -2008132)
                    DrawConduit(_doc, selectedReference, globalPoint, connector, origin, end,
                        level);
                else if (element != null && element.Category.Id.Value == -2008000)
                    DrawDuct(_doc, selectedReference, globalPoint, connector, origin, end);
                else if (element != null && element.Category.Id.Value == -2008044)
                    DrawPipeWithElbow(_doc, selectedReference, globalPoint, connector, origin,
                        end, true);
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
            Element element = _doc.GetElement(selectedReference);
            XYZ globalPoint = selectedReference.GlobalPoint;
            Connector[] cA = ConnectorArrayUnused(element);
            if (cA == null)
                return;
            Connector connector = NearestConnector(cA, globalPoint);
            if (connector.IsConnected)
                connector = FarthestConnector(cA, globalPoint);
            if (connector.IsConnected)
                return;
            ElementId level = GetLevel(_doc, connector.Origin.Z);
            if (level == null)
                return;
            XYZ basisZ = connector.CoordinateSystem.BasisZ;
            double num1 = 1.0 * basisZ.X;
            double num2 = 1.0 * basisZ.Y;
            XYZ origin = connector.Origin;
            double extensionLength = GetExtensionLength(connector);
            XYZ end = Math.Round(basisZ.X, 3) != 0.0 || Math.Round(basisZ.Y, 3) != 0.0 || basisZ.Z <= 0.0
                ? Math.Round(basisZ.X, 3) != 0.0 || Math.Round(basisZ.Y, 3) != 0.0 || basisZ.Z >= 0.0
                    ? new XYZ(origin.X + extensionLength * num2, origin.Y - extensionLength * num1,
                        origin.Z + basisZ.Z * extensionLength)
                    : new XYZ(origin.X + extensionLength, origin.Y, origin.Z)
                : new XYZ(origin.X - extensionLength, origin.Y, origin.Z);
            Transaction transaction = new Transaction(_doc, "Поворот вправо");
            try
            {
                transaction.Start();

                if (element != null && element.Category.Id.Value == -2008130)
                    DrawCableTray(_doc, selectedReference, globalPoint, connector, origin, end,
                        level);
                else if (element != null && element.Category.Id.Value == -2008132)
                    DrawConduit(_doc, selectedReference, globalPoint, connector, origin, end,
                        level);
                else if (element != null && element.Category.Id.Value == -2008000)
                    DrawDuct(_doc, selectedReference, globalPoint, connector, origin, end);
                else if (element != null && element.Category.Id.Value == -2008044)
                    DrawPipeWithElbow(_doc, selectedReference, globalPoint, connector, origin,
                        end, true);
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
            Element element = _doc.GetElement(selectedReference);
            XYZ globalPoint = selectedReference.GlobalPoint;
            Connector[] cA = ConnectorArrayUnused(element);
            if (cA == null)
                return;
            Connector connector = NearestConnector(cA, globalPoint);
            if (connector.IsConnected)
                connector = FarthestConnector(cA, globalPoint);
            if (connector.IsConnected)
                return;
            ElementId level = GetLevel(_doc, connector.Origin.Z);
            if (level == null)
                return;
            XYZ basisZ = connector.CoordinateSystem.BasisZ;
            double num1 = 1.0 * basisZ.X;
            double num2 = 1.0 * basisZ.Y;
            XYZ origin = connector.Origin;
            double extensionLength = GetExtensionLength(connector);
            XYZ end = Math.Round(basisZ.X, 3) != 0.0 || Math.Round(basisZ.Y, 3) != 0.0 || basisZ.Z <= 0.0
                ? Math.Round(basisZ.X, 3) != 0.0 || Math.Round(basisZ.Y, 3) != 0.0 || basisZ.Z >= 0.0
                    ? new XYZ(origin.X - extensionLength * num2, origin.Y + extensionLength * num1,
                        origin.Z + basisZ.Z * extensionLength)
                    : new XYZ(origin.X - extensionLength, origin.Y, origin.Z)
                : new XYZ(origin.X + extensionLength, origin.Y, origin.Z);
            Transaction transaction = new Transaction(_doc, "Поворот влево");
            try
            {
                transaction.Start();

                if (element != null && element.Category.Id.Value == -2008130)
                    DrawCableTray(_doc, selectedReference, globalPoint, connector, origin, end, level);
                else if (element != null && element.Category.Id.Value == -2008132)
                    DrawConduit(_doc, selectedReference, globalPoint, connector, origin, end, level);
                else if (element != null && element.Category.Id.Value == -2008000)
                    DrawDuct(_doc, selectedReference, globalPoint, connector, origin, end);
                else if (element != null && element.Category.Id.Value == -2008044)
                    DrawPipeWithElbow(_doc, selectedReference, globalPoint, connector, origin, end, true);
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
            Element element = _doc.GetElement(selectedReference);
            XYZ globalPoint = selectedReference.GlobalPoint;
            Connector[] cA = ConnectorArrayUnused(element);
            if (cA == null)
                return;
            Connector connector = NearestConnector(cA, globalPoint);
            if (connector.IsConnected || connector.CoordinateSystem.BasisZ.X == 0.0 &&
                connector.CoordinateSystem.BasisZ.Y == 0.0 && connector.CoordinateSystem.BasisZ.Z > 0.0)
                connector = FarthestConnector(cA, globalPoint);
            if (connector.IsConnected)
                return;
            XYZ origin = connector.Origin;
            XYZ basisZ = connector.CoordinateSystem.BasisZ;
            ElementId level = GetLevel(_doc, connector.Origin.Z);
            if (level == null)
                return;
            double extensionLength = GetExtensionLength(connector);
            XYZ end = basisZ.X != 0.0 || basisZ.Y != 0.0 || basisZ.Z >= 0.0
                ? new XYZ(origin.X + basisZ.X * extensionLength, origin.Y + basisZ.Y * extensionLength,
                    origin.Z - extensionLength)
                : new XYZ(origin.X, origin.Y + extensionLength, origin.Z - extensionLength);
            Transaction transaction = new Transaction(_doc, "Поворот вниз на 45°");
            try
            {
                transaction.Start();

                if (element != null && element.Category.BuiltInCategory == BuiltInCategory.OST_Conduit)
                    DrawConduit(_doc, selectedReference, globalPoint, connector, origin, end,
                        level);
                else if (element != null && element.Category.BuiltInCategory == BuiltInCategory.OST_DuctCurves)
                    DrawDuct(_doc, selectedReference, globalPoint, connector, origin, end);
                else if (element != null && element.Category.BuiltInCategory == BuiltInCategory.OST_PipeCurves)
                    DrawPipeWithElbow(_doc, selectedReference, globalPoint, connector, origin,
                        end, true);

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

        public static void ElbowDown()
        {
            Reference selectedReference = GetSelectedReference();
            if (selectedReference == null) return;
            Element element = Context.ActiveDocument?.GetElement(selectedReference);
            XYZ globalPoint = selectedReference?.GlobalPoint;
            Connector[] connectors = ConnectorArrayUnused(element);
            if (connectors == null)
                return;
            Connector connector = NearestConnector(connectors, globalPoint);
            if (connector.IsConnected)
                connector = FarthestConnector(connectors, globalPoint);
            if (connector.IsConnected)
                return;
            XYZ origin = connector.Origin;
            XYZ basisZ = connector.CoordinateSystem.BasisZ;
            ElementId level = GetLevel(Context.ActiveDocument, connector.Origin.Z);
            if (level == null)
                return;
            double extensionLength = GetExtensionLength(connector);
            XYZ end = basisZ.X != 0.0 || basisZ.Y != 0.0 || basisZ.Z <= 0.0
                ? basisZ.X != 0.0 || basisZ.Y != 0.0 || basisZ.Z >= 0.0
                    ? new XYZ(origin.X, origin.Y, origin.Z - extensionLength)
                    : new XYZ(origin.X, origin.Y + extensionLength, origin.Z)
                : new XYZ(origin.X, origin.Y - extensionLength, origin.Z);
            Transaction transaction = new(Context.ActiveDocument, "Поворот вниз");
            try
            {
                transaction.Start();

                switch (element?.Category.BuiltInCategory)
                {
                    case BuiltInCategory.OST_CableTray:
                        DrawCableTray(Context.ActiveDocument, selectedReference, globalPoint,
                            connector, origin, end, level);
                        break;
                    case BuiltInCategory.OST_Conduit:
                        DrawConduit(Context.ActiveDocument, selectedReference, globalPoint,
                            connector, origin, end, level);
                        break;
                    case BuiltInCategory.OST_DuctCurves:
                        DrawDuct(Context.ActiveDocument, selectedReference, globalPoint,
                            connector,
                            origin, end);
                        break;
                    case BuiltInCategory.OST_PipeCurves:
                        DrawPipeWithElbow(Context.ActiveDocument, selectedReference, globalPoint,
                            connector, origin, end, true);
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
            Element element = _doc.GetElement(selectedReference);
            XYZ globalPoint = selectedReference.GlobalPoint;
            Connector[] cA = ConnectorArrayUnused(element);
            if (cA == null)
                return;
            // Находим ближайший коннектор
            Connector connector = NearestConnector(cA, globalPoint);

            if (connector == null || connector.IsConnected)
                return;
            XYZ origin = connector.Origin;
            XYZ basisZ = connector.CoordinateSystem.BasisZ;
            ElementId level = GetLevel(_doc, connector.Origin.Z);
            if (level == null)
                return;
            double extensionLength = GetExtensionLength(connector);
            // Вычисляем конечную точку
            XYZ endPoint = CalculateEndPoint(origin, basisZ, extensionLength);
            using Transaction transaction = new Transaction(_doc, "Поворот вверх на 45°");
            try
            {
                transaction.Start();
                switch (element?.Category.BuiltInCategory)
                {
                    case BuiltInCategory.OST_Conduit:
                        DrawConduit(_doc, selectedReference, globalPoint, connector, origin, endPoint,
                            level);
                        break;
                    case BuiltInCategory.OST_DuctCurves:
                        DrawDuct(_doc, selectedReference, globalPoint, connector, origin, endPoint);
                        break;
                    case BuiltInCategory.OST_PipeCurves:
                        DrawPipeWithElbow(_doc, selectedReference, globalPoint, connector, origin,
                            endPoint, true);
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
            try
            {
                tr.Start();
                // Шаг 1: Выбор целевого элемента и получение точки
                if (AreOpposite(ctx.TargetConn, ctx.AttachConn))
                {
                    if (!HandleOppositeConnectors(ctx))
                    {
                        tr.RollBack();
                        return; // Пользователь нажал Cancel
                    }
                }
                else
                {
                    AlignAndConnect(ctx); // Соосные коннекторы
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
            // 1. Для труб/каналов – отдельная логика
            if (ctx.Attach.Element is Pipe or Duct)
                return HandlePipeOrDuctOpposite(ctx);

            // 2. Для остальных семейств
            return HandleGenericOpposite(ctx);
        }

        private bool HandlePipeOrDuctOpposite(AlignContext ctx)
        {
            var choice = ShowChoiceDialog(
                "Соединить",
                "Выберите действие",
                ("Удлинить/укоротить трубу", 1),
                ("Переместить элемент", 2));
            switch (choice)
            {
                case 1: // Удлинить-укоротить
                    LengthenCurve(ctx.AttachConn, ctx.TargetConn);
                    XYZ newMove = ctx.TargetConn.Origin - ctx.AttachConn.Origin;
                    ElementTransformUtils.MoveElement(_doc, ctx.Attach.Id, newMove);
                    ctx.AttachConn.ConnectTo(ctx.TargetConn);
                    break;

                case 2: // Переместить
                    AlignAndConnect(ctx);
                    break;

                default: return false; // Cancel
            }

            return true;
        }

        private bool HandleGenericOpposite(AlignContext ctx)
        {
            bool singleConnection = GetConnectedConnectors(ctx.Attach.ConnectorManager).Count == 1;
            XYZ translationVector = ctx.TargetConn.Origin -
                                    ctx.AttachConn.Origin;
            if (singleConnection)
            {
                var choice = ShowChoiceDialog(
                    "Соединить",
                    "Выберите действие",
                    ("Переместить выбранный элемент", 1),
                    ("Переместить все элементы", 2));

                switch (choice)
                {
                    case 1:
                        break;

                    case 2:
                        AlignAndConnect(ctx); // Пользовательская логика
                        return true;

                    default:
                        return false; // Cancel
                }
            }

            ElementTransformUtils.MoveElement(_doc, ctx.AttachId, translationVector);
            // 3. Соединяем элементы
            ctx.AttachConn.ConnectTo(ctx.TargetConn);
            return true;
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
            return (int)res; // Cancel => 0
        }

        private void AlignAndConnect(AlignContext ctx)
        {
            // Шаг 5: Отключение существующих соединений и сохранение их для восстановления
            var existingConnections = DisconnectExistingConnections(ctx.Attach.ConnectorManager);
            // Шаг 7: Выравнивание соединителей
            AlignConnectors(ctx.TargetConn, ctx.AttachConn,
                ctx.Attach.Element);
            var translationVector = ctx.TargetConn.Origin -
                                    ctx.AttachConn.Origin;
            ElementTransformUtils.MoveElement(_doc, ctx.AttachId, translationVector);
            // Соединение после вращения
            ctx.AttachConn.ConnectTo(ctx.TargetConn);
            // Шаг 8: Восстановление предыдущих соединений
            if (existingConnections.Any())
            {
                RestoreConnections(existingConnections);
            }
        }

        private static bool AreOpposite(Connector c1, Connector c2)
        {
            double dot = c1.CoordinateSystem.BasisZ
                .DotProduct(c2.CoordinateSystem.BasisZ);
            const double oppositeThreshold = -1.0;
            return Math.Abs(Math.Round(dot, 10) - Math.Round(oppositeThreshold, 10)) < 0.001;
        }

        private bool TryBuildContext(out AlignContext ctx)
        {
            ctx = default;

            // 1. Элемент-приёмник
            if (!TryPickElement(
                    "Выберите точку на элементе, к которому хотите присоединить",
                    out var target, out var targetPt))
                return false;

            // 2. Элемент-донор
            if (!TryPickElement(
                    "Выберите точку на присоединяемом элементе",
                    out var attach, out var attachPt,
                    target.Element))
                return false;

            // 3. Ближайшие коннекторы
            var tConn = target.FindClosestFreeConnector(targetPt);
            var aConn = attach.FindClosestFreeConnector(attachPt);

            if (tConn is null || aConn is null) // нет коннекторов
                return false;

            ctx = new AlignContext(target, attach, tConn, aConn);
            return true;
        }

        private bool TryPickElement(
            string prompt,
            out ElementWrapper wrapper,
            out XYZ pickedPoint,
            Element elementToExclude = null)
        {
            wrapper = null;
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
                wrapper = r == null ? null : new ElementWrapper(_doc.GetElement(r));

                return wrapper != null;
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

        private List<ConnectorConnection> DisconnectExistingConnections(ConnectorManager connectorManager)
        {
            var connections = new List<ConnectorConnection>();

            foreach (Connector connector in connectorManager.Connectors)
            {
                if (!connector.IsConnected)
                {
                    continue;
                }

                // Собираем подключенные коннекторы перед отключением
                var connectedConnectors = new List<Connector>();

                foreach (Connector connectedConnector in connector.AllRefs)
                {
                    if (!IsPhysicalDomain(connectedConnector.Domain))
                        continue;
                    if (!connectedConnector.IsConnected) continue;
                    connectedConnectors.Add(connectedConnector);
                }

                var connectorConnection = new ConnectorConnection(connector);
                // Отключаем и записываем подключенные коннекторы
                foreach (Connector connectedConnector in connectedConnectors)
                {
                    try
                    {
                        // Сохраняем подключенный коннектор
                        connectorConnection.ConnectedConnectors.Add(
                            new ConnectorConnection(connectedConnector));

                        // Отключаем коннекторы
                        connector.DisconnectFrom(connectedConnector);
                    }
                    catch (Exception ex)
                    {
                        // ignored
                    }
                }


                if (connectorConnection.ConnectedConnectors.Count > 0)
                {
                    connections.Add(connectorConnection);
                }
            }

            return connections;
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

        private void AlignConnectors(Connector targetConnector, Connector attachingConnector, Element attachingElement)
        {
            // Получаем нормализованные векторы BasisZ коннекторов
            XYZ targetBasisZ = targetConnector.CoordinateSystem.BasisZ.Normalize();
            XYZ attachingBasisZ = attachingConnector.CoordinateSystem.BasisZ.Normalize();

            // Желаемое направление для attachingBasisZ - противоположное targetBasisZ
            XYZ desiredDirection = -targetBasisZ;

            // Вычисляем скалярное произведение между attachingBasisZ и желаемым направлением
            double dotProduct = attachingBasisZ.DotProduct(desiredDirection);

            // Корректируем значение dotProduct на случай погрешностей вычислений
            dotProduct = Math.Min(Math.Max(dotProduct, -1.0), 1.0);

            // Вычисляем угол между attachingBasisZ и desiredDirection
            double angle = Math.Acos(dotProduct);

            // Вычисляем ось вращения
            XYZ rotationAxis = attachingBasisZ.CrossProduct(desiredDirection);

            // Если ось вращения имеет нулевую длину (векторы параллельны или антипараллельны)
            if (rotationAxis.IsZeroLength())
            {
                // Векторы параллельны или антипараллельны
                if (dotProduct < -0.9999)
                {
                    // Векторы направлены в ту же сторону, нужно вращение на 180 градусов
                    angle = Math.PI;

                    // Выбираем произвольную ось вращения, перпендикулярную attachingBasisZ
                    rotationAxis = attachingConnector.CoordinateSystem.BasisX;

                    if (rotationAxis.IsZeroLength())
                    {
                        rotationAxis = attachingConnector.CoordinateSystem.BasisY;
                    }
                }
                else
                {
                    // Векторы уже направлены в противоположные стороны, вращение не требуется
                    angle = 0;
                }
            }
            else
            {
                // Нормализуем ось вращения
                rotationAxis = rotationAxis.Normalize();
            }

            // Выполняем вращение, если угол больше допустимого порога
            if (angle > 1e-6)
            {
                // Создаем неограниченную линию вращения с началом в attachingConnector.Origin и направлением rotationAxis
                Line rotationLine = Line.CreateUnbound(attachingConnector.Origin, rotationAxis);

                // Вращаем присоединяемый элемент
                ElementTransformUtils.RotateElement(_doc, attachingElement.Id, rotationLine, angle);
            }
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
                    var targetConnector = connectorConnection.Connector;
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
                        var attachingConnector = connectedInfo.Connector;
                        var attachingElement = connectedInfo.Element;
                        if (attachingConnector == null || attachingElement == null)
                        {
                            // Пропускаем некорректные соединения
                            continue;
                        }

                        // Получаем ConnectorManager для присоединяемого элемента
                        var attachingConnectorManager = GetConnectorManager(attachingElement);

                        if (attachingConnectorManager == null)
                        {
                            // Пропускаем, если не удалось получить ConnectorManager
                            continue;
                        }

                        // Отключаем существующие подключения присоединяемого элемента
                        var existingConnections = DisconnectExistingConnections(attachingConnectorManager);

                        // Добавляем отключённые соединения в список для последующей обработки
                        newConnections.AddRange(existingConnections);

                        // Вычисляем вектор перемещения
                        var translationVector = targetConnector.Origin - attachingConnector.Origin;

                        // Перемещаем присоединяемый элемент
                        ElementTransformUtils.MoveElement(_doc, attachingElement.Id, translationVector);
                        // Выравниваем коннекторы
                        AlignConnectors(targetConnector, attachingConnector, attachingElement);
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

        public static void DrawDuct(Document doc, Reference selectedReference, XYZ selectedPoint,
            Connector closestConnector, XYZ start, XYZ end)
        {
            if (doc == null || selectedReference == null || selectedPoint == null ||
                closestConnector == null || start == null || end == null)
            {
                throw new ArgumentNullException("Один или несколько входных параметров равны null.");
            }

            try
            {
                Duct element = doc.GetElement(selectedReference) as Duct;
                if (element == null)
                {
                    TaskDialog.Show("Ошибка", "Выбранный элемент не является воздуховодом.");
                    return;
                }

                // Вычисляем вектор направления от start до end
                XYZ directionVector = end - start;
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

                DuctType ductType = element.DuctType;
                MechanicalSystemType mechanicalSystem = GetMechanicalSystem(doc, closestConnector);
                ElementId level = GetLevel(doc, selectedPoint.Z);

                Duct newDuct = Duct.Create(doc, mechanicalSystem.Id, ductType.Id, level, start, adjustedEnd);

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
                        double width = element.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble();
                        double height = element.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble();

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
                        double diameter = element.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM).AsDouble();
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

                if (Math.Abs(start.X - end.X) < 0.001 && Math.Abs(start.Y - end.Y) < 0.001)
                {
                    XYZ basisZ = closestConnector.CoordinateSystem.BasisZ;
                    XYZ source = new XYZ(0.0, 1.0, 0.0);
                    double angle = basisZ.AngleTo(source);
                    if (basisZ.DotProduct(source) > 0.0 && basisZ.X > 1.0)
                        angle = -angle;
                    Line bound = Line.CreateBound(start, end);
                    if (start.Z > end.Z)
                        bound = Line.CreateBound(end, start);
                    ElementTransformUtils.RotateElement(doc, newDuct.Id, bound, angle);
                }

                // Создаём отвод между коннекторами
                FamilyInstance newElbow = doc.Create.NewElbowFitting(closestConnector, closestConnectorNewDuct);
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

        private static void DrawPipeWithElbow(Document doc, Reference selectedReference, XYZ selectedPoint,
            Connector closestConnector1, XYZ start, XYZ end, bool includeElbow)
        {
            try
            {
                if (doc.GetElement(selectedReference) is Pipe element)
                {
                    PipeType pipeType = element.PipeType;
                    double diameter = element.Diameter;
                    const double scalingFactor = 2; // Этот коэффициент можно настроить по необходимости

                    // Вычисляем дополнительную длину на основе диаметра
                    double extraLength = diameter * scalingFactor;

                    // Вычисляем вектор направления от start до end
                    XYZ direction = end - start;
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
                    PipingSystemType pipeSystem = GetPipeSystem(doc, closestConnector1);
                    ElementId level = GetLevel(doc, selectedPoint.Z);
                    Pipe newPipe = Pipe.Create(doc, pipeSystem.Id, pipeType.Id, level, start, adjustedEnd);
                    newPipe.LookupParameter("Диаметр").Set(diameter);
                    Connector[] connectors = ConnectorArray(newPipe);

                    if (includeElbow)
                    {
                        Connector connector = NearestConnector(connectors, selectedPoint);
                        FamilyInstance newElbow = doc.Create.NewElbowFitting(closestConnector1, connector);
                    }


                    return;
                }
            }
            catch
            {
                return;
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

        private static void Bloom(Document doc, Element selectedElement, MEPCurveType elementType)
        {
            Connector[] source = ConnectorArrayUnused(selectedElement);
            if (source == null)

                return;
            foreach (Connector connector in source)
            {
                if (connector.Domain is Domain.DomainPiping or Domain.DomainHvac)
                {
                    if (connector.Domain == Domain.DomainPiping)
                    {
                        PipingSystemType pipeSystem = GetPipeSystem(doc, connector);
                        double extensionLength = GetExtensionLength(connector);
                        XYZ origin = connector.Origin;
                        XYZ xyz1 = origin + extensionLength * connector.CoordinateSystem.BasisZ;
                        ElementId level = GetLevel(doc, origin.Z);
                        if (elementType != null)
                        {
                            CreatePipe(doc, pipeSystem, elementType as PipeType, connector, level, origin, xyz1);
                        }
                    }

                    if (connector.Domain == Domain.DomainHvac)
                    {
                        MechanicalSystemType mechanicalSystem = GetMechanicalSystem(doc, connector);
                        double extensionLength = GetExtensionLength(connector);
                        XYZ origin = connector.Origin;
                        XYZ endPoint = origin + extensionLength * connector.CoordinateSystem.BasisZ;
                        ElementId level = GetLevel(doc, origin.Z);
                        CreateDuct(doc, mechanicalSystem, elementType as DuctType, connector, level, origin, endPoint);
                    }
                }
            }
        }

        public static MEPCurveType DeterminingTypeOfPipeByFitting(Document doc, Element element)
        {
            ConnectorSet connectors = (element as FamilyInstance)?.MEPModel.ConnectorManager.Connectors;
            if (connectors != null)
            {
                foreach (Connector connector in connectors)
                {
                    // Перебираем соединения
                    foreach (Connector connectedConnector in connector.AllRefs)
                    {
                        if (connectedConnector.Owner is MEPCurve connectedPipe)
                        {
                            // Получаем тип присоединенной трубы
                            return doc.GetElement(connectedPipe.GetTypeId()) as MEPCurveType;
                        }
                    }
                }
            }

            return null;
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
            Connector connector, ElementId level, XYZ origin, XYZ xyz1)
        {
            Element pipe = Pipe.Create(doc, pipeSystem.Id, pipeType.Id, level, origin, xyz1);
            Parameter diameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            var diameterConnectorSelected = connector.Radius * 2;
            diameter.Set(diameterConnectorSelected);
            Connector[] cA = ConnectorArrayUnused(pipe);
            NearestConnector(cA, connector.Origin).ConnectTo(connector);
        }

        private static void CreateDuct(Document doc, MechanicalSystemType mechanicalSystemType, DuctType ductType,
            Connector connector, ElementId level, XYZ origin, XYZ endPoint)
        {
            Element duct = Duct.Create(doc, mechanicalSystemType.Id, ductType.Id, level, origin, endPoint);
            if (connector.Shape == ConnectorProfileType.Round)
            {
                Parameter diameter = duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                var diameterConnectorSelected = connector.Radius * 2;
                diameter.Set(diameterConnectorSelected);
            }
            else if (connector.Shape == ConnectorProfileType.Rectangular)
            {
                Parameter width = duct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
                Parameter height = duct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
                var widthConnectorSelected = connector.Width;
                var heightConnectorSelected = connector.Height;
                width.Set(widthConnectorSelected);
                height.Set(heightConnectorSelected);
            }
            else if (connector.Shape == ConnectorProfileType.Oval)
            {
                return;
            }

            Connector[] cA = ConnectorArrayUnused(duct);
            NearestConnector(cA, connector.Origin).ConnectTo(connector);
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
                .FirstOrDefault();

            // Если не найден уровень ниже 'z', берем ближайший уровень выше
            if (closestLevel == null)
            {
                closestLevel = levels
                    .OrderBy(lvl => lvl.Elevation - z)
                    .FirstOrDefault();
            }

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