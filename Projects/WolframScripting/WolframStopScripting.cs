using System;
using Rhino;
using Rhino.Commands;

namespace Wolfram.Rhino
{
    [System.Runtime.InteropServices.Guid("1d22d85d-1455-426e-af9a-184cd3b9630e")]
    public class WolframStopScripting : Command
    {
        static WolframStopScripting _instance;
        public WolframStopScripting()
        {
            _instance = this;
        }

        ///<summary>The only instance of the WolframStopScripting command.</summary>
        public static WolframStopScripting Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "WolframStopScripting"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            WolframScriptingPlugIn.Instance.StopReaderThread();
            return Result.Success;
        }
    }
}
