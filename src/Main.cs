using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Internal;
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
using NDesk.Options;

namespace Mal {

  public sealed class Program {

    static int verbosity;

    static void Debug (string format, params object[] args) {
      if (verbosity > 0) {
        Console.Write ("# ");
        Console.WriteLine (format, args);
      }
    }

    static void ShowHelp (OptionSet p) {
      Console.WriteLine ("Usage: greet [OPTIONS]+ message");
      Console.WriteLine ("Greet a list of individuals with an optional message.");
      Console.WriteLine ("If no message is specified, a generic greeting is used.");
      Console.WriteLine ();
      Console.WriteLine ("Options:");
      p.WriteOptionDescriptions (Console.Out);
    }

    public static void Main (string[] args) {
      bool showHelpMessage = false;
      List<string> names = new List<string> ();
      int repeat = 1;

      var p = new OptionSet () {
        {
        "repl",
        "start repl",
        v => Mal.Repl.Loop (args)
        }, {
        "v",
        "increase debug message verbosity",
        v => ++verbosity
        }, {
        "h|help",
        "show this message and exit",
        v => showHelpMessage = v != null
        },
      };

      List<string> extra;

      try {
        extra = p.Parse (args);
      } catch (OptionException e) {
        Console.Write ("greet: ");
        Console.WriteLine (e.Message);
        Console.WriteLine ("Try `greet --help' for more information.");
        return;
      }

      if (showHelpMessage) {
        ShowHelp (p);
        return;
      }

      string message;

      if (extra.Count > 0) {
        message = string.Join (" ", extra.ToArray ());
        Debug ("Using new message: {0}", message);
      } else {
        message = "Hello {0}!";
        Debug ("Using default message: {0}", message);
      }

      foreach (string name in names) {
        for (int i = 0; i < repeat; ++i)
          Console.WriteLine (message, name);
      }

    }

  }

}