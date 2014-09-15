using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Wolfram.NETLink;

namespace Wolfram.Grasshopper
{
    public class `Name` : GH_Component
    {
        public `Name`()
            : base("`Name`", "`Nickname`",
                "`Description`",
                "`Category`", "`Subcategory`")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            `RegisterInputParams`
            pManager.AddParameter(new LinkParam(), "link", "WL", "The link to the Wolfram Engine", GH_ParamAccess.item);
            pManager[pManager.ParamCount - 1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            `RegisterOutputParams`
            pManager.AddParameter(new LinkParam(), "link", "WL", "The link to the Wolfram Engine", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            `SolveInstance`
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{`GUID`}"); }
        }
    }
}
