using System;
using System.Collections.Generic;
using System.Text;

namespace SQLMerger.Instance
{
    public class FileInstance
    {
        public Dictionary<string, Table> Tables { get; set; } = new Dictionary<string, Table>();

    }
}
