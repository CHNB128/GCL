using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Evil;
using eValue = Evil.Types.eValue;
using eSymbol = Evil.Types.eSymbol;
using eList = Evil.Types.eList;
using eVector = Evil.Types.eVector;
using eHashMap = Evil.Types.eHashMap;
using eThrowable = Evil.Types.eThrowable;
using eContinue = Evil.Types.eContinue;

public class Reader
{
    List<string> lexemes;
    int position;

    public Reader(List<string> lexemes)
    {
        this.lexemes = lexemes;
        this.position = 0;
    }

    public string peek()
    {
        return (position >= lexemes.Count) ? null : lexemes[position];
    }

    public string next()
    {
        return lexemes[position++];
    }
}

namespace Evil
{
    public class Lexer
    {

        private class ParseError : eThrowable
        {
            public ParseError(string msg) : base(msg) { }
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

        private static eValue ReadAtom(Reader reader)
        {
            string token = reader.next();
            string pattern = @"(^-?[0-9]+$)|(^-?[0-9][0-9.]*$)|(^nil$)|(^true$)|(^false$)|^("".*"")$|:(.*)|(^[^""]*$)";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(token);

            if (!match.Success)
                throw new ParseError("unrecognized token '" + token + "'");
            if (match.Groups[1].Value != String.Empty)
                return new Evil.Types.eInt(int.Parse(match.Groups[1].Value));
            else if (match.Groups[3].Value != String.Empty)
                return Evil.Types.Nil;
            else if (match.Groups[4].Value != String.Empty)
                return Evil.Types.True;
            else if (match.Groups[5].Value != String.Empty)
                return Evil.Types.False;
            else if (match.Groups[6].Value != String.Empty)
            {
                string str = match.Groups[6].Value;
                str = str.Substring(1, str.Length - 2)
                    .Replace("\\\\", "\u029e")
                    .Replace("\\\"", "\"")
                    .Replace("\\n", "\n")
                    .Replace("\u029e", "\\");
                return new Evil.Types.eString(str);
            }
            else if (match.Groups[7].Value != String.Empty)
                return new Evil.Types.eString("\u029e" + match.Groups[7].Value);
            else if (match.Groups[8].Value != String.Empty)
                return new Evil.Types.eSymbol(match.Groups[8].Value);
            else
                throw new ParseError("unrecognized '" + match.Groups[0] + "'");
        }

        private static eValue ReadList(Reader reader, eList list, char start, char end)
        {
            string token = reader.next();

            if (token[0] != start)
                throw new ParseError("expected '" + start + "'");

            while ((token = reader.peek()) != null && token[0] != end)
                list.conj_BANG(ReadForm(reader));

            if (token == null)
                throw new ParseError("expected '" + end + "', got EOF");

            reader.next();

            return list;
        }

        private static eValue ReadHashMap(Reader reader)
        {
            eList list = (eList) ReadList(reader, new eList(), '{', '}');
            return new eHashMap(list);
        }

        private static eValue ReadForm(Reader reader)
        {
            string token = reader.peek();
            eValue form = null;

            if (token == null) throw new eContinue();

            switch (token)
            {
                case "'":
                    reader.next();
                    return new eList(new eSymbol("quote"), ReadForm(reader));
                case "`":
                    reader.next();
                    return new eList(
                        new eSymbol("quasiquote"), ReadForm(reader)
                    );
                case "~":
                    reader.next();
                    return new eList(new eSymbol("unquote"), ReadForm(reader));
                case "~@":
                    reader.next();
                    return new eList(
                        new eSymbol("splice-unquote"), ReadForm(reader)
                    );
                case "^":
                    reader.next();
                    eValue meta = ReadForm(reader);
                    return new eList(
                        new eSymbol("with-meta"), ReadForm(reader), meta
                    );
                case "@":
                    reader.next();
                    return new eList(new eSymbol("deref"), ReadForm(reader));

                case "(":
                    form = ReadList(reader, new eList(), '(', ')');
                    break;
                case ")":
                    throw new ParseError("unexpected ')'");
                case "[":
                    form = ReadList(reader, new eVector(), '[', ']');
                    break;
                case "]":
                    throw new ParseError("unexpected ']'");
                case "{":
                    form = ReadHashMap(reader);
                    break;
                case "}":
                    throw new ParseError("unexpected '}'");

                default:
                    form = ReadAtom(reader);
                    break;
            }
            return form;
        }

        public static eValue Tokenize(string source)
        {
            return ReadForm(new Reader(GetLexemes(source)));
        }
    }
}
