using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DesignationOfRisers.Models
{
    public partial class ViewMdl:ObservableObject
    {
        public View View { get; } = null;
        public string Name { get; } = "";
        public ElementId Id { get; } = null;
        [ObservableProperty] private bool _isCheked=false;
        public ViewMdl(View view) 
        {
            View = view;
        Name = view.Name;
            Id=view.Id;
        }
    }
}
