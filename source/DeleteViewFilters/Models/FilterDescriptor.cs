using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeleteViewFilters.Models
{
    public partial class FilterDescriptor : ObservableObject
    {
       [ObservableProperty] private bool _isCheked;
        public FilterElement Filter {  get; set; }
        public string Name { get; set; }
        public FilterDescriptor(FilterElement filter) 
        {
        Filter = filter;
            Name = filter.Name;
        }
    }
}
