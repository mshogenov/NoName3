using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CopyByLevel.Services;
using CopyByLevel.ViewModels;
using CopyByLevel.Views;
using Nice3point.Revit.Toolkit.External;
using System.Windows;

namespace CopyByLevel.Commands
{
    /// <summary>
    ///     External command entry point invoked from the Revit interface
    /// </summary>
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class CopyByLevelCommand : ExternalCommand
    {
        public override void Execute()
        {
            try
            {
                
                CopyMepService copyMepService = new CopyMepService();
                copyMepService.FillLevelWrs();
                if (!copyMepService.FillMepElements())
                {
                    MessageBox.Show("", "");
                    
                }
                var viewModel = new CopyByLevelViewModel(copyMepService);
                var view = new CopyByLevelView(viewModel);
                view.ShowDialog();
                
            }
            catch (OperationCanceledException ex)
            {
                return ;
            }
            
            
        }
    }
}