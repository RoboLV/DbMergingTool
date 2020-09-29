using System;
using System.Collections.Generic;
using System.Text;

namespace SQLMerger.Config
{
    public enum ActionName
    {
        Add = 0, 
        Ignore = 1, 
        SkipBoth = 2, 
        BindToThis = 3, 
        SkipCheck = 4,
        ReverseBind = 5,
        CopyInto = 6
    }

    public enum CacheAction
    {
        Add = 0,
        Drop = 1
    }
}
