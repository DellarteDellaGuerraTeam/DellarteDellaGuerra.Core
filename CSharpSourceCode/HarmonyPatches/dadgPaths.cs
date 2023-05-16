using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace DADG_Core.Utilities
{
    public static class DADGPaths
    {

        public static string DellarteDellaGuerraModuleDataPath
        {
            get { return ModuleHelper.GetModuleFullPath("DellarteDellaGuerra") + "ModuleData/"; }
        }
    }
}