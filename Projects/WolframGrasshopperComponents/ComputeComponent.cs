using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Wolfram.NETLink;

namespace Wolfram.Grasshopper
{
    public class ComputeComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ComputeComponent()
            : base("WolframCompute", "WolframCompute",
                "Computes arbitrary one-arg functions",
                "Wolfram", "")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ExprParam(), "head", "H", "The head of the expression to compute", GH_ParamAccess.item);
            pManager.AddParameter(new ExprParam(), "arg1", "A1", "The first argument of the expression to compute (optional)", GH_ParamAccess.item);
            pManager.AddParameter(new LinkParam(), "link", "WL", "The link to the Wolfram Engine", GH_ParamAccess.item);
            // ONE ARG FOR NOW
            //pManager.AddParameter(new ExprParam(), "arg2", "A2", "The second argument of the expression to compute (optional)", GH_ParamAccess.item);
            //pManager.AddParameter(new ExprParam(), "arg3", "A3", "The third argument of the expression to compute (optional)", GH_ParamAccess.item);

            pManager[2].Optional = true;
            //pManager[2].Optional = true;
            //pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ExprParam(), "result", "R", "The result", GH_ParamAccess.item);
            pManager.AddParameter(new LinkParam(), "link", "WL", "The link to the Wolfram Engine", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ExprType head = null;
            ExprType arg1 = null;
            LinkType linkType = null;
            //ExprType arg2 = null;
            //ExprType arg3 = null;
            //int argCount = 0;

            // Use the DA object to retrieve the data inside the first input parameter.
            // If the retieval fails (for example if there is no data) we need to abort.
            if (!DA.GetData(0, ref head)) { return; }
            if (!DA.GetData(1, ref arg1)) { return; }
            // Link arg is optional.
            DA.GetData(2, ref linkType);

            // If the retrieved data is Nothing, we need to abort.
            if (head == null || arg1 == null) { return; }

            IKernelLink ml = linkType != null ? linkType.Value : KernelLinkProvider.Link;

            // Total hack here: Treat arriving strings as symbols for the head (but not args).
            // Need a better way to differentiate strings and symbols. Perhaps a WolframSymbol component that takes a string and emits a symbol.
            Expr h = head.Value;
            if (h.StringQ()) {
                string headStr = h.ToString();
                // Ugly: need to strip off the " chars wrapped arond the string by ToString(). Expr really needs an AsString() method like J/Link.
                h = new Expr(ExpressionType.Symbol, headStr.Substring(1, headStr.Length-2));
            }

            Expr e = new Expr(h, new object[] { arg1.Value });

            String ss = e.ToString();

            ml.Evaluate(e);
            ml.WaitForAnswer();
            Expr result = ml.GetExpr();

            DA.SetData(0, new ExprType(result));
            DA.SetData(1, new LinkType(ml));
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
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                // WORKS:
                //return WolframGrasshopperComponents.Properties.Resources.Spikey;

                // Also works.
                // I think you can point the ResourceManage ctor at a .resources file
                System.Resources.ResourceManager temp = new System.Resources.ResourceManager("WolframGrasshopperComponents.Properties.Resources", typeof(ComputeComponent).Assembly);
                object obj = temp.GetObject("SpikeyIcon");
                return ((System.Drawing.Bitmap)(obj));
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{e6d6bc9a-9462-4794-b093-e5512c287a42}"); }
        }
    }
}
