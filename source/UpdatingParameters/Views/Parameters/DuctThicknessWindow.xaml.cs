using System.Windows;
using UpdatingParameters.ViewModels.Parameters;


namespace UpdatingParameters.Views.Parameters;

public partial class DuctThicknessWindow
{
    public DuctThicknessWindow( DuctThicknessViewModel ductThicknessViewModel)
    {
        InitializeComponent();
        DataContext = ductThicknessViewModel;
    }
}