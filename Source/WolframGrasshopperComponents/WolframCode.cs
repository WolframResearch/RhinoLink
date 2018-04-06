using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Wolfram.NETLink;


namespace Wolfram.Grasshopper
{
    public class ToRhinoPoint3dList : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CodeComponent class.
        /// </summary>
        public ToRhinoPoint3dList()
            : base("Wolfram Code", "Wolfram Code",
                "Input arbitrary Wolfram Language code as a string.",
                "Wolfram", "")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("code", "code", "The Wolfram Language code to execute", GH_ParamAccess.item);
            pManager.AddParameter(new LinkParam(), "link", "link", "The link to the Wolfram Engine", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("result", "res", "The result", GH_ParamAccess.item);
            pManager.AddParameter(new ExprParam(), "Expr result", "expr", "The entire result, as an Expr, for debugging", GH_ParamAccess.item);
            pManager.AddParameter(new LinkParam(), "link", "link", "The link to the Wolfram Engine", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string code = null;
            LinkType linkType = null;

            // Use the DA object to retrieve the data inside the first input parameter.
            // If the retieval fails (for example if there is no data) we need to abort.
            if (!DA.GetData(0, ref code)) { return; }
            // Link arg is optional.
            DA.GetData(1, ref linkType);

            // If the retrieved data is Nothing, we need to abort.
            if (code == null) { return; }

            IKernelLink ml = linkType != null ? linkType.Value : Utils.GetLink();

            ml.PutFunction("EvaluatePacket", 1);
            ml.PutFunction("ToExpression", 1);
            ml.Put(code);
            ml.EndPacket();
            
            try {
                ml.WaitForAnswer();
            } catch (MathLinkException) {
                ml.ClearError();
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error on the link.");
                ml.NewPacket();
                return;
            }

            // This is the (likely) temporary feature that puts the Expr result on the second output. 
            Expr debuggingExpr = ml.PeekExpr();
            DA.SetData(1, new ExprType(debuggingExpr));

            if (!Utils.ReadAndStoreResult("Any", 0, ml, DA, GH_ParamAccess.item, this))
                return;

            // 2 is the index here because 1 was used (temporarily) for the debugging feature above.
            DA.SetData(2, new LinkType(ml));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                System.Resources.ResourceManager temp = new System.Resources.ResourceManager("WolframGrasshopperComponents.Properties.Resources", typeof(WolframCodeComponent).Assembly);
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
            get { return new Guid("{aa15eaab-5801-421a-a937-bfdfa4768b64}"); }
        }
    }
}