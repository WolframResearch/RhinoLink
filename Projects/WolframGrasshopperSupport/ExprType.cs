using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wolfram.NETLink;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace Wolfram.Grasshopper {


public class ExprType : GH_Goo<Expr> {

    public ExprType() {
        Value = null;
    }

    public ExprType(Expr e) {
        Value = e;
    }

    // Copy Constructor
    public ExprType(ExprType source) {
        Value = source.Value;
    }

    public override IGH_Goo Duplicate() {
        return new ExprType(this);
    }

    // I think this determines whether an input will fire. I don't want unassigned Exprs to fire, right?
    public override bool IsValid {
        get { return Value != null; }
    }

    public override string TypeName {
        get { return "Expr"; }
    }

    public override string TypeDescription {
        get { return "An arbitrary Wolfram Engine expression"; }
    }

    public override string ToString() {
        return Value.ToString();
    }

    //////////////////////////////  CastTo  ////////////////////////////////////

    public override bool CastTo<Q>(ref Q target)  {

        if (Value.IntegerQ()) {
            if (typeof(Q).IsAssignableFrom(typeof(Int32))) {
                object i = (int) Value;
                target = (Q)i;
                return true;
            } else if (typeof(Q).IsAssignableFrom(typeof(Int64))) {
                object i = Value.AsInt64();
                target = (Q)i;
                return true;
            } else if (typeof(Q).IsAssignableFrom(typeof(GH_Integer))) {
                object ptr = new GH_Integer((int) Value.AsInt64());
                target = (Q)ptr;
                return true;
            } else if (typeof(Q).IsAssignableFrom(typeof(Double))) {
                object d = Value.AsDouble();
                target = (Q)d;
                return true;
            } else if (typeof(Q).IsAssignableFrom(typeof(GH_Number))) {
                object d = new GH_Number(Value.AsDouble());
                target = (Q)d;
                return true;
            }
        }
        if (Value.NumberQ()) {
            if (typeof(Q).IsAssignableFrom(typeof(Double))) {
                object d = Value.AsDouble();
                target = (Q)d;
                return true;
            }
            else if (typeof(Q).IsAssignableFrom(typeof(GH_Number))) {
                object d = new GH_Number(Value.AsDouble());
                target = (Q)d;
                return true;
            }
        }
        if (Value.StringQ() || Value.SymbolQ()) {
            if (typeof(Q).IsAssignableFrom(typeof(string))) {
                object s = Value.ToString();
                target = (Q)s;
                return true;
            } else if (typeof(Q).IsAssignableFrom(typeof(GH_String))) {
                object s = new GH_String(Value.ToString());
                target = (Q)s;
                return true;
            }
        }
        if (Value.TrueQ() || Value.SymbolQ() && Value.ToString() == "False") {
            if (typeof(Q).IsAssignableFrom(typeof(bool))) {
                object s = Value.TrueQ() ? true : false;
                target = (Q)s;
                return true;
            } else if (typeof(Q).IsAssignableFrom(typeof(GH_Boolean))) {
                object s = new GH_Boolean(Value.TrueQ() ? true : false);
                target = (Q)s;
                return true;
            }
        }
        // Try to convert to an array.
        Array data = null;
        try
        {
            if (Value.Head.Equals(SYM_LIST))
            {
                int[] dims = Value.Dimensions;
                if (dims.Length == 1)
                {
                    try
                    {
                        data = Value.AsArray(ExpressionType.Integer, 1);
                    }
                    catch (Exception)
                    {
                        data = Value.AsArray(ExpressionType.Real, 1);
                    }
                }
                else if (dims.Length == 2)
                {
                }
            }
        }
        catch (Exception)
        {
        }
        // TODO: More types, like ComplexNumber.

        return false;
    }

    private static readonly Expr SYM_LIST = new Expr(ExpressionType.Symbol, "List");

    /////////////////////////////////////////  CastFrom  //////////////////////////////////////////

    public override bool CastFrom(object source) {

        //Abort immediately on bogus data.
        if (source == null) { return false; }

        //Type t = source.GetType();

        // I don't yet understand this method very well. If I use a recommended conversion like:
        //    GH_Convert.ToInt32(source, out val, GH_Conversion.Both)
        // I think I will get reals converted to ints (?). Likewise, if I do GH_Convert.ToDouble(), I think
        // I will get integers converted to reals. I don't want either of these, so instead I first try
        // inspecting the raw type. LAter I fall through to GH_Convert methods.

        // I don't know what types Grasshopper will ever give me. Maybe it never uses float, for example? What about, say, char or uint?
        if (source is int || source is long || source is float || source is double || source is bool || source is string) {
            Value = new Expr(source);
            return true;
        } else if (source is GH_Integer) {
            int val;
            GH_Convert.ToInt32(source, out val, GH_Conversion.Both);
            this.Value = new Expr(val);
            return true;
        }  else if (source is GH_Number) {
            double val;
            GH_Convert.ToDouble(source, out val, GH_Conversion.Both);
            this.Value = new Expr(val);
            return true;
        } else if (source is GH_Boolean) {
            bool val;
            GH_Convert.ToBoolean(source, out val, GH_Conversion.Both);
            this.Value = new Expr(val);
            return true;
        }

        // Try a series of conversions. 
        double dval;
        if (GH_Convert.ToDouble(source, out dval, GH_Conversion.Both)) {
            this.Value = new Expr(dval);
            return true;
        }

       int ival;
        if (GH_Convert.ToInt32(source, out ival, GH_Conversion.Both)) {
            this.Value = new Expr(ival);
            return true;
        }

        string str = null;
        if (GH_Convert.ToString(source, out str, GH_Conversion.Both)) {
            this.Value = new Expr(str);
            return true;
        }

        return false;
    }

}


}
