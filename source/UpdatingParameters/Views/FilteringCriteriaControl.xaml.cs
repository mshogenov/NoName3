using System.Windows.Controls;
using UpdatingParameters.ViewModels;

namespace UpdatingParameters.Views;

public partial class FilteringCriteriaControl : UserControl
{
    public FilteringCriteriaControl()
    {
        InitializeComponent();
        DataContext = new FilteringCriteriaVM();

    }

   
}