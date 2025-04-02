using MarkingOfMarksNoModeless.ViewModels;

namespace MarkingOfMarksNoModeless.Views
{
    public sealed partial class MarkingOfMarksView
    {
       
        public MarkingOfMarksView(MarkingOfMarksViewModel viewModel)
        {
           DataContext = viewModel;
            InitializeComponent();
        }

        
    }
}