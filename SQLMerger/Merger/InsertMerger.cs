using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLMerger.Config;
using SQLMerger.Instance;

namespace SQLMerger.Merger
{
    public static class InsertMerger
    {
        private static int lastId;

        public static void Merge(Config.Config globalConfig, Table target, Table b)
        {
            Console.WriteLine($"--- Merging INSERT in table: {target.Name}");

            cacheSameIds = new Dictionary<string, int>();

            var config = globalConfig.Global;
            if (globalConfig.Tables != null && globalConfig.Tables.ContainsKey(target.Name))
            {
                if (globalConfig.Tables[target.Name].Config != null)
                {
                    config = globalConfig.Tables[target.Name].Config;
                }

                if (globalConfig.Tables[target.Name].SameOn != null)
                {
                    var comparedFields = globalConfig.Tables[target.Name].SameOn;
                    foreach (var field in comparedFields)
                    {
                        cacheSameIds.Add(field, b.GetColumnId(field));
                    }
                }
            }

            if (target.ID == b.ID)
                return;

            try
            {
                lastId = int.Parse(target.Inserts[^1].Rows[^1][0]) + 1;
            }
            catch (Exception)
            {
                if (target.Inserts.Count > 0 && target.Inserts[0].Rows != null)
                    lastId = target.Inserts.Count * target.Inserts[0].Rows.Count + 999;
                else
                    lastId = 1;
            }

            var toInsert = new List<Insert>();
            foreach (var insert in b.Inserts)
            {
                var i = new Insert {ID = insert.ID, Table = insert.Table};
                toInsert.Add(new Insert());

                foreach (var row in insert.Rows)
                {
                    if (Register.Registers[b.ID].InBlackBox(insert.Table, row[0]))
                    {
                        Console.WriteLine($"!-- Skipping Black Box element: {row[0]}");
                        continue;
                    }

                    var same = (config.WhenSame == ActionName.SkipCheck) ? null : AreSame(target, row);
                    if (same != null)
                        BindAction(config.WhenSame, target, b, same, row, i);
                    else
                    {
                        var like = (config.WhenLike == ActionName.SkipCheck) ? null : AreLike(target, row);
                        if (like != null)
                            BindAction(config.WhenLike, target, b, like, row, i);
                        else
                        {
                            var hasId = (config.WhenIdSame == ActionName.SkipCheck) ? null : HasId(target, row);
                            if (hasId != null)
                                BindAction(config.WhenIdSame, target, b, hasId, row, i);
                            else
                            {
                                if (b.PrimaryKey.Length == 1)
                                    Register.Registers[b.ID].AddPK(b.Name, row[0]);
                                i.Rows.Add(row);
                            }
                        }
                    }
                }

                if(i.Rows.Count != 0)
                    toInsert.Add(i);
            }

            if (toInsert.Count != 0)
                target.Inserts.AddRange(toInsert);
        }

        public static void BindAction(ActionName action, Table target, Table b, List<string> targetRow, List<string> bRow, Insert insert)
        {
            switch (action)
            {
                case ActionName.BindToThis:
                    Register.Registers[b.ID].UpdatePk(b.Name, bRow[0], targetRow[0]);
                    break;
                case ActionName.Add:
                    Register.Registers[b.ID].UpdatePk(b.Name, bRow[0], "" + lastId);
                    bRow[0] = "" + lastId;
                    lastId++;
                    insert.Rows.Add(bRow);
                    break;
                case ActionName.Ignore:
                    Register.Registers[b.ID].AddToBlackBox(b.Name, bRow[0]);
                    break;
                case ActionName.SkipBoth:
                    Register.Registers[target.ID].AddToBlackBox(target.Name, targetRow[0]);
                    Register.Registers[b.ID].AddToBlackBox(b.Name, bRow[0]);
                    break;
            }
        }

        private static Dictionary<string, int> cacheSameIds = new Dictionary<string, int>();
        public static List<string> AreSame(Table target, List<string> row)
        {
            if (cacheSameIds.Count > 0)
            {
                foreach (var insert in target.Inserts)
                {
                    foreach (var rowTarget in insert.Rows)
                    {
                        var isSame = true;
                        foreach (var cacheSameId in cacheSameIds)
                        {
                            if (rowTarget[cacheSameId.Value] == row[cacheSameId.Value]) continue;
                            isSame = false;
                            break;

                        }
                        if (isSame)
                            return rowTarget;
                    }
                }
            }
            return null;
        }

        public static List<string> AreLike(Table target, List<string> row)
        {
            foreach (var insert in target.Inserts)
            {
                foreach (var rowTarget in insert.Rows)
                {
                    var areLike = 0;
                    for (var i = 1; i < rowTarget.Count; i++)
                    {
                        if (rowTarget[i] == row[i])
                        {
                            areLike++;
                        }
                        break;

                    }
                    if (areLike + 3 >= insert.Rows.Count)
                        return rowTarget;
                }
            }
            return null;
        }

        public static List<string> HasId(Table target, List<string> row)
        {
            return target.Inserts.SelectMany(insert => insert.Rows).FirstOrDefault(targetRow => targetRow[0] == row[0]);
        }

    }
}
