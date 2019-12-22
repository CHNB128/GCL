using System;

using Mono.Terminal; // LineEditor (getline.cs)

namespace Evil
{
    public enum ReadLineMode { Terminal, Raw }

    public class ReadLine
    {
        public static ReadLineMode mode = ReadLineMode.Terminal;

        static LineEditor lineedit = null;

        public static string Read(string prompt)
        {
            if (mode == ReadLineMode.Terminal)
            {
                if (lineedit == null)
                {
                    lineedit = new LineEditor("Mal");
                }
                return lineedit.Edit(prompt, "");
            }
            else
            {
                Console.Write(prompt);
                Console.Out.Flush();
                return Console.ReadLine();
            }
        }
    }
}
