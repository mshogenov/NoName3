using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorHandlerUpdater
{
    public class ErrorHandling : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            var failures = failuresAccessor.GetFailureMessages();
            foreach (var failure in failures)
            {
               
                // Удаление ошибки, чтобы Revit её игнорировал
                failuresAccessor.DeleteWarning(failure);
            }
            return FailureProcessingResult.Continue;
        }
        
    }
}
