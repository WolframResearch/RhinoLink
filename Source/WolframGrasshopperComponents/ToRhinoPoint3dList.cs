using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Wolfram.NETLink;


namespace Wolfram.Grasshopper
{
    public class ToRhinoPoint3DList : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CodeComponent class.
        /// </summary>
        public ToRhinoPoint3DList()
            : base("Point3d List", "Point3d List",
                "Convert a Wolfram Language list of 3D points to a Rhino list of Point3d",
                "Wolfram", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("E", "E", "WL Expr that evaluates to a list of 3D points", GH_ParamAccess.item);
            pManager.AddParameter(new LinkParam(), "l", "L", "The link to the Wolfram Engine", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("P", "P", "List of Rhino Points", GH_ParamAccess.list);
            pManager.AddParameter(new LinkParam(), "L", "L", "The link to the Wolfram Engine", GH_ParamAccess.item);
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
            ml.PutFunction("Wolfram`Rhino`WolframScriptingPlugIn`ToRhinoPoint3dArray", 1);
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

            if (!Utils.ReadAndStoreResult("Any", 0, ml, DA, GH_ParamAccess.list, this))
                return;

            DA.SetData(1, new LinkType(ml));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                System.Resources.ResourceManager temp = new System.Resources.ResourceManager("WolframGrasshopperComponents.Resources", typeof(ToRhinoPoint3DList).Assembly);
                object obj = temp.GetObject("p3dIcon");
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
            get { return new Guid("{74a15279-032a-4dc6-aff9-bcf5890ad443}"); }
        }
    }
}