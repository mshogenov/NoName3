using CopyElementsByLevel.ViewModels;
using System.Windows;
using System.Windows.Markup;

namespace CopyElementsByLevel.Views
{
    public sealed partial class CopyElementsByLevelView :  IComponentConnector
    {
        
        public CopyElementsByLevelView()
        {
            
            InitializeComponent();
        }
        void IComponentConnector.InitializeComponent()
        {
            if (_contentLoaded)
                return;
            _contentLoaded = true;
            Application.LoadComponent(this, new Uri("/mprMEPCopy_2024;component/views/copytolevelswindow.xaml", UriKind.Relative));
        }
    }
}