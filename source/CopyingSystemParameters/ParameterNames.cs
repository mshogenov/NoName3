using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyingSystemParameters
{
    enum ParameterNames
    {
        [Description("ADSK_Количество")]
        ADSK_Количество,
        [Description("ADSK_Система_Сокращение")]
        ADSK_Система_Сокращение,
        [Description("ADSK_Система_Имя")]
        ADSK_Система_Имя,
        [Description("ADSK_Система_Тип")]
        ADSK_Система_Тип,
        [Description("ADSK_Система_Классификация")]
        ADSK_Система_Классификация,
        [Description("msh_Количество с учетом типовых этажей")]
        msh_Количество_с_учетом_типовых_этажей,
        [Description("msh_Количество этажей")]
        msh_Количество_этажей,
        [Description("msh_Типовой этаж")]
        msh_Типовой_этаж
    }
}
