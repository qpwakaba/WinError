using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

using OptionName = System.String;
using OptionValue = System.String;
namespace qpwakaba.WinError
{
    internal class Program
    {
        private const OptionName LanguageOption = "language";
        private const OptionName HelpOption = "help";

        private static void Main(string[] args)
        {
            var parameters = new List<string>();
            var options = new Dictionary<OptionName, OptionValue>();

            bool endOfOptions = false;
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (endOfOptions)
                {
                    parameters.Add(arg);
                    continue;
                }

                if (arg == "--")
                {
                    endOfOptions = true;
                    continue;
                }
                else if (arg == $"--{LanguageOption}")
                {
                    options[LanguageOption] = args[++i];
                }
                else if (arg == $"--{HelpOption}")
                {
                    options[HelpOption] = "";
                }
                else
                {
                    parameters.Add(arg);
                }
            }
            Environment.Exit(Apply(parameters.ToArray(), options));
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, out IntPtr lpBuffer, int nSize, IntPtr Arguments);

        private static string FormatMessage(int languageId, int messageId)
        {
            const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x100;
            const int FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;

            int result = FormatMessage(
                FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
                IntPtr.Zero,
                messageId,
                languageId,
                out var buffer,
                0,
                IntPtr.Zero);

            string message = Marshal.PtrToStringUni(buffer);
            Marshal.FreeHGlobal(buffer); //LocalFree

            return message;
        }

        private static int GetLCID(string languageString)
        {
            if (languageString == null)
            {
                throw new ArgumentNullException(nameof(languageString));
            }

            try
            {
                return CultureInfo.GetCultureInfoByIetfLanguageTag(languageString).LCID;
            }
            catch (ArgumentException)
            {
                return int.Parse(languageString);
            }
        }

        private static int ToInt32(string value)
        {
            if (value[0] == '0' && value[1] == 'x')
            {
                return Convert.ToInt32(value.Substring(2), 16);
            }
            else if (value[0] == 0)
            {
                return Convert.ToInt32(value, 8);
            }
            else
            {
                return Convert.ToInt32(value, 10);
            }
        }

        private static int Apply(string[] args, IDictionary<OptionName, OptionValue> options)
        {
            if (args.Length == 0 || options.ContainsKey(HelpOption))
            {
                Console.Error.WriteLine($"Usage: {Environment.GetCommandLineArgs()[0]} [--{LanguageOption} <language id>] <message id>");
                Console.Error.WriteLine();
                Console.Error.WriteLine($"Example: > {Environment.GetCommandLineArgs()[0]} --{LanguageOption} 1033 0x00000005");
                Console.Error.WriteLine($"         < Access is denied.");
                return options.ContainsKey(HelpOption) ? 0 : 1;
            }

            int messageId = ToInt32(args[0]);
            int languageId =
                options.TryGetValue(LanguageOption, out OptionValue value)
                ? GetLCID(value)
                : CultureInfo.CurrentCulture.LCID;

            Console.WriteLine(FormatMessage(languageId, messageId));

            return 0;
        }
    }
}
