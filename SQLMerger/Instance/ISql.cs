using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQLMerger.Instance
{
    public interface ISql
    {
        public void GetSql(in StreamWriter writer);
    }
}
