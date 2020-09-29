using System;
using System.Collections.Generic;
using System.Text;
using SQLMerger.Instance;

namespace SQLMerger.Merger
{
    public static class RegisterBuilder
    {
        public static void BuildFk(FileInstance file, int fileId, Config.Config config)
        {
            var register = Register.Registers[fileId];
            foreach (var table in file.Tables)
            {
                if (config.Files[fileId].Tables != null &&
                    config.Files[fileId].Tables.ContainsKey(table.Key) &&
                    config.Files[fileId].Tables[table.Key].ForeignKey != null)
                {
                    foreach (var fkConfig in config.Files[fileId].Tables[table.Key].ForeignKey)
                    {
                        register.AddFK(table.Key, fkConfig.Column, fkConfig.TargetTable);
                    }
                }


                if (config.Files[fileId].Tables != null &&
                    config.Files[fileId].Tables.ContainsKey(table.Key) &&
                    config.Files[fileId].Tables[table.Key].ForeignKeyRule != null)
                {
                    foreach (var fkRule in config.Files[fileId].Tables[table.Key].ForeignKeyRule)
                    {
                        register.AddFkRule(table.Key, fkRule.ForeignKey.Column, fkRule);
                    }
                }

                foreach (var foreignKey in table.Value.ForeignKeys)
                {
                    register.AddFK(table.Key, foreignKey.Key, foreignKey.Value.Table);
                }
            }
        }
    }
}
