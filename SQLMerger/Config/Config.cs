using System.Collections.Generic;

namespace SQLMerger.Config
{
    public class AppendConfig
    {
        public bool DoReplace { get; set; } = true;
        public string StartBeforeSet { get; set; }
        public string StartAfterSet { get; set; }
        public string EndBeforeSet { get; set; }
        public string EndAfterSet { get; set; }
    }

    public class Config
    {
        public string Output { get; set; }
        public string LogDir { get; set; }
        public CacheConfig Cache { get; set; }
        public BaseConfig Global { get; set; } = new BaseConfig();
        public List<string> KeepTables { get; set; }
        public List<string> IgnoreTables { get; set; }
        public Dictionary<string, ConfigTable> Tables { get; set; }
        public List<FileConfig> Files { get; set; }
        public AppendConfig Append { get; set; }
        public Dictionary<string, List<string>> Handlers { get; set; } = new Dictionary<string, List<string>>();
    }
}
