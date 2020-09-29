using System;
using System.Collections.Generic;
using System.Text;

namespace SQLMerger.Config
{
    public class BaseConfig
    {
        //public int IdOffset { get; set; }
        public ActionName WhenSame { get; set; } = ActionName.BindToThis;
        public ActionName WhenLike { get; set; } = ActionName.Add;
        public ActionName WhenIdSame { get; set; } = ActionName.Add;
        //public bool Cache { get; set; }
        //public bool Log { get; set; }
    }
}
