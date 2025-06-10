using System.Windows.Controls;
using UpdatingParameters.Models;
using UpdatingParameters.ViewModels;

namespace UpdatingParameters.Views;

public partial class FilterGroupControl : UserControl
{
    public FilterGroupControl()
    {
        
        InitializeComponent();
        DataContext = new FilterGroupVM();
    }
}