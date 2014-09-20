using System;
using System.Collections.Generic;
using System.Threading;

using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoPlugIns = Rhino.PlugIns;

namespace Wolfram.Rhino
{
    [System.Runtime.InteropServices.Guid("ce5c7dea-93fd-4d2d-8ad7-d37eafb6d3e1")]
    public class WolframScriptingCommand : Command
    {
        public WolframScriptingCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static WolframScriptingCommand Instance
        {
            get;
            private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "WolframScriptingCommand"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {


            //System.Windows.Forms.MessageBox.Show("hello");

            RhinoApp.WriteLine("WriteLine output from my plugin 2");
            string[] pluginNames = RhinoPlugIns.PlugIn.GetInstalledPlugInNames();
            RhinoApp.WriteLine("Number of installed plugins: {0}", pluginNames.Length);
            foreach (string s in pluginNames)
                RhinoApp.WriteLine(s);

            /*
            // TODO: start here modifying the behaviour of your command.
            // ---
            RhinoApp.WriteLine("The {0} command will add a line right now.", EnglishName);

            Point3d pt0;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("Please select the start point");
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No start point was selected.");
                    return getPointAction.CommandResult();
                }
                pt0 = getPointAction.Point();
            }

            Point3d pt1;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("Please select the end point");
                getPointAction.SetBasePoint(pt0, true);
                getPointAction.DynamicDraw +=
                  (sender, e) => e.Display.DrawLine(pt0, e.CurrentPoint, System.Drawing.Color.DarkRed);
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No end point was selected.");
                    return getPointAction.CommandResult();
                }
                pt1 = getPointAction.Point();
            }

            doc.Objects.AddLine(pt0, pt1);
            doc.Views.Redraw();
            RhinoApp.WriteLine("The {0} command added one line to the document.", EnglishName);

            // ---
            */
            return Result.Success;
        }
    }
}
