using NoNameAPI.Abstractions;
using NoNameAPI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyElementsByLevel
{
    public class NoNameConnector :
     INoNamePlugin,
     INoNamePluginGeneral,
     IRibbonSettings,
     ISubCommandsOrder
    {
        private static NoNameConnector _instance;
        public static NoNameConnector Instance => NoNameConnector._instance ?? (NoNameConnector._instance = new NoNameConnector());
        public string AvailProductExternalVersion => "2024";

        public string FullClassName => string.Empty;

        public bool CanAddToRibbon => true;

        public string ToolTipHelpImage => string.Empty;

        public List<string> SubPluginsNames => new List<string>(3)
    {
      "mprMEPCopyToLevels",
      "mprMEPCopyByDistance",
      "mprMEPCopyInDirection"
    };

        public List<string> SubHelpImages => new List<string>(3)
    {
      string.Empty,
      string.Empty,
      string.Empty
    };

        public List<string> SubClassNames => new List<string>(3)
    {
      string.Empty,
      string.Empty,
      string.Empty
    };

        public SupportedProduct SupportedProduct => SupportedProduct.Revit;

        public string Name => "mprMEPCopy";

        public string Price => "1";

        public string AvailabilityClassName => string.Empty;

        public List<string> AvailabilityClassNameForSubCommands => new List<string>(3)
    {
      "mprMEPCopy.Commands.CopyToLevelsCommandAvailability",
      "mprMEPCopy.Commands.CopyByDistanceCommandAvailabity",
      "mprMEPCopy.Commands.CopyInDirectionCommandAvailability"
    };

        public bool BuildOnlySubCommands => true;

        public List<int> SubCommandsOrder => new List<int>();
    }
}
