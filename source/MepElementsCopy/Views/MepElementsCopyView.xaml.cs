using System.ComponentModel;
using System.Windows.Media.Animation;
using MepElementsCopy.ViewModels;

namespace MepElementsCopy.Views;

public sealed partial class MepElementsCopyView
{
    public MepElementsCopyView(MepElementsCopyLevelsViewModel levelsViewModel)
    {
        DataContext = levelsViewModel;
        InitializeComponent();
    }
}