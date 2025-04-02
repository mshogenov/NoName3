using CopyElementsByLevel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyElementsByLevel.UserConfig
{
    public class CopyToLevelsUserConfig
    {
        public List<SelectedLevelConfig> SelectedLevels { get; set; } = new List<SelectedLevelConfig>();
        public bool TryGetValue(string key, out List<string> values)
        {
            SelectedLevelConfig selectedLevelConfig = SelectedLevels.Find(pair => pair.Key == key);
            if (selectedLevelConfig != null)
            {
                values = selectedLevelConfig.Value;
                return true;
            }
            values =null;
            return false;
        }
    }
}
