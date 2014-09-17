using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Wolfram.NETLink;


namespace Wolfram.Grasshopper
{
    public class CodeComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CodeComponent class.
        /// </summary>
        public CodeComponent()
            : base("CodeComponent", "Code",
                "Input arbitrary Wolfram Language code as a string.",
                "Wolfram", "")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("code", "C", "The Wolfram Language code to execute", GH_ParamAccess.item);
            pManager.AddParameter(new LinkParam(), "link", "WL", "The link to the Wolfram Engine", GH_ParamAccess.item);
            pManager[1].Optional = true;
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

            IKernelLink ml = linkType != null ? linkType.Value : KernelLinkProvider.Link;

            ml.PutFunction("EvaluatePacket", 1);
            ml.PutFunction("ToExpression", 1);
            ml.Put(code);
            ml.EndPacket();

            Expr result = null;
            try
            {
                ml.WaitForAnswer();
                result = ml.GetExpr();
            }
            catch (MathLinkException)
            {
                ml.ClearError();
                ml.NewPacket();
                return;
            }

            DA.SetData(0, new ExprType(result));
            DA.SetData(1, new LinkType(ml));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                System.Resources.ResourceManager temp = new System.Resources.ResourceManager("WolframGrasshopperComponents.Properties.Resources", typeof(ComputeComponent).Assembly);
                object obj = temp.GetObject("SpikeyIcon");
                return ((System.Drawing.Bitmap)(obj));
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{74a15279-032a-4dc6-aff9-bcf5890ad443}"); }
        }
    }
}