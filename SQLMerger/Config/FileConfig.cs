using System;
using System.Collections.Generic;
using System.Text;

namespace SQLMerger.Config
{
    public class FileConfig
    {
        public string Path { get; set; }
        public List<string> KeepTables { get; set; }
        public int MergeFileInto { get; set; } = -1;
        public Dictionary<string, ConfigTable> Tables { get; set; }
    }
}
