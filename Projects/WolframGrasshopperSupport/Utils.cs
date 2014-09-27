using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino;

using Wolfram.NETLink;

namespace Wolfram.Grasshopper
{
    
    public class Utils {


        private static IKernelLink link = null;
        private static object pluginLock = new object();

        // This is the property that GH components use to acquire the link to the kernel, which is
        // held in the WolframScripting Rhino plugin.
        public static IKernelLink GetLink() {
            lock (pluginLock)
            {
                if (link == null)
                    link = (IKernelLink) RhinoApp.GetPlugInObject("WolframScripting");
                return link;
            }
        }


        public static object readArbitraryResult(IKernelLink ml, GH_Component component) {

            Object exprOrObjectResult = null;
            ILinkMark mark = ml.CreateMark();
            try {
                exprOrObjectResult = ml.GetObject();
            } catch (MathLinkException) {
                ml.ClearError();
                ml.SeekMark(mark);
                // Technically, GetExpr() should never throw MathLinkException. But there was at least one bug I found and fixed where it did. 
                // So to be safe we leave this try/catch here, otherwise the link will get in a bad state.
                try {
                    Expr ex = ml.GetExpr();
                    exprOrObjectResult = new ExprType(ex);
                } catch (MathLinkException) {
                    ml.ClearError();
                    component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error reading result as an Expr. This is a bug in GrasshopperLink, not the user program.");
                }
            } finally {
                ml.DestroyMark(mark);
                ml.NewPacket();
            }

            return exprOrObjectResult;
        }


        public static void SendInputParam(object arg, IKernelLink link, GH_ParamAccess accessType, bool isFunctionHead)
        {
            // ScriptVariable() is the method that extracts the wrapped Rhino object from the GH_Goo wrapper. e.g., GH_Text --> string,
            // or GH_Circle to Rhino.Geometry.Circle.
            if (accessType == GH_ParamAccess.list)
            {
                List<IGH_Goo> list = (List<IGH_Goo>)arg;
                link.PutFunction("List", list.Count);
                foreach (IGH_Goo obj in list) {
                    object o = obj.ScriptVariable();
                    link.Put(o);
                }
            }
            else if (accessType == GH_ParamAccess.tree)
            {
                // TODO: Is this right? Do I need to worry about Invalid paths? Should I do this on a loopback link?
                GH_Structure<IGH_Goo> structure = (GH_Structure<IGH_Goo>)arg;
                IList<List<IGH_Goo>> data = structure.Branches;
                link.PutFunction("List", data.Count);
                foreach (List<IGH_Goo> row in data)
                {
                    link.PutFunction("List", row.Count);
                    foreach (IGH_Goo obj in row) {
                        object o = obj.ScriptVariable();
                        link.Put(o);
                    }
                }
            }
            else
            {
                object native = (arg is IGH_Goo) ? ((IGH_Goo)arg).ScriptVariable() : arg;
                // isFunctionHead lets us treat strings as symbols or pure functions for the head (but not used for args).
                // Need a better way to differentiate strings and symbols. Perhaps a WolframSymbol component that takes a string and emits a symbol.
                if (isFunctionHead && (native is string || native is Expr && ((Expr)native).StringQ())) {
                    link.PutFunction("ToExpression", 1);
                    link.Put(native);
                } else {
                    link.Put(native);
                }
            }
        }


        // M code must ensure that the only types that can arrive in this function are ones that are currently handled here.
        public static bool ReadAndStoreResult(string type, int index, IKernelLink link, IGH_DataAccess DA, GH_ParamAccess accessType, GH_Component component)
        {

            try
            {
                switch (accessType)
                {
                    case GH_ParamAccess.item:
                        {
                            object res = ReadSingleItemAsType(type, link);
                            DA.SetData(index, res);
                            break;
                        }
                    case GH_ParamAccess.list:
                        {
                            ILinkMark mark = link.CreateMark();
                            int length = 1;
                            bool isList = false;
                            try
                            {
                                length = link.CheckFunction("List");
                                isList = true;
                            }
                            catch (MathLinkException)
                            {
                                link.ClearError();
                            }
                            finally
                            {
                                link.SeekMark(mark);
                                link.DestroyMark(mark);
                            }
                            if (isList)
                            {
                                IList list = ReadListOfItemsAsType(type, link);
                                DA.SetDataList(index, list);
                            }
                            else
                            {
                                object item = ReadSingleItemAsType(type, link);
                                DA.SetData(index, item);
                            }
                            break;
                        }
                    case GH_ParamAccess.tree:
                        {
                            // Quick and dirty method. Only support numbers. Read as Expr, then use Dimensions, etc.
                            // TODO: Handle objects. See how it is done in ReadSingleItemAsType().
                            // This read-as-expr method shows problems for tree type. It would be better, for tree, to not use
                            // the expr, but rather re-read the data from the link (as is done for item, list). In the curremt
                            // method, I don't see any good way to handle "Any".
                            // WAIT, IT'S WORSE. Expr.AsArray() only does Integer, Real, and only up to depth 2.
                            ILinkMark mark = link.CreateMark();
                            try
                            {
                                Expr e = link.GetExpr();
                                int[] dims = e.Dimensions;
                                if (dims.Length == 0)
                                {
                                    link.SeekMark(mark);
                                    object res = ReadSingleItemAsType(type, link);
                                    DA.SetData(index, res);
                                }
                                else if (dims.Length == 1)
                                {
                                    link.SeekMark(mark);
                                    IList list = ReadListOfItemsAsType(type, link);
                                    DA.SetDataList(index, list);
                                }
                                else if (dims.Length == 2)
                                {
                                    // This code could be cleaner with GH_Structure, but I dont quite understand that class...
                                    switch (type)
                                    {
                                        case "Number":
                                            {
                                                DataTree<double> tree = new DataTree<double>();
                                                Array dataArray = e.AsArray(ExpressionType.Real, 2);
                                                for (int i = 0; i < dims[0]; i++)
                                                {
                                                    GH_Path pth = new GH_Path(i);
                                                    for (int j = 0; j < dims[1]; j++)
                                                    {
                                                        tree.Add((double)dataArray.GetValue(i, j), pth);
                                                    }
                                                }
                                                DA.SetDataTree(index, tree);
                                                break;
                                            }
                                        case "Integer":
                                            {
                                                DataTree<int> tree = new DataTree<int>();
                                                Array dataArray = e.AsArray(ExpressionType.Integer, 2);
                                                for (int i = 0; i < dims[0]; i++)
                                                {
                                                    GH_Path pth = new GH_Path(i);
                                                    for (int j = 0; j < dims[1]; j++)
                                                    {
                                                        tree.Add((int)dataArray.GetValue(i, j), pth);
                                                    }
                                                }
                                                DA.SetDataTree(index, tree);
                                                break;
                                            }
                                        default:
                                            // Can't handle this.
                                            return false;
                                    }
                                }
                                else
                                {
                                    // TODO. At least set a RuntimeMessage before returning false.
                                    return false;
                                }
                            }
                            finally
                            {
                                link.DestroyMark(mark);
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
            }
            catch (Exception)
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unexpected type of result from Wolfram Engine");
                link.ClearError();
                link.NewPacket();
                return false;
            }
            return true;
        }


        private static object ReadSingleItemAsType(string type, IKernelLink link)
        {

            object res = null;
            switch (type)
            {
                case "Text":
                    {
                        res = link.GetString();
                        break;
                    }
                case "Integer":
                    {
                        res = link.GetInteger();
                        break;
                    }
                case "Number":
                    {
                        res = link.GetDouble();
                        break;
                    }
                case "Boolean":
                    {
                        res = link.GetBoolean();
                        break;
                    }
                case "Any":
                    {
                        res = ReadAsAny(link);
                        break;
                    }
                case "Expr":
                    {
                        res = new ExprType(link.GetExpr());
                        break;
                    }
            }
            return res;
        }


        private static ArrayList ReadListOfItemsAsType(string type, IKernelLink link)
        {
            int length = link.CheckFunction("List");
            ArrayList res = new ArrayList(length);
            for (int i = 0; i < length; i++)
            {
                switch (type)
                {
                    case "Text":
                        {
                            res.Add(link.GetString());
                            break;
                        }
                    case "Integer":
                        {
                            res.Add(link.GetInteger());
                            break;
                        }
                    case "Number":
                        {
                            res.Add(link.GetDouble());
                            break;
                        }
                    case "Boolean":
                        {
                            res.Add(link.GetBoolean());
                            break;
                        }
                    case "Any":
                        {
                            object obj = ReadAsAny(link);
                            res.Add(obj);
                            break;
                        }
                    case "Expr":
                        {
                            res.Add(new ExprType(link.GetExpr()));
                            break;
                        }
                }
            }
            return res;
        }
        
        // Special method for reading results typed as "Any". We want native objects for simple types like int, double,
        // arbitrary object refs as objects (let Grasshopper internals worry about wrapping in IGH_Goo), and Expr for anything else.
        // Note that we don't have to worry here about list/tree access type, because this is only called either for 
        // leaf elements, or to get whole nested lists as a single item for DataAccess.item slots.
        private static object ReadAsAny(IKernelLink link) {

            ILinkMark mark = link.CreateMark();
            try {
                ExpressionType leafExprType = link.GetNextExpressionType();
                link.SeekMark(mark);
                switch (leafExprType) {
                    case ExpressionType.Integer:
                        return link.GetInteger();
                    case ExpressionType.Real:
                        return link.GetDouble();
                    case ExpressionType.String:
                    case ExpressionType.Symbol:
                        return link.GetString();
                    case ExpressionType.Object:
                        return link.GetObject();
                    case ExpressionType.Boolean:
                        return link.GetBoolean();
                    case ExpressionType.Complex:
                        // TODO: Don't have any idea how this is handled elsewhere
                        return link.GetComplex();
                    case ExpressionType.Function:
                        return new ExprType(link.GetExpr());
                    default:
                        // Just to satisfy the compiler.
                        return null;
                }
            } finally {
                link.DestroyMark(mark);
            }
        }
        
        
    }
}
