using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SQLMerger.Cache;
using SQLMerger.Handlers;
using SQLMerger.Instance;
using SQLMerger.Interpreter;

namespace SQLMerger.Merger
{
    public class MergingController
    {
        public static Config.Config Config;

        public static List<FileInstance> rawFiles = new List<FileInstance>();

        public static bool FirstRun { get; set; } = true;

        public MergingController(Config.Config config) { Config = config; }

        public void Init()
        {
            foreach (var file in Config.Files)
            {
                foreach (var configTable in file.Tables)
                {
                    if (!string.IsNullOrEmpty(configTable.Value.PopulateWhiteBoxFromFile))
                    {
                        var path = configTable.Value.PopulateWhiteBoxFromFile;
                        using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var bs = new BufferedStream(fs);
                        using var sr = new StreamReader(bs);
                        string line;
                        string column = null;
                        var skus = new List<string>();
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (string.IsNullOrEmpty(column))
                            {
                                column = line;
                                continue;
                            }
                            skus.Add(line);
                        }
                        configTable.Value.WhiteBox.Add(column, skus);
                    }

                    foreach (var configColumn in configTable.Value.Columns)
                    {
                        if (!string.IsNullOrEmpty(configColumn.Value.PopulateChangeFromFile))
                        {
                            var path = configTable.Value.PopulateWhiteBoxFromFile;
                            using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            using var bs = new BufferedStream(fs);
                            using var sr = new StreamReader(bs);
                            string line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                var parts = line.Split(',');
                                configColumn.Value.Change.Add(parts[0], parts[1]);
                            }
                        }
                    }

                }
            }

            if (Config.Cache != null && Config.Cache.On)
            {
                CacheManager.BuildTablesToRemove(Config.Cache);
            }

            var id = 0;
            foreach (var file in Config.Files)
            {
                if (id == 0 && Config.Cache != null && 
                    Config.Cache.On && 
                    !string.IsNullOrEmpty(Config.Cache.BaseFileOverridePath))
                {
                    file.Path = Config.Cache.BaseFileOverridePath;
                }

                Register.Registers.Add(new FileRegister());

                var fi = new FileInterpreter();
                fi.Interpret(file.Path, id++);
                rawFiles.Add( (FileInstance)fi.GetData() );
            }
            SpamUsers.SaveLog();
        }

        public void Merge()
        {
            var outputFile = new FileInstance();

            //if (Config.Cache != null && Config.Cache.On)
            //{
            //    CacheManager.CleanUp(rawFiles);
            //}

            // Step 1. Build Tables
            for (var i = 0; i < rawFiles.Count; i++)
            {
                // Step 1.1 Apply each file config to each file
                ApplyConfigFile.Apply(Config.Files[i], rawFiles[i], Config);

                // Step 1.2 While Building Tables create register of TB/PK and TB/FK
                RegisterBuilder.BuildFk(rawFiles[i], i, Config);

                // Step 1.3 Merge tables
                //Merger.Merge(ref outputFile, rawFiles[i]);
            }

            // Custom Attribute Option Registration
            if (Config.Cache != null && !Config.Cache.On && Config.Cache.CustomAttributeOption != null &&
                Config.Cache.CustomAttributeOption.Count != 0)
            {
                CustomAttributeOptionBuilder.AddLastIdReference(rawFiles[0], Config.Cache.CustomAttributeOption);
            }

            // Merge file inserts
            for (var f = 0; f < Config.Files.Count; f++)
            {
                if (Config.Files[f].MergeFileInto == -1) continue;

                // Copies inserts into target file
                var targetFile = Config.Files[f].MergeFileInto;
                foreach (var table in rawFiles[f].Tables)
                {
                    rawFiles[targetFile].Tables[table.Key].Inserts.AddRange(table.Value.Inserts);
                }

                // Removes files from config and rawFile
                Config.Files.RemoveAt(f);
                rawFiles.RemoveAt(f);
                f--;
            }

            // Move Data
            for (var f = 0; f < Config.Files.Count; f++)
            {
                if (Config.Files[f].Tables == null) continue;

                foreach (var configTable in Config.Files[f].Tables)
                {
                    if (configTable.Value.MoveData == null || configTable.Value.MoveData.Count == 0)
                        continue;

                    if (!rawFiles[f].Tables.ContainsKey(configTable.Key))
                        continue;

                    MoveData.Move(configTable.Value.MoveData, rawFiles[f], rawFiles[f].Tables[configTable.Key]);
                }
            }

            // Copy Data
            for (var f = 0; f < Config.Files.Count; f++)
            {
                if(Config.Files[f].Tables == null) continue;

                foreach (var configTable in Config.Files[f].Tables)
                {
                    if(configTable.Value.CopyData == null || configTable.Value.CopyData.Count == 0)
                        continue;

                    if(!rawFiles[f].Tables.ContainsKey(configTable.Key))
                        continue;

                    CopyData.Copy(configTable.Value.CopyData, rawFiles[f].Tables[configTable.Key]);
                }
            }

            if (Config.Cache != null && Config.Cache.On)
            {
                CacheManager.Build(rawFiles, Config.Cache);
            }

            // Step 1.3 Merge tables
            Merger.Merge(ref outputFile, rawFiles[0]);

            // Config product register
            for (var f = 1; f < Config.Files.Count; f++)
            {
                if (rawFiles[f].Tables.ContainsKey("catalog_product_super_link"))
                {
                    ConfigProductRegister.Build(rawFiles[f].Tables["catalog_product_super_link"]);
                }
            }


            bool didMerge;
            var executionId = 0;
            do
            {
                foreach (var table in outputFile.Tables)
                {
                    var register = table.Value.PrimaryKey != null && table.Value.PrimaryKey.Length == 1;

                    if (!register)
                        continue;

                    var pkId = table.Value.GetColumnId(table.Value.PrimaryKey[0]);

                    for (var i = 0; i < table.Value.Inserts.Count; i++)
                    {
                        var rows = table.Value.Inserts[i].Rows;
                        for (var r = 0; r < rows.Count; r++)
                        {
                            Register.Registers[0].AddPK(table.Key, rows[r][pkId]);
                        }
                    }

                }

                HashRegister.register.Clear();
                var cacheSameFields = new Dictionary<string, Dictionary<string, int>>();
                foreach (var table in outputFile.Tables)
                {
                    var hash = Config.Tables.ContainsKey(table.Key) && Config.Tables[table.Key].SameOn != null;
                    if (!hash)
                        continue;

                    var comparedFields = Config.Tables[table.Key].SameOn;
                    if (comparedFields != null)
                    {
                        cacheSameFields.Add(table.Key, new Dictionary<string, int>());
                        foreach (var field in comparedFields)
                            cacheSameFields[table.Key].Add(field, table.Value.GetColumnId(field));
                    }

                    for (var i = 0; i < table.Value.Inserts.Count; i++)
                    {
                        var rows = table.Value.Inserts[i].Rows;
                        for (var r = 0; r < rows.Count; r++)
                        {
                            var hashName = new StringBuilder();
                            hashName.Append(table.Key + "::");
                            foreach (var cacheSameId in cacheSameFields[table.Key])
                                hashName.Append(rows[r][cacheSameId.Value].ToLower() + "$__::__$");
                            HashRegister.AddHashValue(table.Key, hashName.ToString(), new HashValue()
                            {
                                FileId = table.Value.ID,
                                InsertId = i,
                                RowId = r
                            });
                        }
                    }

                }

                didMerge = false;

                if (FirstRun)
                {
                    for (var f = 1; f < Config.Files.Count; f++)
                    {
                        foreach (var tableConfig in Config.Files[f].Tables)
                        {
                            if(tableConfig.Value.ForceMerge == null || tableConfig.Value.ForceMerge.Count == 0)
                                continue;

                            if(!rawFiles[f].Tables.ContainsKey(tableConfig.Key))
                                continue;

                            if (didMerge) ForceMerger.Merge(rawFiles[f].Tables[tableConfig.Key], tableConfig.Value.ForceMerge);
                            else didMerge = ForceMerger.Merge(rawFiles[f].Tables[tableConfig.Key], tableConfig.Value.ForceMerge);
                        }
                    }
                }

                for (var f = 1; f < rawFiles.Count; f++)
                {
                    if (didMerge) FileInsertMerger.Merge(rawFiles[f], FirstRun);
                    else didMerge = FileInsertMerger.Merge(rawFiles[f], FirstRun);
                }

                if (FirstRun)
                    didMerge = true;

                if(didMerge)
                    SameUsers.Run(outputFile, rawFiles, FirstRun);
                else
                    didMerge = SameUsers.Run(outputFile, rawFiles, FirstRun);

                if (!didMerge)
                    break;

                for (var f = 1; f < rawFiles.Count; f++)
                    FileBlackBoxRemover.Cleanup(rawFiles[f]);

                for (var f = 1; f < rawFiles.Count; f++)
                {
                    FileFkUpdater.UpdateFk(rawFiles[f], executionId);
                    FileFkUpdater.UpdateFkRule(rawFiles[f], executionId);
                }

                for (var f = 1; f < rawFiles.Count; f++)
                {
                    if (rawFiles[f].Tables.ContainsKey("catalog_category_entity"))
                    {
                        // Rebuild catalog_category_entity.path
                        foreach (var insert in rawFiles[f].Tables["catalog_category_entity"].Inserts)
                            CatalogCategoryPath.Run(insert);
                    }

                    if (rawFiles[f].Tables.ContainsKey("url_rewrite"))
                    {
                        // Rebuild url_rewrite.target_path
                        foreach (var insert in rawFiles[f].Tables["url_rewrite"].Inserts)
                            UrlRewriteTargetPath.Run(insert);
                    }
                }

                executionId++;

                Register.Registers[0].PrimaryKeys.Clear();
                Register.Registers[0].BlackBox.Clear();

                for (var f = 1; f < rawFiles.Count; f++)
                {
                    Register.Registers[f].RefreshPk();
                    Register.Registers[f].BlackBox.Clear();
                }

                if (FirstRun)
                    FirstRun = false;

            } while (didMerge);

            for (var f = 1; f < rawFiles.Count; f++)
            {
                if (rawFiles[f].Tables.ContainsKey("inventory_source_item"))
                {
                    SkuRegister.Update(rawFiles[f].Tables["inventory_source_item"]);
                }
            }


            SameUsers.SaveLog();

            // Custom Attribute Option Builder - removes tmp insert
            if (Config.Cache != null && !Config.Cache.On && Config.Cache.CustomAttributeOption != null &&
                Config.Cache.CustomAttributeOption.Count != 0)
            {
                CustomAttributeOptionBuilder.RemoveReferenceRows(rawFiles[0]);
            }



            for (var f = 1; f < rawFiles.Count; f++)
            {
                foreach (var table in rawFiles[f].Tables)
                {
                    //table.Value.BuildInsertColumnText();
                    outputFile.Tables[table.Key].Inserts.AddRange(table.Value.Inserts);
                }
            }

            foreach (var table in outputFile.Tables)
            {
                table.Value.BuildInsertColumnText();
            }

            if (Config.Handlers.ContainsKey("ProductSameUrl"))
            {
                SameUrlAddressFixer.Run(outputFile, Config.Handlers["ProductSameUrl"]);
            }

            // Custom Attribute Option Builder
            if (Config.Cache != null && !Config.Cache.On && Config.Cache.CustomAttributeOption != null &&
                Config.Cache.CustomAttributeOption.Count != 0)
            {
                CustomAttributeOptionBuilder.Build(outputFile, Config.Cache.CustomAttributeOption);
            }

            // Step 3. Write SQL
            using var file = new StreamWriter(Config.Output);

            if(Config.Append != null && !string.IsNullOrEmpty(Config.Append.StartBeforeSet))
                file.WriteLine(Config.Append.StartBeforeSet);

            file.WriteLine("SET FOREIGN_KEY_CHECKS=0;");
            file.WriteLine("SET NAMES utf8;");
            file.WriteLine("SET SESSION sql_mode='NO_AUTO_VALUE_ON_ZERO';");
            //file.WriteLine("SET GLOBAL max_allowed_packet=5073741824;");

            if (Config.Append != null && !string.IsNullOrEmpty(Config.Append.StartAfterSet))
                file.WriteLine(Config.Append.StartAfterSet);

            foreach (var fInstTable in outputFile.Tables)
            {
                fInstTable.Value.GetSql(in file);
            }

            if (Config.Append != null && !string.IsNullOrEmpty(Config.Append.EndBeforeSet))
                file.WriteLine(Config.Append.EndBeforeSet);

            file.WriteLine("SET FOREIGN_KEY_CHECKS=1;");
            // file.WriteLine("SET GLOBAL max_allowed_packet=16777216;");

            if (Config.Append != null && !string.IsNullOrEmpty(Config.Append.EndAfterSet))
            {
                if (Config.Append.DoReplace)
                    Config.Append.EndAfterSet = Config.Append.EndAfterSet.Replace("\\'", "'");
                file.WriteLine(Config.Append.EndAfterSet);
            }
        }
    }
}
