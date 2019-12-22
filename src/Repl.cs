using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Evil;

using Internal;
using eValue = Evil.Types.eValue;
using eString = Evil.Types.eString;
using eSymbol = Evil.Types.eSymbol;
using eInt = Evil.Types.eInt;
using eList = Evil.Types.eList;
using eVector = Evil.Types.eVector;
using eHashMap = Evil.Types.eHashMap;
using eFunction = Evil.Types.eFunction;
using Env = Evil.Env;

namespace Evil
{

  public sealed class Repl
  {

    private Evil.Env enviroment;

    public Repl()
    {
      this.enviroment = new Evil.Env(null);

      // core.cs: defined using C#
      foreach (var entry in core.ns)
      {
        this.enviroment.set(new eSymbol(entry.Key), entry.Value);
      }

      this.enviroment.set(
        new eSymbol("eval"),
        new eFunction(a => Eval(a[0], this.enviroment))
      );
    }

    public void EvalString(string expresion)
    {
      Eval(Read(expresion), this.enviroment);
    }

    // eval
    public static bool is_pair(eValue x)
    {
      return x is eList && ((eList) x).size() > 0;
    }

    public static eValue quasiquote(eValue ast)
    {
      if (!is_pair(ast))
      {
        return new eList(new eSymbol("quote"), ast);
      }
      else
      {
        eValue a0 = ((eList) ast) [0];
        if ((a0 is eSymbol) &&
          (((eSymbol) a0).getName() == "unquote"))
        {
          return ((eList) ast) [1];
        }
        else if (is_pair(a0))
        {
          eValue a00 = ((eList) a0) [0];
          if ((a00 is eSymbol) &&
            (((eSymbol) a00).getName() == "splice-unquote"))
          {
            return new eList(new eSymbol("concat"),
              ((eList) a0) [1],
              quasiquote(((eList) ast).rest()));
          }
        }
        return new eList(new eSymbol("cons"),
          quasiquote(a0),
          quasiquote(((eList) ast).rest()));
      }
    }

    public static bool is_macro_call(eValue ast, Env env)
    {
      if (ast is eList)
      {
        eValue a0 = ((eList) ast) [0];
        if (a0 is eSymbol &&
          env.find((eSymbol) a0) != null)
        {
          eValue mac = env.get((eSymbol) a0);
          if (mac is eFunction &&
            ((eFunction) mac).isMacro())
          {
            return true;
          }
        }
      }
      return false;
    }

    public static eValue macroexpand(eValue ast, Env env)
    {
      while (is_macro_call(ast, env))
      {
        eSymbol a0 = (eSymbol) ((eList) ast) [0];
        eFunction mac = (eFunction) env.get(a0);
        ast = mac.apply(((eList) ast).rest());
      }
      return ast;
    }

    static eValue ParseAST(eValue ast, Env env)
    {
      if (ast is eSymbol)
      {
        return env.get((eSymbol) ast);
      }
      else if (ast is eList)
      {
        eList oldList = (eList) ast;
        eList newList = ast.list_Q() ? new eList() : (eList) new eVector();

        foreach (eValue mv in oldList.getValue())
        {
          newList.conj_BANG(Eval(mv, env));
        }

        return newList;
      }
      else if (ast is eHashMap)
      {
        var hashMap = new Dictionary<string, eValue>();

        foreach (var entry in ((eHashMap) ast).getValue())
        {
          hashMap.Add(entry.Key, Eval((eValue) entry.Value, env));
        }

        return new eHashMap(hashMap);
      }
      else
      {
        return ast;
      }
    }

    static eValue Eval(eValue orig_ast, Env env)
    {
      eValue a0, a1, a2, res;
      eList el;

      while (true)
      {
        if (!orig_ast.list_Q()) return ParseAST(orig_ast, env);

        eValue expanded = macroexpand(orig_ast, env);
        if (!expanded.list_Q()) return ParseAST(expanded, env);

        eList ast = (eList) expanded;
        if (ast.size() == 0) return ast;

        a0 = ast[0];

        String a0sym = a0 is eSymbol ? ((eSymbol) a0).getName() : "__<*fn*>__";

        switch (a0sym)
        {
          case "def": // define vareable
            res = Eval(ast[2] /* vareable value */ , env);
            env.set((eSymbol) ast[1] /* vareable name */ , res);
            return res;
          case "let":
            a1 = ast[1];
            a2 = ast[2];
            eSymbol key;
            eValue val;
            Env let_env = new Env(env);
            for (int i = 0; i < ((eList) a1).size(); i += 2)
            {
              key = (eSymbol) ((eList) a1) [i];
              val = ((eList) a1) [i + 1];
              let_env.set(key, Eval(val, let_env));
            }
            orig_ast = a2;
            env = let_env;
            break;
          case "quote":
            return ast[1];
          case "quasiquote":
            orig_ast = quasiquote(ast[1]);
            break;
          case "defmacro": // define macros
            res = Eval(ast[2] /* macros value */ , env);
            ((eFunction) res).setMacro();
            env.set(((eSymbol) ast[1] /* macros name */ ), res);
            return res;
          case "macroexpand": // return macros body
            return macroexpand(ast[1] /* macros name */ , env);
          case "try*":
            try
            {
              return Eval(ast[1], env);
            }
            catch (Exception e)
            {
              if (ast.size() > 2)
              {
                eValue exc;
                a2 = ast[2];
                eValue a20 = ((eList) a2) [0];
                if (((eSymbol) a20).getName() == "catch*")
                {
                  if (e is Evil.Types.eException)
                  {
                    exc = ((Evil.Types.eException) e).getValue();
                  }
                  else
                  {
                    exc = new eString(e.StackTrace);
                  }
                  return Eval(((eList) a2) [2],
                    new Env(env, ((eList) a2).slice(1, 2),
                      new eList(exc)));
                }
              }
              throw e;
            }
          case "do": // simple block construction
            ParseAST(ast.slice(1, ast.size() - 1), env);
            orig_ast = ast[ast.size() - 1];
            break;
          case "if":
            a1 = ast[1];
            eValue cond = Eval(a1, env);
            if (cond == Evil.Types.Nil || cond == Evil.Types.False)
            {
              // eval false slot form
              if (ast.size() > 3)
              {
                orig_ast = ast[3];
              }
              else
              {
                return Evil.Types.Nil;
              }
            }
            else
            {
              // eval true slot form
              orig_ast = ast[2];
            }
            break;
          case "fn":
            eList a1f = (eList) ast[1];
            eValue a2f = ast[2];
            Env cur_env = env;
            return new eFunction(a2f, env, a1f,
              args => Eval(a2f, new Env(cur_env, a1f, args)));
          default:
            el = (eList) ParseAST(ast, env);
            var f = (eFunction) el[0];
            eValue fnast = f.getAst();
            if (fnast != null)
            {
              orig_ast = fnast;
              env = f.genEnv(el.rest());
            }
            else
            {
              return f.apply(el.rest());
            }
            break;
        }

      }
    }

    private eValue Read(string expresion)
    {
      return Lexer.Tokenize(expresion);
    }

    private string Print(eValue expresion)
    {
      return printer._pr_str(expresion, true);
    }

    public void EvalFile(string filepath)
    {
      this.EvalString("(def load-file (fn [f] (eval (read-string (str \"(do \" (slurp f) \")\")))))");
      this.EvalString($"(load-file \"{filepath}\")");
    }

    public void Loop(string[] args)
    {

      eList _argv = new eList();

      for (int i = 1; i < args.Length; i++)
      {
        _argv.conj_BANG(new eString(args[i]));
      }

      this.enviroment.set(new eSymbol("*ARGV*"), _argv);

      while (true)
      {
        string line;

        try
        {
          line = ReadLine.Read("user> ");
          if (line == null) { break; }
          if (line == "") { continue; }
        }
        catch (IOException e)
        {
          Console.WriteLine("IOException: " + e.Message);
          break;
        }

        try
        {
          Console.WriteLine(this.Print(this.EvalString(line)));
        }
        catch (Evil.Types.eContinue)
        {
          continue;
        }
        catch (Evil.Types.eException e)
        {
          Console.WriteLine($"Error: {printer._pr_str (e.getValue (), false)}");
          continue;
        }
        catch (Exception e)
        {
          Console.WriteLine($"Error: {e.Message}");
          Console.WriteLine(e.StackTrace);
          continue;
        }

      }
    }

  }

}
