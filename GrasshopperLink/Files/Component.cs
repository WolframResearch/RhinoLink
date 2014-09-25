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
                        `ReadAndStoreResults`
                    }
                } else {
                    `ReadAndStoreResults`
                }
                // Last output item is always the link.
                DA.SetData(ghResultCount + 1, new LinkType(link));
            } catch (MathLinkException exc) {
                link.ClearError();
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "WSTPException communicating with Wolfram Engine while reading results: " + exc);
            } finally {
                link.NewPacket();
            }
        }


        private void SendInputParam(object arg, IKernelLink link, GH_ParamAccess accessType) {
        
             if (accessType == GH_ParamAccess.list) {
                 List<IGH_Goo> list = (List<IGH_Goo>) arg;
                 link.PutFunction("List", list.Count);
                 foreach (IGH_Goo obj in list)
                      link.Put(obj.ScriptVariable());
             } else if (accessType == GH_ParamAccess.tree) {
                 // TODO: Is this right? Do I need to worry about Invalid paths? Should I do this on a loopback link?
                 GH_Structure<IGH_Goo> structure = (GH_Structure<IGH_Goo>) arg;
                 IList<List<IGH_Goo>> data = structure.Branches;
                 link.PutFunction("List", data.Count);
                 foreach (List<IGH_Goo> row in data) {
                     link.PutFunction("List", row.Count);
                     foreach (IGH_Goo obj in row)
                         link.Put(obj.ScriptVariable());
                 }
             } else if (arg is IGH_Goo) {
                 link.Put(((IGH_Goo) arg).ScriptVariable());
             } else {
                 link.Put(arg);
             }
        }
        
        
        // M code must ensure that the only types that can arrive in this function are ones that are currently handled here.
        private bool ReadAndStoreResult(string type, int index, IKernelLink link, IGH_DataAccess DA, GH_ParamAccess accessType) {
        
            try {
                switch (accessType) {
                    case GH_ParamAccess.item: {
                        object res = ReadSingleItemAsType(type, link);
                        DA.SetData(index, res);
                        break;
                    }
                    case GH_ParamAccess.list: {
                        ILinkMark mark = link.CreateMark();
                        int length = 1;
                        bool isList = false;
                        try {
                            length = link.CheckFunction("List");
                            isList = true;
                        } catch (MathLinkException) {
                            link.ClearError();
                        } finally {
                            link.SeekMark(mark);
                            link.DestroyMark(mark);
                        }
                        if (isList) {
                            IList list = ReadListOfItemsAsType(type, link);
                            DA.SetDataList(index, list);
                        } else {
                            object item = ReadSingleItemAsType(type, link);
                            DA.SetData(index, item);
                        }
                        break;
                    }
                    case GH_ParamAccess.tree: {
                        // Quick and dirty method. Only support numbers. Read as Expr, then use Dimensions, etc.
                        // TODO: Handle objects. See how it is done in ReadSingleItemAsType().
                        // This read-as-expr method shows problems for tree type. It would be better, for tree, to not use
                        // the expr, but rather re-read the data from the link (as is done for item, list). In the curremt
                        // method, I don't see any good way to handle "Any".
                        // WAIT, IT'S WORSE. Expr.AsArray() only does Integer, Real, and only up to depth 2.
                        ILinkMark mark = link.CreateMark();
                        Expr e = link.GetExpr();
                        link.SeekMark(mark);
                        link.DestroyMark(mark);
                        int[] dims = e.Dimensions;
                        if (dims.Length == 0) {
                            object res = ReadSingleItemAsType(type, link);
                            DA.SetData(index, res);
                        } else if (dims.Length == 1) {
                            IList list = ReadListOfItemsAsType(type, link);
                            DA.SetDataList(index, list);
                        } else if (dims.Length == 2) {
                            // This code could be cleaner with GH_Structure, but I dont quite understand that class...
                            switch (type) {
                                case "Number": {
                                    DataTree<double> tree = new DataTree<double>();
                                    Array dataArray = e.AsArray(ExpressionType.Real, 2);
                                    for (int i = 0; i < dims[0]; i++) {
                                        GH_Path pth = new GH_Path(i);
                                        for (int j = 0; j < dims[1]; j++) {
                                            tree.Add((double) dataArray.GetValue(i,j), pth);
                                        }
                                    }
                                    DA.SetDataTree(index, tree);
                                    break;
                                }
                                case "Integer": {
                                    DataTree<int> tree = new DataTree<int>();
                                    Array dataArray = e.AsArray(ExpressionType.Integer, 2);
                                    for (int i = 0; i < dims[0]; i++) {
                                        GH_Path pth = new GH_Path(i);
                                        for (int j = 0; j < dims[1]; j++) {
                                            tree.Add((int) dataArray.GetValue(i,j), pth);
                                        }
                                    }
                                    DA.SetDataTree(index, tree);
                                    break;
                                }
                                default:
                                    // Can't handle this.
                                    return false;
                            }
                        } else {
                            // TODO. At least set a RuntimeMessage before returning false.
                            return false;
                        }
                        break;
                    
                    // Problem with the read-and-fill-element-by-element approach is that this is a GH_Structure<IGH_Goo>,
                    // so I need to create an IGH_Goo pbjectout of every piece of data. But there are zillions of IGH_Goo
                    // objects out there. I don't want to map every object to its IGH_Goo type (e.g., Circle --> GH_Circle).
                    // For lists, I could simply create a List<anything> and apparently it gets converted to the appopriate
                    // IGH_Goo object later on. But with trees, I don't see the corresponding technique. Compare the signature
                    // of DA.SetDataList() and DA.SetDataTree().
                    /*
                        ILinkMark mark3 = link.CreateMark();
                        int length = 1;
                        try {
                            length = link.CheckFunction("List");
                            // Don't catch; allow to fail. Must be a list arriving for tree results.
                        } finally {
                            link.DestroyMark(mark3);
                        }
                        GH_Structure<IGH_Goo> structure = new GH_Structure<IGH_Goo>();
                        GH_Path path = new GH_Path(0);
                        int pathLen = 1;
                        int pathIndex = 0;
                        for (int i = 0; i < length; i++) {
                            if (isListWaitingOnLink(link)) {
                                 path.
                            } else {
                                structure.Append(
                            }
                        }
                       */ 
                    }
                }
            } catch (Exception) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unexpected type of result from Wolfram Engine");
                link.ClearError();
                link.NewPacket();
                return false;
            }
            return true;
        }
        
        
        private object ReadSingleItemAsType(string type, IKernelLink link) {
        
            object res = null;
            switch (type) {
                case "Text": {
                    res = link.GetString();
                    break;
                }
                case "Integer": {
                    res = link.GetInteger();
                    break;
                }
                case "Number": {
                    res = link.GetDouble();
                    break;
                }
                case "Boolean": {
                    res = link.GetBoolean();
                    break;
                }
                case "Any": {
                    ILinkMark mark = link.CreateMark();
                    try {
                         res = link.GetObject();
                     } catch (MathLinkException) {
                         link.ClearError();
                         link.SeekMark(mark);
                         res = new ExprType(link.GetExpr());
                     } finally {
                         link.DestroyMark(mark);
                     }
                     break;
                }
                case "Expr": {
                    res = new ExprType(link.GetExpr());
                    break;
                }
            }
            return res;
        }


        private ArrayList ReadListOfItemsAsType(string type, IKernelLink link) {

            int length = link.CheckFunction("List");
            ArrayList res = new ArrayList(length);
            for (int i = 0; i < length; i++) {
                switch (type) {
                    case "Text": {
                        res.Add(link.GetString());
                        break;
                    }
                    case "Integer": {
                        res.Add(link.GetInteger());
                        break;
                    }
                    case "Number": {
                        res.Add(link.GetDouble());
                        break;
                    }
                    case "Boolean": {
                        res.Add(link.GetBoolean());
                        break;
                    }
                    case "Any": {
                        object obj = null;
                        ILinkMark mark = link.CreateMark();
                        try {
                             obj = link.GetObject();
                         } catch (MathLinkException) {
                             link.ClearError();
                             link.SeekMark(mark);
                             obj = new ExprType(link.GetExpr());
                         } finally {
                             link.DestroyMark(mark);
                         }
                         res.Add(obj);
                         break;
                    }
                    case "Expr": {
                        res.Add(new ExprType(link.GetExpr()));
                        break;
                    }
                }
            }
            return res;
        }
        
        
        
        private bool isListWaitingOnLink(IKernelLink link) {
        
            ILinkMark mark = link.CreateMark();
            bool isList = false;
            try {
                link.CheckFunction("List");
                isList = true;
            } catch (MathLinkException) {
                link.ClearError();
                link.SeekMark(mark);
            } finally {
                link.DestroyMark(mark);
            }
            return isList;
        }
        

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                System.Resources.ResourceManager rm = new System.Resources.ResourceManager("gh", typeof(`Name`).Assembly);
                byte[] array = (byte[]) rm.GetObject("Icon");
                return new System.Drawing.Bitmap(new System.IO.MemoryStream(array));
                //return (System.Drawing.Bitmap) obj;
                //return obj ==null ? null : new System.Drawing.Bitmap(@"C:\Users\tgayley\Documents\workspace\GrasshopperLink\Projects\WolframGrasshopperComponents\Resources\spikeyIcon.png");
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{`GUID`}"); }
        }
    }
}
