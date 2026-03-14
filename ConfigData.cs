using System;
using SFS.Variables;

namespace NetworkControlMod
{
    [Serializable]
    public class ConfigData
    {
        public Int_Local maxLines = new Int_Local { Value = 0 };   // 0 = unlimited
        public Bool_Local showFullPath = new Bool_Local { Value = false };
    }
}