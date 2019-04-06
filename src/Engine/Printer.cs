using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Mal;
using eValue = Mal.Types.eValue;
using eList = Mal.Types.eList;

namespace Mal
{
    public class printer
    {
        public static string join(List<eValue> value,
            string delim, bool print_readably)
        {
            List<string> strs = new List<string>();
            foreach (eValue mv in value)
            {
                strs.Add(mv.ToString(print_readably));
            }
            return String.Join(delim, strs.ToArray());
        }

        public static string join(Dictionary<string, eValue> value,
            string delim, bool print_readably)
        {
            List<string> strs = new List<string>();
            foreach (KeyValuePair<string, eValue> entry in value)
            {
                if (entry.Key.Length > 0 && entry.Key[0] == '\u029e')
                {
                    strs.Add(":" + entry.Key.Substring(1));
                }
                else if (print_readably)
                {
                    strs.Add("\"" + entry.Key.ToString() + "\"");
                }
                else
                {
                    strs.Add(entry.Key.ToString());
                }
                strs.Add(entry.Value.ToString(print_readably));
            }
            return String.Join(delim, strs.ToArray());
        }

        public static string _pr_str(eValue mv, bool print_readably)
        {
            return mv.ToString(print_readably);
        }

        public static string _pr_str_args(eList args, String sep,
            bool print_readably)
        {
            return join(args.getValue(), sep, print_readably);
        }

        public static string escapeString(string str)
        {
            return Regex.Escape(str);
        }

    }
}
