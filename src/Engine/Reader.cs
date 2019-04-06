using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Mal;
using eValue = Mal.Types.eValue;
using eSymbol = Mal.Types.eSymbol;
using eList = Mal.Types.eList;
using eVector = Mal.Types.eVector;
using eHashMap = Mal.Types.eHashMap;
using eThrowable = Mal.Types.eThrowable;
using eContinue = Mal.Types.eContinue;

namespace Mal
{
    public class reader
    {

        public class ParseError : eThrowable
        {
            public ParseError(string msg) : base(msg) { }
        }

        public class Reader
        {
            List<string> lexemes;
            int position;

            public Reader(List<string> t)
            {
                lexemes = t;
                position = 0;
            }

            public string peek()
            {
                if (position >= lexemes.Count)
                {
                    return null;
                }
                else
                {
                    return lexemes[position];
                }
            }

            public string next()
            {
                return lexemes[position++];
            }
        }

        private static List<string> GetLexemes(string str)
        {
            List<string> tokens = new List<string>();
            string pattern = @"[\s ,]*(~@|[\[\]{}()'`~@]|""(?:[\\].|[^\\""])*""|;.*|[^\s \[\]{}()'""`~@,;]*)";
            Regex regex = new Regex(pattern);
            foreach (Match match in regex.Matches(str))
            {
                string token = match.Groups[1].Value;
                if ((token != null) && !(token == "") && !(token[0] == ';'))
                {
                    //Console.WriteLine("match: ^" + match.Groups[1] + "$");
                    tokens.Add(token);
                }
            }
            return tokens;
        }

        public static eValue read_atom(Reader rdr)
        {
            string token = rdr.next();
            string pattern = @"(^-?[0-9]+$)|(^-?[0-9][0-9.]*$)|(^nil$)|(^true$)|(^false$)|^("".*"")$|:(.*)|(^[^""]*$)";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(token);
            //Console.WriteLine("token: ^" + token + "$");
            if (!match.Success)
            {
                throw new ParseError("unrecognized token '" + token + "'");
            }
            if (match.Groups[1].Value != String.Empty)
            {
                return new Mal.Types.eInt(int.Parse(match.Groups[1].Value));
            }
            else if (match.Groups[3].Value != String.Empty)
            {
                return Mal.Types.Nil;
            }
            else if (match.Groups[4].Value != String.Empty)
            {
                return Mal.Types.True;
            }
            else if (match.Groups[5].Value != String.Empty)
            {
                return Mal.Types.False;
            }
            else if (match.Groups[6].Value != String.Empty)
            {
                string str = match.Groups[6].Value;
                str = str.Substring(1, str.Length - 2)
                    .Replace("\\\\", "\u029e")
                    .Replace("\\\"", "\"")
                    .Replace("\\n", "\n")
                    .Replace("\u029e", "\\");
                return new Mal.Types.eString(str);
            }
            else if (match.Groups[7].Value != String.Empty)
            {
                return new Mal.Types.eString("\u029e" + match.Groups[7].Value);
            }
            else if (match.Groups[8].Value != String.Empty)
            {
                return new Mal.Types.eSymbol(match.Groups[8].Value);
            }
            else
            {
                throw new ParseError("unrecognized '" + match.Groups[0] + "'");
            }
        }

        public static eValue read_list(Reader rdr, eList lst, char start, char end)
        {
            string token = rdr.next();

            if (token[0] != start)
            {
                throw new ParseError("expected '" + start + "'");
            }

            while ((token = rdr.peek()) != null && token[0] != end)
            {
                lst.conj_BANG(read_form(rdr));
            }

            if (token == null)
            {
                throw new ParseError("expected '" + end + "', got EOF");
            }

            rdr.next();

            return lst;
        }

        public static eValue read_hash_map(Reader rdr)
        {
            eList lst = (eList) read_list(rdr, new eList(), '{', '}');
            return new eHashMap(lst);
        }

        public static eValue read_form(Reader rdr)
        {
            string token = rdr.peek();
            if (token == null) { throw new eContinue(); }
            eValue form = null;

            switch (token)
            {
                case "'":
                    rdr.next();
                    return new eList(new eSymbol("quote"),
                        read_form(rdr));
                case "`":
                    rdr.next();
                    return new eList(new eSymbol("quasiquote"),
                        read_form(rdr));
                case "~":
                    rdr.next();
                    return new eList(new eSymbol("unquote"),
                        read_form(rdr));
                case "~@":
                    rdr.next();
                    return new eList(new eSymbol("splice-unquote"),
                        read_form(rdr));
                case "^":
                    rdr.next();
                    eValue meta = read_form(rdr);
                    return new eList(new eSymbol("with-meta"),
                        read_form(rdr),
                        meta);
                case "@":
                    rdr.next();
                    return new eList(new eSymbol("deref"),
                        read_form(rdr));

                case "(":
                    form = read_list(rdr, new eList(), '(', ')');
                    break;
                case ")":
                    throw new ParseError("unexpected ')'");
                case "[":
                    form = read_list(rdr, new eVector(), '[', ']');
                    break;
                case "]":
                    throw new ParseError("unexpected ']'");
                case "{":
                    form = read_hash_map(rdr);
                    break;
                case "}":
                    throw new ParseError("unexpected '}'");
                default:
                    form = read_atom(rdr);
                    break;
            }
            return form;
        }

        public static eValue read_str(string str)
        {
            return read_form(new Reader(GetLexemes(str)));
        }
    }
}
