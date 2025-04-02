using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipelineGradients.Models
{
   public class MarkAnnotationMdl
    {
        public string Name {  get; set; }
        public bool IsCheked {  get; set; }
        public MarkAnnotationMdl(Element tag) 
        {
        Name = tag.Name;

        }
    }
}
