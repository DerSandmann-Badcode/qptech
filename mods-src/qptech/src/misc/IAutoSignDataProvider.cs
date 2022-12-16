using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qptech.src
{
    public enum AutoSignType { NORMAL,LOG}
    interface IAutoSignDataProvider
    {
        AutoSignType GetAutoSignType();
        string GetAutoSignText();

    }
}
