using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using Wolfram.NETLink;

namespace Wolfram.Grasshopper
{
    public class WolframFComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ObjectComponent class.
        /// </summary>
        public WolframFComponent()
            : base("Wolfram F[]", "Wolfram F[]",
                "Computes arbitrary zero-arg functions",
                "Wolfram", "")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("head", "head", "The head of the function being computed", GH_ParamAccess.item);
            pManager.AddParameter(new LinkParam(), "link", "link", "The link to the Wolfram Engine", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("result", "res", "The result of the Wolfram Engine computation", GH_ParamAccess.item);
            pManager.AddParameter(new LinkParam(), "link", "link", "The link to the Wolfram Engine", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            IGH_Goo head = null;
            LinkType linkType = null;

            if (!DA.GetData(0, ref head)) { return; }
            // Link arg is optional.
            DA.GetData(2, ref linkType);

            // If the retrieved data is Nothing, we need to abort.
            if (head == null) { return; }

            // ScriptVariable() is the method that extracts the wrapped Rhino object from the GH_Goo wrapper. e.g., GH_Text --> string,
            // or GH_Circle to Rhino.Geometry.Circle.
            object nativeHead = head.ScriptVariable();

            IKernelLink ml = linkType != null ? linkType.Value : Utils.GetLink();

            ml.PutFunction("EvaluatePacket", 1);
            ml.PutNext(ExpressionType.Function);
            ml.PutArgCount(0);
            // Total hack here: Treat arriving strings as symbols for the head (but not args).
            // Need a better way to differentiate strings and symbols. Perhaps a WolframSymbol component that takes a string and emits a symbol.
            if (nativeHead is string || nativeHead is Expr && ((Expr)nativeHead).StringQ()) {
                ml.PutFunction("ToExpression", 1);
                ml.Put(nativeHead);
            } else {
                ml.Put(nativeHead);
            }
            ml.EndPacket();

            object exprOrObjectResult = Utils.readArbitraryResult(ml, this);
            if (exprOrObjectResult == null)
                return;

            // We spit out either an Expr or an object.
            DA.SetData(0, exprOrObjectResult);
            DA.SetData(1, new LinkType(ml));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                System.Resources.ResourceManager temp = new System.Resources.ResourceManager("WolframGrasshopperComponents.Properties.Resources", typeof(WolframFComponent).Assembly);
                object obj = temp.GetObject("SpikeyIcon");
                return ((System.Drawing.Bitmap)(obj));
            }
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{c6d82532-c9f3-4f65-920f-5c09c84117bc}"); }
        }
    }
}