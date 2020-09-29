using System;
using System.Collections.Generic;
using System.Text;
using SQLMerger.Instance;

namespace SQLMerger.Config
{
    public struct FkConfig
    {
        public string Column { get; set; }
        public string TargetTable { get; set; }
        public string TargetColumn { get; set; }

    }

    public struct FkRuleRuleConfig
    {
        public string Column { get; set; }
        public string Value { get; set; }
    }

    public struct FkRuleConfig
    {
        public FkRuleRuleConfig Rule { get; set; }
        public FkConfig ForeignKey { get; set; }
    }

    public struct ForceMerge
    {
        public string IdSource { get; set; }
        public string IdTarget { get; set; }

    }

    public struct CopyDataWhere
    {
        public string Column { get; set; }
        public string Value { get; set; }
    }

    public struct CopyDataConfig
    {
        public CopyDataWhere Where { get; set; }

        // column_name -> target_value -> new_value
        public Dictionary<string, Dictionary<string, string>> Change { get; set; }
    }

    public struct MoveDataConfig
    {
        public CopyDataWhere Where { get; set; }
        public string To { get; set; }
        public List<string> StripTagsFor { get; set; }
    }

    public class ConfigTable
    {
        public string NewName { get; set; }
        public string ForceName { get; set; }
        public BaseConfig Config { get; set; }
        public string PopulateWhiteBoxFromFile { get; set; }
        public Dictionary<string, List<string>> BlackBox { get; set; }
        public Dictionary<string, List<string>> WhiteBox { get; set; }
        public bool SkipInserts { get; set; } = false;
        public List<string> SameOn { get; set; }
        public List<FkConfig> ForeignKey { get; set; }
        public List<FkRuleConfig> ForeignKeyRule { get; set; }
        public List<string> CustomHandler { get; set; }
        public bool ExceptionOnMissingFk { get; set; } = true;
        public Dictionary<string, ConfigColumn> Columns { get; set; }
        public Dictionary<string, Column> NewColumns { get; set; }
        public List<string> IgnoreColumns { get; set; }
        public List<string> DropColumns { get; set; }
        public List<string> RowAppend { get; set; }
        public List<ForceMerge> ForceMerge { get; set; }
        public bool UpdatePk { get; set; } = true;
        public bool FirstTimeMerge { get; set; } = true;
        public bool ZeroIdInTable { get; set; } = false;
        public List<CopyDataConfig> CopyData { get; set; }
        public List<MoveDataConfig> MoveData { get; set; }
    }
}
