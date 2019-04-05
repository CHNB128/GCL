using System;
using System.Collections.Generic;
using System.IO;
using eValue = Mal.Types.eValue;
using MalConstant = Mal.Types.MalConstant;
using eInt = Mal.Types.eInt;
using eSymbol = Mal.Types.eSymbol;
using eString = Mal.Types.eString;
using eList = Mal.Types.eList;
using eVector = Mal.Types.eVector;
using eHashMap = Mal.Types.eHashMap;
using MalAtom = Mal.Types.MalAtom;
using eFunction = Mal.Types.eFunction;

namespace Mal {
    public class core {
        static MalConstant Nil = Mal.Types.Nil;
        static MalConstant True = Mal.Types.True;
        static MalConstant False = Mal.Types.False;

        // Errors/Exceptions
        static public eFunction mal_throw = new eFunction (
            a => { throw new Mal.Types.eException (a[0]); });

        // Scalar functions
        static eFunction nil_Q = new eFunction (
            a => a[0] == Nil ? True : False);

        static eFunction true_Q = new eFunction (
            a => a[0] == True ? True : False);

        static eFunction false_Q = new eFunction (
            a => a[0] == False ? True : False);

        static eFunction symbol_Q = new eFunction (
            a => a[0] is eSymbol ? True : False);

        static eFunction string_Q = new eFunction (
            a => {
                if (a[0] is eString) {
                    var s = ((eString) a[0]).getValue ();
                    return (s.Length == 0 || s[0] != '\u029e') ? True : False;
                } else {
                    return False;
                }
            });

        static eFunction keyword = new eFunction (
            a => {
                if (a[0] is eString &&
                    ((eString) a[0]).getValue () [0] == '\u029e') {
                    return a[0];
                } else {
                    return new eString ("\u029e" + ((eString) a[0]).getValue ());
                }
            });

        static eFunction keyword_Q = new eFunction (
            a => {
                if (a[0] is eString) {
                    var s = ((eString) a[0]).getValue ();
                    return (s.Length > 0 && s[0] == '\u029e') ? True : False;
                } else {
                    return False;
                }
            });

        static eFunction number_Q = new eFunction (
            a => a[0] is eInt ? True : False);

        static eFunction function_Q = new eFunction (
            a => a[0] is eFunction && !((eFunction) a[0]).isMacro () ? True : False);

        static eFunction macro_Q = new eFunction (
            a => a[0] is eFunction && ((eFunction) a[0]).isMacro () ? True : False);

        // Number functions
        static eFunction time_ms = new eFunction (
            a => new eInt (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond));

        // String functions
        static public eFunction pr_str = new eFunction (
            a => new eString (printer._pr_str_args (a, " ", true)));

        static public eFunction str = new eFunction (
            a => new eString (printer._pr_str_args (a, "", false)));

        static public eFunction prn = new eFunction (
            a => {
                Console.WriteLine (printer._pr_str_args (a, " ", true));
                return Nil;
            });

        static public eFunction println = new eFunction (
            a => {
                Console.WriteLine (printer._pr_str_args (a, " ", false));
                return Nil;
            });

        static public eFunction mal_readline = new eFunction (
            a => {
                var line = readline.Readline (((eString) a[0]).getValue ());
                if (line == null) { return Types.Nil; } else { return new eString (line); }
            });

        static public eFunction read_string = new eFunction (
            a => reader.read_str (((eString) a[0]).getValue ()));

        static public eFunction slurp = new eFunction (
            a => new eString (File.ReadAllText (
                ((eString) a[0]).getValue ())));

        // List/Vector functions
        static public eFunction list_Q = new eFunction (
            a => a[0].GetType () == typeof (eList) ? True : False);

        static public eFunction vector_Q = new eFunction (
            a => a[0].GetType () == typeof (eVector) ? True : False);

        // HashMap functions
        static public eFunction hash_map_Q = new eFunction (
            a => a[0].GetType () == typeof (eHashMap) ? True : False);

        static eFunction contains_Q = new eFunction (
            a => {
                string key = ((eString) a[1]).getValue ();
                var dict = ((eHashMap) a[0]).getValue ();
                return dict.ContainsKey (key) ? True : False;
            });

        static eFunction assoc = new eFunction (
            a => {
                var new_hm = ((eHashMap) a[0]).copy ();
                return new_hm.assoc_BANG ((eList) a.slice (1));
            });

        static eFunction dissoc = new eFunction (
            a => {
                var new_hm = ((eHashMap) a[0]).copy ();
                return new_hm.dissoc_BANG ((eList) a.slice (1));
            });

        static eFunction get = new eFunction (
            a => {
                string key = ((eString) a[1]).getValue ();
                if (a[0] == Nil) {
                    return Nil;
                } else {
                    var dict = ((eHashMap) a[0]).getValue ();
                    return dict.ContainsKey (key) ? dict[key] : Nil;
                }
            });

        static eFunction keys = new eFunction (
            a => {
                var dict = ((eHashMap) a[0]).getValue ();
                eList key_lst = new eList ();
                foreach (var key in dict.Keys) {
                    key_lst.conj_BANG (new eString (key));
                }
                return key_lst;
            });

        static eFunction vals = new eFunction (
            a => {
                var dict = ((eHashMap) a[0]).getValue ();
                eList val_lst = new eList ();
                foreach (var val in dict.Values) {
                    val_lst.conj_BANG (val);
                }
                return val_lst;
            });

        // Sequence functions
        static public eFunction sequential_Q = new eFunction (
            a => a[0] is eList ? True : False);

        static eFunction cons = new eFunction (
            a => {
                var lst = new List<eValue> ();
                lst.Add (a[0]);
                lst.AddRange (((eList) a[1]).getValue ());
                return (eValue) new eList (lst);
            });

        static eFunction concat = new eFunction (
            a => {
                if (a.size () == 0) { return new eList (); }
                var lst = new List<eValue> ();
                lst.AddRange (((eList) a[0]).getValue ());
                for (int i = 1; i < a.size (); i++) {
                    lst.AddRange (((eList) a[i]).getValue ());
                }
                return (eValue) new eList (lst);
            });

        static eFunction nth = new eFunction (
            a => {
                var idx = (int) ((eInt) a[1]).getValue ();
                if (idx < ((eList) a[0]).size ()) {
                    return ((eList) a[0]) [idx];
                } else {
                    throw new Mal.Types.eException (
                        "nth: index out of range");
                }
            });

        static eFunction first = new eFunction (
            a => a[0] == Nil ? Nil : ((eList) a[0]) [0]);

        static eFunction rest = new eFunction (
            a => a[0] == Nil ? new eList () : ((eList) a[0]).rest ());

        static eFunction empty_Q = new eFunction (
            a => ((eList) a[0]).size () == 0 ? True : False);

        static eFunction count = new eFunction (
            a => {
                return (a[0] == Nil) ?
                    new eInt (0) :
                    new eInt (((eList) a[0]).size ());
            });

        static eFunction conj = new eFunction (
            a => {
                var src_lst = ((eList) a[0]).getValue ();
                var new_lst = new List<eValue> ();
                new_lst.AddRange (src_lst);
                if (a[0] is eVector) {
                    for (int i = 1; i < a.size (); i++) {
                        new_lst.Add (a[i]);
                    }
                    return new eVector (new_lst);
                } else {
                    for (int i = 1; i < a.size (); i++) {
                        new_lst.Insert (0, a[i]);
                    }
                    return new eList (new_lst);
                }
            });

        static eFunction seq = new eFunction (
            a => {
                if (a[0] == Nil) {
                    return Nil;
                } else if (a[0] is eVector) {
                    return (((eVector) a[0]).size () == 0) ?
                        (eValue) Nil :
                        new eList (((eVector) a[0]).getValue ());
                } else if (a[0] is eList) {
                    return (((eList) a[0]).size () == 0) ?
                        Nil :
                        a[0];
                } else if (a[0] is eString) {
                    var s = ((eString) a[0]).getValue ();
                    if (s.Length == 0) {
                        return Nil;
                    }
                    var chars_list = new List<eValue> ();
                    foreach (var c in s) {
                        chars_list.Add (new eString (c.ToString ()));
                    }
                    return new eList (chars_list);
                }
                return Nil;
            });

        // General list related functions
        static eFunction apply = new eFunction (
            a => {
                var f = (eFunction) a[0];
                var lst = new List<eValue> ();
                lst.AddRange (a.slice (1, a.size () - 1).getValue ());
                lst.AddRange (((eList) a[a.size () - 1]).getValue ());
                return f.apply (new eList (lst));
            });

        static eFunction map = new eFunction (
            a => {
                eFunction f = (eFunction) a[0];
                var src_lst = ((eList) a[1]).getValue ();
                var new_lst = new List<eValue> ();
                for (int i = 0; i < src_lst.Count; i++) {
                    new_lst.Add (f.apply (new eList (src_lst[i])));
                }
                return new eList (new_lst);
            });

        // Metadata functions
        static eFunction meta = new eFunction (
            a => a[0].getMeta ());

        static eFunction with_meta = new eFunction (
            a => ((eValue) a[0]).copy ().setMeta (a[1]));

        // Atom functions
        static eFunction atom_Q = new eFunction (
            a => a[0] is MalAtom ? True : False);

        static eFunction deref = new eFunction (
            a => ((MalAtom) a[0]).getValue ());

        static eFunction reset_BANG = new eFunction (
            a => ((MalAtom) a[0]).setValue (a[1]));

        static eFunction swap_BANG = new eFunction (
            a => {
                MalAtom atm = (MalAtom) a[0];
                eFunction f = (eFunction) a[1];
                var new_lst = new List<eValue> ();
                new_lst.Add (atm.getValue ());
                new_lst.AddRange (((eList) a.slice (2)).getValue ());
                return atm.setValue (f.apply (new eList (new_lst)));
            });

        static public Dictionary<string, eValue> ns =
            new Dictionary<string, eValue> {
                {
                "=",
                new eFunction (
                a => Mal.Types._equal_Q (a[0], a[1]) ? True : False)
                },
                { "throw", mal_throw },
                { "nil?", nil_Q },
                { "true?", true_Q },
                { "false?", false_Q },
                { "symbol", new eFunction (a => new eSymbol ((eString) a[0])) },
                { "symbol?", symbol_Q },
                { "string?", string_Q },
                { "keyword", keyword },
                { "keyword?", keyword_Q },
                { "number?", number_Q },
                { "fn?", function_Q },
                { "macro?", macro_Q },

                { "pr-str", pr_str },
                { "str", str },
                { "prn", prn },
                { "println", println },
                { "readline", mal_readline },
                { "read-string", read_string },
                { "slurp", slurp },
                { "<", new eFunction (a => (eInt) a[0] < (eInt) a[1]) },
                { "<=", new eFunction (a => (eInt) a[0] <= (eInt) a[1]) },
                { ">", new eFunction (a => (eInt) a[0] > (eInt) a[1]) },
                { ">=", new eFunction (a => (eInt) a[0] >= (eInt) a[1]) },
                { "+", new eFunction (a => (eInt) a[0] + (eInt) a[1]) },
                { "-", new eFunction (a => (eInt) a[0] - (eInt) a[1]) },
                { "*", new eFunction (a => (eInt) a[0] * (eInt) a[1]) },
                { "/", new eFunction (a => (eInt) a[0] / (eInt) a[1]) },
                { "time-ms", time_ms },

                { "list", new eFunction (a => new eList (a.getValue ())) },
                { "list?", list_Q },
                { "vector", new eFunction (a => new eVector (a.getValue ())) },
                { "vector?", vector_Q },
                { "hash-map", new eFunction (a => new eHashMap (a)) },
                { "map?", hash_map_Q },
                { "contains?", contains_Q },
                { "assoc", assoc },
                { "dissoc", dissoc },
                { "get", get },
                { "keys", keys },
                { "vals", vals },

                { "sequential?", sequential_Q },
                { "cons", cons },
                { "concat", concat },
                { "nth", nth },
                { "first", first },
                { "rest", rest },
                { "empty?", empty_Q },
                { "count", count },
                { "conj", conj },
                { "seq", seq },
                { "apply", apply },
                { "map", map },

                { "with-meta", with_meta },
                { "meta", meta },
                { "atom", new eFunction (a => new MalAtom (a[0])) },
                { "atom?", atom_Q },
                { "deref", deref },
                { "reset!", reset_BANG },
                { "swap!", swap_BANG },
            };
    }
}