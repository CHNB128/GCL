using System;
using System.Collections.Generic;
using Mal;

namespace Mal {
    public class Types {
        //
        // Exceptions/Errors
        //
        public class eThrowable : Exception {
            public eThrowable () : base () { }
            public eThrowable (string message) : base (message) { }
        }

        public class eError : eThrowable {
            public eError (string message) : base (message) { }
        }

        public class eContinue : eThrowable { }

        // Thrown by throw function
        public class eException : eThrowable {
            eValue value;

            public eException (eValue value) {
                this.value = value;
            }

            public eException (string value) : base (value) {
                this.value = new eString (value);
            }

            public eValue getValue () { return value; }
        }

        //
        // General functions
        //
        public static bool _equal_Q (eValue a, eValue b) {
            Type ota = a.GetType (), otb = b.GetType ();
            if (!((ota == otb) ||
                    (a is eList && b is eList))) {
                return false;
            } else {
                if (a is eInt) {
                    return ((eInt) a).getValue () ==
                        ((eInt) b).getValue ();
                } else if (a is eSymbol) {
                    return ((eSymbol) a).getName () ==
                        ((eSymbol) b).getName ();
                } else if (a is eString) {
                    return ((eString) a).getValue () ==
                        ((eString) b).getValue ();
                } else if (a is eList) {
                    if (((eList) a).size () != ((eList) b).size ()) {
                        return false;
                    }
                    for (int i = 0; i < ((eList) a).size (); i++) {
                        if (!_equal_Q (((eList) a) [i], ((eList) b) [i])) {
                            return false;
                        }
                    }
                    return true;
                } else if (a is eHashMap) {
                    var akeys = ((eHashMap) a).getValue ().Keys;
                    var bkeys = ((eHashMap) b).getValue ().Keys;
                    if (akeys.Count != bkeys.Count) {
                        return false;
                    }
                    foreach (var k in akeys) {
                        if (!_equal_Q (((eHashMap) a).getValue () [k],
                                ((eHashMap) b).getValue () [k])) {
                            return false;
                        }
                    }
                    return true;
                } else {
                    return a == b;
                }
            }
        }

        public abstract class eValue {
            eValue meta = Nil;
            public virtual eValue copy () {
                return (eValue) this.MemberwiseClone ();
            }

            // Default is just to call regular toString()
            public virtual string ToString (bool print_readably) {
                return this.ToString ();
            }
            public eValue getMeta () { return meta; }
            public eValue setMeta (eValue m) { meta = m; return this; }
            public virtual bool list_Q () { return false; }
        }

        public class MalConstant : eValue {
            string value;
            public MalConstant (string name) { value = name; }
            public new MalConstant copy () { return this; }

            public override string ToString () {
                return value;
            }
            public override string ToString (bool print_readably) {
                return value;
            }
        }

        static public MalConstant Nil = new MalConstant ("nil");
        static public MalConstant True = new MalConstant ("true");
        static public MalConstant False = new MalConstant ("false");

        public class eInt : eValue {
            Int64 value;
            public eInt (Int64 v) { value = v; }
            public new eInt copy () { return this; }

            public Int64 getValue () { return value; }
            public override string ToString () {
                return value.ToString ();
            }
            public override string ToString (bool print_readably) {
                return value.ToString ();
            }
            public static MalConstant operator < (eInt a, eInt b) {
                return a.getValue () < b.getValue () ? True : False;
            }
            public static MalConstant operator <= (eInt a, eInt b) {
                return a.getValue () <= b.getValue () ? True : False;
            }
            public static MalConstant operator > (eInt a, eInt b) {
                return a.getValue () > b.getValue () ? True : False;
            }
            public static MalConstant operator >= (eInt a, eInt b) {
                return a.getValue () >= b.getValue () ? True : False;
            }
            public static eInt operator + (eInt a, eInt b) {
                return new eInt (a.getValue () + b.getValue ());
            }
            public static eInt operator - (eInt a, eInt b) {
                return new eInt (a.getValue () - b.getValue ());
            }
            public static eInt operator * (eInt a, eInt b) {
                return new eInt (a.getValue () * b.getValue ());
            }
            public static eInt operator / (eInt a, eInt b) {
                return new eInt (a.getValue () / b.getValue ());
            }
        }

        public class eSymbol : eValue {
            string value;
            public eSymbol (string v) { value = v; }
            public eSymbol (eString v) { value = v.getValue (); }
            public new eSymbol copy () { return this; }

            public string getName () { return value; }
            public override string ToString () {
                return value;
            }
            public override string ToString (bool print_readably) {
                return value;
            }
        }

        public class eString : eValue {
            string value;
            public eString (string v) { value = v; }
            public new eString copy () { return this; }

            public string getValue () { return value; }
            public override string ToString () {
                return "\"" + value + "\"";
            }
            public override string ToString (bool print_readably) {
                if (value.Length > 0 && value[0] == '\u029e') {
                    return ":" + value.Substring (1);
                } else if (print_readably) {
                    return "\"" + value.Replace ("\\", "\\\\")
                        .Replace ("\"", "\\\"")
                        .Replace ("\n", "\\n") + "\"";
                } else {
                    return value;
                }
            }
        }

        public class eList : eValue {
            public string start = "(", end = ")";
            List<eValue> value;

            public eList () {
                value = new List<eValue> ();
            }

            public eList (List<eValue> val) {
                value = val;
            }

            public eList (params eValue[] mvs) {
                value = new List<eValue> ();
                conj_BANG (mvs);
            }

            public List<eValue> getValue () { return value; }

            public override bool list_Q () { return true; }

            public override string ToString () {
                return start + printer.join (value, " ", true) + end;
            }

            public override string ToString (bool print_readably) {
                return start + printer.join (value, " ", print_readably) + end;
            }

            public eList conj_BANG (params eValue[] mvs) {
                for (int i = 0; i < mvs.Length; i++) {
                    value.Add (mvs[i]);
                }
                return this;
            }

            public int size () { return value.Count; }

            public eValue nth (int idx) {
                return value.Count > idx ? value[idx] : Nil;
            }

            public eValue this [int idx] {
                get { return value.Count > idx ? value[idx] : Nil; }
            }

            public eList rest () {
                if (size () > 0) {
                    return new eList (value.GetRange (1, value.Count - 1));
                } else {
                    return new eList ();
                }
            }

            public virtual eList slice (int start) {
                return new eList (value.GetRange (start, value.Count - start));
            }

            public virtual eList slice (int start, int end) {
                return new eList (value.GetRange (start, end - start));
            }

        }

        public class eVector : eList {
            // Same implementation except for instantiation methods
            public eVector () : base () {
                start = "[";
                end = "]";
            }

            public eVector (List<eValue> val) : base (val) {
                start = "[";
                end = "]";
            }

            public override bool list_Q () { return false; }

            public override eList slice (int start, int end) {
                var val = this.getValue ();
                return new eVector (val.GetRange (start, val.Count - start));
            }
        }

        public class eHashMap : eValue {
            Dictionary<string, eValue> value;
            public eHashMap (Dictionary<string, eValue> val) {
                value = val;
            }
            public eHashMap (eList lst) {
                value = new Dictionary<String, eValue> ();
                assoc_BANG (lst);
            }
            public new eHashMap copy () {
                var new_self = (eHashMap) this.MemberwiseClone ();
                new_self.value = new Dictionary<string, eValue> (value);
                return new_self;
            }

            public Dictionary<string, eValue> getValue () { return value; }

            public override string ToString () {
                return "{" + printer.join (value, " ", true) + "}";
            }
            public override string ToString (bool print_readably) {
                return "{" + printer.join (value, " ", print_readably) + "}";
            }

            public eHashMap assoc_BANG (eList lst) {
                for (int i = 0; i < lst.size (); i += 2) {
                    value[((eString) lst[i]).getValue ()] = lst[i + 1];
                }
                return this;
            }

            public eHashMap dissoc_BANG (eList lst) {
                for (int i = 0; i < lst.size (); i++) {
                    value.Remove (((eString) lst[i]).getValue ());
                }
                return this;
            }
        }

        public class MalAtom : eValue {
            eValue value;
            public MalAtom (eValue value) { this.value = value; }
            //public MalAtom copy() { return new MalAtom(value); }
            public eValue getValue () { return value; }
            public eValue setValue (eValue value) { return this.value = value; }
            public override string ToString () {
                return "(atom " + printer._pr_str (value, true) + ")";
            }
            public override string ToString (Boolean print_readably) {
                return "(atom " + printer._pr_str (value, print_readably) + ")";
            }
        }

        public class eFunction : eValue {
            Func<eList, eValue> fn = null;
            eValue ast = null;
            Mal.env.Env env = null;
            eList fparams;
            bool macro = false;
            public eFunction (Func<eList, eValue> fn) {
                this.fn = fn;
            }
            public eFunction (eValue ast, Mal.env.Env env, eList fparams,
                Func<eList, eValue> fn) {
                this.fn = fn;
                this.ast = ast;
                this.env = env;
                this.fparams = fparams;
            }

            public override string ToString () {
                if (ast != null) {
                    return "<fn* " + Mal.printer._pr_str (fparams, true) +
                        " " + Mal.printer._pr_str (ast, true) + ">";
                } else {
                    return "<builtin_function " + fn.ToString () + ">";
                }
            }

            public eValue apply (eList args) {
                return fn (args);
            }

            public eValue getAst () { return ast; }
            public Mal.env.Env getEnv () { return env; }
            public eList getFParams () { return fparams; }
            public Mal.env.Env genEnv (eList args) {
                return new Mal.env.Env (env, fparams, args);
            }
            public bool isMacro () { return macro; }
            public void setMacro () { macro = true; }

        }
    }
}