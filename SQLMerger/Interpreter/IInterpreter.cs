using System;
using System.Collections.Generic;
using System.Text;

namespace SQLMerger.Interpreter
{
    public interface IInterpreter
    {
        public bool Interpret(object data, int ID);
        public object GetData();
    }
}
