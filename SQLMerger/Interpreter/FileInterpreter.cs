using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SQLMerger.Config;
using SQLMerger.Instance;

namespace SQLMerger.Interpreter
{
    public class FileInterpreter : IInterpreter
    {
        private FileInstance fileInst;

        public FileInterpreter()
        {
            fileInst = new FileInstance();
        }

        public bool Interpret(object data, int ID)
        {
            var path = (string) data;
            var lines = new List<string>();
            var isTableOpen = false;
            var tb = new TableInterpreter();

            using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var bs = new BufferedStream(fs);
            using var sr = new StreamReader(bs);
            string line;


            while ((line = sr.ReadLine()) != null)
            {
                // Empty Line
                if(line.Length < 2)
                    continue;

                // Comment Lines
                if(line[0] == '-' && line[1] == '-')
                    continue;
                if(line[0] == '/' && line[1] == '*')
                    continue;

                if (isTableOpen)
                {
                    lines.Add(line);
                    if (line.StartsWith(") ENGINE=", StringComparison.CurrentCultureIgnoreCase))
                    {
                        isTableOpen = false;
                    }
                    continue;
                }

                if (line.StartsWith("CREATE TABLE", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (lines.Count > 0)
                    {
                        if (tb.Interpret(lines, ID))
                        {
                            AddTable((Table)tb.GetData());
                        }
                        lines.Clear();

                    }
                    lines.Add(line);
                    isTableOpen = true;
                }
                else if (line.StartsWith("INSERT INTO", StringComparison.CurrentCultureIgnoreCase))
                {
                    lines.Add(line);
                }
            }

            if (lines.Count <= 0) return true;
            if (tb.Interpret(lines, ID))
            {
                AddTable((Table)tb.GetData());
            }
            return true;
        }

        private void AddTable(Table tb)
        {
            if (tb == null)
                return;

            fileInst.Tables.Add(tb.Name, tb);
        }

        public object GetData()
        {
            return fileInst;
        }
    }
}
