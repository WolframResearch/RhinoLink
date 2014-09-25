using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Grasshopper.Kernel;
using Rhino;

using Wolfram.NETLink;

namespace Wolfram.Grasshopper
{
    
    public class Utils {


        private static IKernelLink link = null;
        private static object pluginLock = new object();

        // This is the property that GH components use to acquire the link to the kernel, which is
        // held in the WolframScripting Rhino plugin.
        public static IKernelLink GetLink() {
            lock (pluginLock)
            {
                if (link == null)
                    link = (IKernelLink) RhinoApp.GetPlugInObject("WolframScripting");
                return link;
            }
        }


        public static object readArbitraryResult(IKernelLink ml, GH_Component component) {

            try {
                ml.WaitForAnswer();
            } catch (MathLinkException) {
                ml.ClearError();
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error on the link.");
                // TODO: What to do here??
            }

            Object exprOrObjectResult = null;
            ILinkMark mark = ml.CreateMark();
            try {
                exprOrObjectResult = ml.GetObject();
            } catch (MathLinkException) {
                ml.ClearError();
                ml.SeekMark(mark);
                // Technically, GetExpr() should never throw MathLinkException. But there was at least one bug I found and fixed where it did. 
                // So to be safe we leave this try/catch here, otherwise the link will get in a bad state.
                try {
                    Expr ex = ml.GetExpr();
                    exprOrObjectResult = new ExprType(ex);
                } catch (MathLinkException) {
                    ml.ClearError();
                    component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error reading result as an Expr. This is a bug in GrasshopperLink, not the user program.");
                }
            } finally {
                ml.DestroyMark(mark);
                ml.NewPacket();
            }

            return exprOrObjectResult;
        }
    }
}
