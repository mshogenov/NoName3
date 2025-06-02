namespace SystemModelingCommands.Services;

public class CustomFailurePreprocessor : IFailuresPreprocessor
{
    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    {
        // Получаем все ошибки
        IList<FailureMessageAccessor> failures = failuresAccessor.GetFailureMessages();

        foreach (FailureMessageAccessor failure in failures)
        {
            // Получаем тип ошибки
            FailureSeverity failureSeverity = failure.GetSeverity();

            // Получаем id ошибки
            FailureDefinitionId failureId = failure.GetFailureDefinitionId();

            // Различные варианты обработки
            switch (failureSeverity)
            {
                case FailureSeverity.Warning:
                    // Игнорировать предупреждение
                    failuresAccessor.DeleteWarning(failure);
                    break;

                case FailureSeverity.Error:
                
                    // Отменить операцию
                    return FailureProcessingResult.ProceedWithRollBack;
            }
        }

        return FailureProcessingResult.Continue;
    }
}