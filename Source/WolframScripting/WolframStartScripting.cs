using System;
using System.Threading;

using Rhino;
using Rhino.Commands;

namespace Wolfram.Rhino
{
    [System.Runtime.InteropServices.Guid("fcb07020-5dd7-4dae-a832-facef0bc6c5c")]
    public class WolframStartScripting : Command
    {
        static WolframStartScripting _instance;
        public WolframStartScripting()
        {
            _instance = this;
        }

        ///<summary>The only instance of the WolframStartScripting command.</summary>
        public static WolframStartScripting Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "WolframConnect"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            WolframScriptingPlugIn.Instance.StartReaderThread();
            return Result.Success;
        }
    }
}
