using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Wolfram.NETLink;

namespace Wolfram.Grasshopper
{
    public class ObjectComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ObjectComponent class.
        /// </summary>
        public ObjectComponent()
            : base("WolframObject", "WolframObject",
                "Description",
                "Wolfram", "")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("head", "H", "The head of the expression to compute", GH_ParamAccess.item);
            pManager.AddGenericParameter("obj", "O", "The head of the expression to compute", GH_ParamAccess.item);
            pManager.AddParameter(new LinkParam(), "link", "WL", "The link to the Wolfram Engine", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("expr", "E", "The Expr result", GH_ParamAccess.item);
            pManager.AddParameter(new LinkParam(), "link", "WL", "The link to the Wolfram Engine", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string head = null;
            object obj = null;
            LinkType linkType = null;

            if (!DA.GetData(0, ref head)) { return; }
            if (!DA.GetData(1, ref obj)) { return; }
            // Link arg is optional.
            DA.GetData(2, ref linkType);
 
            // If the retrieved data is Nothing, we need to abort.
            if (head == null || obj == null) { return; }

            IKernelLink ml = linkType != null ? linkType.Value : KernelLinkProvider.Link;

            ml.PutFunction("EvaluatePacket", 1);
            ml.PutFunction(head, 1);
            // TODO: For object args, we want to not always put as refs. Forexample, strings will be of type GH_String, so we need to 
            // convert to native types before sending, so they arrive in a sensible format in M.
            ml.Put(obj);
            ml.EndPacket();
            ml.WaitForAnswer();
            Expr result = ml.GetExpr();

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
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{b7d82532-c9f3-4f65-910f-5c09c84f67bc}"); }
        }
    }
}