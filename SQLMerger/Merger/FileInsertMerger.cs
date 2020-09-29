using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using SQLMerger.Config;
using SQLMerger.Handlers;
using SQLMerger.Instance;
using SQLMerger.Interpreter;

namespace SQLMerger.Merger
{
    public static class FileInsertMerger
    {
        private static Dictionary<string, int> lastIdDictionary = new Dictionary<string, int>();
        private static BaseConfig config;
        private static int lastId; // TODO: FIX THIS
        private static Dictionary<string, Dictionary<string, int>> cacheSameFields = new Dictionary<string, Dictionary<string, int>>();
        public static bool Merge(FileInstance file, bool firstTime = false)
        {
            var didMerge = false;
            var canDoMerge = true;
            var canUpdatePk = true;
            cacheSameFields.Clear();

            // -- Tables
            foreach (var table in file.Tables)
            {
                Console.WriteLine($"--- Merging INSERT in table: {table.Key}");

                var pkId = 0;
                if (table.Value.PrimaryKey != null && table.Value.PrimaryKey.Length > 0)
                    pkId = table.Value.GetColumnId(table.Value.PrimaryKey[0]);

                config = MergingController.Config.Global;
                lastId = GetLastId(table.Key, pkId);
                if (MergingController.Config.Tables.ContainsKey(table.Key))
                {
                    // Changes configuration from global to local
                    if (MergingController.Config.Tables[table.Key].Config != null)
                        config = MergingController.Config.Tables[table.Key].Config;

                    // Builds Cache of ("same" compression) field IDs
                    if (!cacheSameFields.ContainsKey(table.Key))
                    {
                        cacheSameFields.Add(table.Key, new Dictionary<string, int>());
                        var comparedFields = MergingController.Config.Tables[table.Key].SameOn;
                        if (comparedFields != null)
                        {
                            foreach (var field in comparedFields)
                            {
                                cacheSameFields[table.Key].Add(field, table.Value.GetColumnId(field));
                            }
                        }
                    }

                    // Gets info to whether or not do merge on first time
                    if (MergingController.FirstRun)
                    {
                        canDoMerge = MergingController.Config.Tables[table.Key].FirstTimeMerge;
                        canUpdatePk = MergingController.Config.Tables[table.Key].UpdatePk;
                    }
                        
                }

                // -- Inserts
                for (var i = 0; i < table.Value.Inserts.Count; i++)
                {
                    var pkCount = table.Value.PrimaryKey?.Length ?? 0;
                    var insert = table.Value.Inserts[i];

                    // -- Rows
                    for (var r = 0; r < insert.Rows.Count; r++)
                    {
                        //Console.WriteLine($"----- table: {table} | insert: {i}/{table.Value.Inserts.Count} | row {r}/{insert.Rows.Count}");

                        // Removes element if in black box
                        if (pkCount == 1)
                        {
                            if (Register.Registers[insert.ID].InBlackBox(insert.Table, insert.Rows[r][pkId]))
                            {
                                Console.WriteLine($"!-- Skipping/Removing Black Box element: {insert.Rows[r][pkId]}");
                                insert.Rows.RemoveAt(r);
                                r--;
                                continue;
                            }
                        }

                        if (table.Value.Name == "catalog_product_super_attribute")
                        {
                            if (ConfigProductRegister.ConfigParent.Contains(insert.Rows[r][1]))
                            {
                                Register.Registers[insert.ID].AddToBlackBox(table.Value.Name, insert.Rows[r][0]);
                                insert.Rows.RemoveAt(r);
                                r--;
                                continue;
                            }
                        }

                        var sameValues = (config.WhenSame == ActionName.SkipCheck) ? (-1, null) : SameValues(table.Value, i, r, insert.Rows[r]);
                        if (sameValues.row != null && canDoMerge)
                        {
                            if (BindAction(
                                (sameValues.fileId == -999 ? ActionName.Ignore : config.WhenSame),
                                insert.Table,
                                sameValues.fileId,
                                insert.ID,
                                sameValues.row,
                                insert.Rows[r],
                                pkId
                            ))
                            {
                                insert.Rows.RemoveAt(r);
                                r--;
                            }

                            // If something was merged return true
                            if (config.WhenSame == ActionName.BindToThis || config.WhenSame == ActionName.Add)
                                didMerge = true;
                        }
                        else
                        {
                            if (table.Value.PrimaryKey != null && table.Value.PrimaryKey.Length == 1)
                            {
                                // NOTE: When Same IDs always adds
                                //(config.WhenIdSame == ActionName.SkipCheck) ? (-1, null) : 
                                var sameIds = SameId(table.Value, insert.Rows[r][pkId], table.Value.AddedCopy, firstTime);
                                if (sameIds != -1 && canUpdatePk)
                                {
                                    BindAction(
                                        //config.WhenIdSame, 
                                        ActionName.Add,
                                        insert.Table,
                                        sameIds, 
                                        insert.ID, 
                                        null,
                                        insert.Rows[r],
                                        pkId
                                    );
                                    didMerge = true;
                                }
                                // Registers new PK
                                else
                                {
                                    Register.Registers[insert.ID].AddPK(insert.Table, insert.Rows[r][pkId]);
                                }
                            }
                        }

                    }
                }

                lastIdDictionary[table.Key] = lastId;
            }

            return didMerge;
        }

        private static int GetLastId(string table, int pkId)
        {
            if (lastIdDictionary.ContainsKey(table))
                return lastIdDictionary[table];

            var maxId = 0;
            for (var f = 0; f < MergingController.rawFiles.Count; f++)
            {
                for (var i = 0; i < MergingController.rawFiles[f].Tables[table].Inserts.Count; i++)
                {
                    if (MergingController.rawFiles[f].Tables[table].Inserts[i].Rows.Count > 0)
                    {
                        try
                        {
                            var id = int.Parse(MergingController.rawFiles[f].Tables[table].Inserts[i].Rows[^1][pkId]);
                            if (id > maxId) maxId = id;
                        }
                        catch (Exception)
                        {
                            lastIdDictionary.Add(table, 1);
                            return 1;
                        }
                    }
                }
            }

            lastIdDictionary.Add(table, maxId + 1);
            return maxId + 1;
        }

        public static bool BindAction(ActionName action, string table, int targetId, int referenceId, List<string> targetRow, List<string> referenceRow, int pkId)
        {
            switch (action)
            {
                case ActionName.BindToThis:
                case ActionName.ReverseBind:
                    Register.Registers[referenceId].UpdatePk(table, referenceRow[pkId], targetRow[pkId]);
                    return true;
                case ActionName.Add:
                    Register.Registers[referenceId].UpdatePk(table, referenceRow[pkId], "" + lastId);
                    referenceRow[pkId] = "" + lastId;
                    lastId++;
                    break;
                case ActionName.Ignore:
                    Register.Registers[referenceId].AddToBlackBox(table, referenceRow[pkId]);
                    return true;
                case ActionName.SkipBoth:
                    Register.Registers[targetId].AddToBlackBox(table, targetRow[pkId]);
                    Register.Registers[referenceId].AddToBlackBox(table, referenceRow[pkId]);
                    return true;
                case ActionName.CopyInto:
                    for (var i = 1; i < targetRow.Count; i++)
                        targetRow[i] = referenceRow[i];
                    Register.Registers[referenceId].UpdatePk(table, referenceRow[pkId], targetRow[pkId]);
                    return true;
            }
            return false;
        }

        private static StringBuilder hash;
        private static string hashStr;
        private static int counterConfigItem = 0;

        private static (int fileId,List<string> row) SameValues(Table target, int insertId, int rowId, List<string> row)
        {
            if (cacheSameFields[target.Name].Count <= 0) 
                return (-1, null);

            // Step 1. Generate Hash Value
            hash = new StringBuilder();
            hash.Append(target.Name + "::");
            foreach (var cacheSameId in cacheSameFields[target.Name])
                hash.Append(row[cacheSameId.Value].ToLower() + "$__::__$");
                
            // Step 2. Search for hash
            hashStr = hash.ToString();
            var hashResult = HashRegister.Find(target.Name, hashStr);
            if (hashResult != null)
            {
                var didSomething = false;


                if (target.Name == "customer_entity")
                {
                    LogDuplicates.Log("customer-duplicate", row[2]);
                }
                else if (target.Name == "catalog_product_entity")
                {
                    var joiner = "";
                    if (row[2] == "'configurable'")
                    {
                        LogDuplicates.Log("product-duplicate", row[3]);
                        var prodRef = MergingController.rawFiles[hashResult.Value.FileId].Tables[target.Name]
                            .Inserts[hashResult.Value.InsertId].Rows[hashResult.Value.RowId];

                        if (prodRef[2] != "'simple'")
                            ConfigProductRegister.ConfigParent.Add(prodRef[0]);
                        else
                            prodRef[2] = "'configurable'";
                    }
                    else if(ConfigProductRegister.ProductList[target.ID].ContainsKey(int.Parse(row[0])))
                    {
                        LogDuplicates.Log("product-duplicate", row[3]);
                        //return (-999, new List<string>());
                        //joiner = "-cnf-";
                        //didSomething = true;
                    }

                    else if (row[2] == "'bundle'")
                    {
                        joiner = "-BND-";
                        didSomething = true;
                    }

                    if (didSomething)
                    {
                        var oldSku = row[3];
                        row[3] = "'" + Helper.RemoveTags(row[3]) + joiner + counterConfigItem++ + "'";
                        SkuRegister.Add(oldSku, row[3]);

                        hash = new StringBuilder();
                        hash.Append(target.Name + "::");
                        foreach (var cacheSameId in cacheSameFields[target.Name])
                            hash.Append(row[cacheSameId.Value].ToLower() + "$__::__$");
                        hashStr = hash.ToString();
                    }

                }

                if(!didSomething)
                {
                    Console.WriteLine($"----- Found same element with hash: {hashStr}");
                    return (hashResult.Value.FileId,
                        MergingController.rawFiles[hashResult.Value.FileId].Tables[target.Name]
                            .Inserts[hashResult.Value.InsertId].Rows[hashResult.Value.RowId]);
                }
            }
            // Step 3. If hash not found -> register it
            HashRegister.AddHashValue(target.Name, hashStr, new HashValue()
            {
                FileId = target.ID,
                InsertId = insertId,
                RowId = rowId
            });

            return (-1, null);
        }

        private static int SameId(Table target, string id, bool isCopied = false, bool firstTime = false)
        {
            if ((target.Name == "eav_attribute_option" || target.Name == "eav_attribute_option_value") &&
                int.Parse(id) < CustomAttributeOptionBuilder.LastId)
            {
                return 1;
            }

            if (isCopied)
                return firstTime ? 1 : -1;

            for (var f = 0; f < target.ID; f++)
            {
                if (Register.Registers[f].PrimaryKeys.ContainsKey(target.Name) &&
                    Register.Registers[f].PrimaryKeys[target.Name].ContainsKey(id))
                    return f;
            }

            return -1;
        }
    }
}
