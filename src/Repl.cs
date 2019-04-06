using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Mal;
using eValue = Mal.Types.eValue;
using eString = Mal.Types.eString;
using eSymbol = Mal.Types.eSymbol;
using eInt = Mal.Types.eInt;
using eList = Mal.Types.eList;
using eVector = Mal.Types.eVector;
using eHashMap = Mal.Types.eHashMap;
using eFunction = Mal.Types.eFunction;
using Env = Mal.env.Env;

namespace Mal
{

  public sealed class Repl
  {

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

    static eValue eval_ast(eValue ast, Env env)
    {
      if (ast is eSymbol)
      {
        return env.get((eSymbol) ast);
      }
      else if (ast is eList)
      {
        eList old_lst = (eList) ast;
        eList new_lst = ast.list_Q() ? new eList() :
          (eList) new eVector();
        foreach (eValue mv in old_lst.getValue())
        {
          new_lst.conj_BANG(Eval(mv, env));
        }
        return new_lst;
      }
      else if (ast is eHashMap)
      {
        var new_dict = new Dictionary<string, eValue>();
        foreach (var entry in ((eHashMap) ast).getValue())
        {
          new_dict.Add(entry.Key, Eval((eValue) entry.Value, env));
        }
        return new eHashMap(new_dict);
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

        //Console.WriteLine("Eval: " + printer._pr_str(orig_ast, true));
        if (!orig_ast.list_Q())
        {
          return eval_ast(orig_ast, env);
        }

        // apply list
        eValue expanded = macroexpand(orig_ast, env);
        if (!expanded.list_Q())
        {
          return eval_ast(expanded, env);
        }
        eList ast = (eList) expanded;

        if (ast.size() == 0) { return ast; }
        a0 = ast[0];

        String a0sym = a0 is eSymbol ? ((eSymbol) a0).getName() : "__<*fn*>__";

        switch (a0sym)
        {
          case "def!":
            a1 = ast[1];
            a2 = ast[2];
            res = Eval(a2, env);
            env.set((eSymbol) a1, res);
            return res;
          case "let*":
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
          case "defmacro!":
            a1 = ast[1];
            a2 = ast[2];
            res = Eval(a2, env);
            ((eFunction) res).setMacro();
            env.set(((eSymbol) a1), res);
            return res;
          case "macroexpand":
            a1 = ast[1];
            return macroexpand(a1, env);
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
                  if (e is Mal.Types.eException)
                  {
                    exc = ((Mal.Types.eException) e).getValue();
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
          case "do":
            eval_ast(ast.slice(1, ast.size() - 1), env);
            orig_ast = ast[ast.size() - 1];
            break;
          case "if":
            a1 = ast[1];
            eValue cond = Eval(a1, env);
            if (cond == Mal.Types.Nil || cond == Mal.Types.False)
            {
              // eval false slot form
              if (ast.size() > 3)
              {
                orig_ast = ast[3];
              }
              else
              {
                return Mal.Types.Nil;
              }
            }
            else
            {
              // eval true slot form
              orig_ast = ast[2];
            }
            break;
          case "fn*":
            eList a1f = (eList) ast[1];
            eValue a2f = ast[2];
            Env cur_env = env;
            return new eFunction(a2f, env, a1f,
              args => Eval(a2f, new Env(cur_env, a1f, args)));
          default:
            el = (eList) eval_ast(ast, env);
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

    static eValue Read(string str)
    {
      return reader.read_str(str);
    }

    static string Print(eValue expresions)
    {
      return printer._pr_str(expresions, true);
    }

    public static void Loop(string[] args)
    {
      var repl_env = new Mal.env.Env(null);

      Func<string, eValue> RE = (string str) => Eval(Read(str), repl_env);

      // core.cs: defined using C#
      foreach (var entry in core.ns)
      {
        repl_env.set(new eSymbol(entry.Key), entry.Value);
      }

      repl_env.set(new eSymbol("eval"), new eFunction(a => Eval(a[0], repl_env)));

      int fileIdx = 0;

      // if (args.Length > 0 && args[0] == "--raw") {
      //   Mal.readline.mode = Mal.readline.Mode.Raw;
      //   fileIdx = 1;
      // }

      eList _argv = new eList();

      for (int i = fileIdx + 1; i < args.Length; i++)
      {
        _argv.conj_BANG(new eString(args[i]));
      }

      repl_env.set(new eSymbol("*ARGV*"), _argv);

      // if (args.Length > fileIdx) {
      //   RE ("(load-file \"" + args[fileIdx] + "\")");
      //   return;
      // }

      // RE ("(println (str \"Mal [\" *host-language* \"]\"))");

      while (true)
      {
        string line;

        try
        {
          line = Mal.readline.Readline("user> ");
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
          Console.WriteLine(Print(RE(line)));
        }
        catch (Mal.Types.eContinue)
        {
          continue;
        }
        catch (Mal.Types.eException e)
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
