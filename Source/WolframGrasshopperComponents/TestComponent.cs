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
    public class TestComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TestComponent class.
        /// </summary>
        public TestComponent()
            : base("TestComponent", "Nickname",
                "Description",
                "Wolfram", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
    pManager.AddGenericParameter("pts", "pts", "", GH_ParamAccess.list );
pManager.AddIntegerParameter("num", "num", "", GH_ParamAccess.item );
pManager.AddNumberParameter("r", "r", "", GH_ParamAccess.item );

            pManager.AddParameter(new LinkParam(), "link", "WL", "The link to the Wolfram Engine", GH_ParamAccess.item);
            pManager[pManager.ParamCount - 1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("pts", "pts", "", GH_ParamAccess.tree );
            pManager.AddParameter(new ExprParam(), "Expr result", "expr", "The entire result, as an Expr, for debugging", GH_ParamAccess.item);
            pManager.AddParameter(new LinkParam(), "link", "WL", "The link to the Wolfram Engine", GH_ParamAccess.item);
       }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
           List<IGH_Goo> arg1 = new List<IGH_Goo>();
if (!DA.GetDataList(0, arg1)) return;
int arg2 = 0;
if (!DA.GetData(1, ref arg2)) return;
                         if (arg2 == null) return;
double arg3 = 0.0;
if (!DA.GetData(2, ref arg3)) return;
                         if (arg3 == null) return;

            
            // Final, optional link input param is always present.
            LinkType linkType = null;
            DA.GetData(3, ref linkType);
            
            IKernelLink link = linkType != null ? linkType.Value : Utils.GetLink();
            
            // Send initialization code and saved defs.
            try {
                if (false) {
                    link.Evaluate("ReleaseHold[\"" + "" + "\"]");
                    link.WaitAndDiscardAnswer();
                }
                if (true) {
                    link.Evaluate("InterpolatedPhyllotaxicSurfacePointsWrapper[pts_, n_:1000, r_:1] := InterpolatedPhyllotaxicSurfacePoints[Map[{#1[X], #1[Y], #1[Z]} & , pts], n, r]\n \n InterpolatedPhyllotaxicSurfacePoints[pts_, n_:1000, r_:1] := With[{f = Interpolation[Sort[Reverse /@ pts], Method -> \"Spline\"]}, PhyllotaxicSurfacePoints[{f[t], t}, {t, Min[Last /@ pts], Max[Last /@ pts]}, n, r]]\n \n Attributes[InterpolatedPhyllotaxicSurfacePoints] = {HoldAll}\n \n PhyllotaxicSurfacePoints[{x_, z_}, {t_, t0_, t1_}, n_:1000, r_:1] := Block[{\\[Gamma] = 2*Pi*GoldenRatio, aFunc, tFunc, totalArea}, aFunc = AreaFunction[{x, z}, {t, t0, t1}]; totalArea = aFunc[t1]; tFunc = Interpolation[Table[{aFunc[t], t}, {t, t0, t1, N[(t1 - t0)/400]}]]; {(With[{t = tFunc[totalArea*(#1/n)], \\[Theta] = #1*\\[Gamma]}, {x*Cos[\\[Theta]], x*Sin[\\[Theta]], z}] & ) /@ Range[n], r*Sqrt[totalArea/(n*Pi)]}]\n \n Attributes[PhyllotaxicSurfacePoints] = {HoldAll}\n \n AreaFunction[{x_, z_}, {t_, t0_, t1_}] := Block[{y}, y /. First[NDSolve[{Derivative[1][y][t] == 2.*Pi*x*Sqrt[D[x, t]^2 + D[z, t]^2], y[0] == 0}, y, {t, t0, t1}]]]\n \n Attributes[AreaFunction] = {HoldAll}");
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
                link.PutArgCount(3);
                
                if (true) {
                    link.PutSymbol("InterpolatedPhyllotaxicSurfacePointsWrapper");
                } else {
                    // pure function
                    link.PutFunction("ToExpression", 1);
                    link.Put("InterpolatedPhyllotaxicSurfacePointsWrapper");
                }
                
                Utils.SendInputParam(arg1, link, GH_ParamAccess.list, false);
Utils.SendInputParam(arg2, link, GH_ParamAccess.item, false);
Utils.SendInputParam(arg3, link, GH_ParamAccess.item, false);

                
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
            int ghResultCount = 0;
            bool isGHResult = false;
            try {
                ghResultCount = link.CheckFunction("GHResult");
                isGHResult = true;
            } catch (MathLinkException) {
                link.SeekMark(mark);
            } finally {
                link.DestroyMark(mark);
            }
            try {
                if (isGHResult) {
                    for (int i = 0; i < ghResultCount; i++) {
                        if (!Utils.ReadAndStoreResult("Any", 0, link, DA, GH_ParamAccess.tree, this)) return;

                    }
                } else {
                    if (!Utils.ReadAndStoreResult("Any", 0, link, DA, GH_ParamAccess.tree, this)) return;

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
            get { return new Guid("{57ed71a0-1e8f-4e47-823c-67be628c090a}"); }
        }
    }
}