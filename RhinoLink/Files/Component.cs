using System;
using System.Collections;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

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
            pManager.AddParameter(new LinkParam(), "link", "seq", "The link to the Wolfram Engine", GH_ParamAccess.item);
            pManager[pManager.ParamCount - 1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            `RegisterOutputParams`
            pManager.AddParameter(new ExprParam(), "Expr result", "expr", "The entire result, as an Expr, for debugging", GH_ParamAccess.item);
            pManager.AddParameter(new LinkParam(), "link", "seq", "The link to the Wolfram Engine", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            `GetData`
            
            // Final, optional link input param is always present.
            LinkType linkType = null;
            DA.GetData(`InputArgCount`, ref linkType);
            
            IKernelLink link = linkType != null ? linkType.Value : Utils.GetLink();
            
            // Send initialization code and saved defs.
            try {
                if (`UseInit`) {
                    link.Evaluate("ReleaseHold[\"" + `Initialization` + "\"]");
                    link.WaitAndDiscardAnswer();
                }
                if (`UseDefs`) {
                    link.Evaluate(`Definitions`);
                    link.WaitAndDiscardAnswer();
                }    
            } catch (MathLinkException exc) {
                link.ClearError();
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "WSTPException communicating with Wolfram Engine: " + exc);
                return;
            }
            
            // Now begin sending the actual computation.
            try {
                link.PutFunction("EvaluatePacket", 1);
                link.PutNext(ExpressionType.Function);
                link.PutArgCount(`InputArgCount`);
                
                if (`HeadIsSymbol`) {
                    link.PutSymbol("`Func`");
                } else {
                    // pure function
                    link.PutFunction("ToExpression", 1);
                    link.Put("`Func`");
                }
                
                `SendInputParams`
                
                link.EndPacket();
                link.WaitForAnswer();
            } catch (MathLinkException exc) {
                link.ClearError();
                link.NewPacket();
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "WSTPException communicating with Wolfram Engine: " + exc);
                return;
            }
            
            // This is one part of a (likely) temporary feature that puts the Expr result on the second output. 
            Expr debuggingExpr = link.PeekExpr();
            
            // Read the result(s) and store them in the DA object.
            ILinkMark mark = link.CreateMark();
            // TODO: ghResultCount is not used, but if we are going to allow multiple outputs, then we have to protect against
            // users specifying more than one, but only one arrives. This will hang reading the second one. 
            int ghResultCount = 0;
            bool isGHResult = false;
            try {
                ghResultCount = link.CheckFunction("GHResult") - 1;
                isGHResult = true;
            } catch (MathLinkException) {
                link.SeekMark(mark);
            } finally {
                link.DestroyMark(mark);
            }
            try {
                if (isGHResult) {
                    `ReadAndStoreResults`
                } else {
                    `ReadAndStoreResults`
                }
                
                // This is the second part of the temporary feature that puts the entire result as an Expr on a second output port.
                DA.SetData(++ghResultCount, new ExprType(debuggingExpr));
                
                // Last output item is always the link.
                DA.SetData(ghResultCount + 1, new LinkType(link));
            } catch (MathLinkException exc) {
                link.ClearError();
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "WSTPException communicating with Wolfram Engine while reading results: " + exc);
            } finally {
                link.NewPacket();
            }
        }


                

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                System.Resources.ResourceManager rm = new System.Resources.ResourceManager("gh", typeof(`Name`).Assembly);
                byte[] array = (byte[]) rm.GetObject("Icon");
                return new System.Drawing.Bitmap(new System.IO.MemoryStream(array));
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{`GUID`}"); }
        }
    }
}
