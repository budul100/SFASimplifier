using CommandLine;
using CommandLine.Text;
using SFASimplifier.Simplifier;
using SFASimplifier.Simplifier.Models;
using ShellProgressBar;
using System.Diagnostics;

namespace SFASimplifier.CLI
{
    internal static class Program
    {
        #region Public Fields

        public const int ExitCodeDefault = -1;
        public const int ExitCodeHelpRequested = 1;
        public const int ExitCodeSuccess = 0;

        #endregion Public Fields

        #region Private Fields

        private const int ProgressMaxTicks = 10000;

        #endregion Private Fields

        #region Internal Methods

        internal static void Main(string[] args)
        {
            var result = ExitCodeDefault;

            var parsing = new Parser(with => with.HelpWriter = default)
                .ParseArguments<Options>(args);

            parsing.MapResult(
                parsedFunc: options => result = RunService(options),
                notParsedFunc: _ => result = RunHelp(parsing));
        }

        #endregion Internal Methods

        #region Private Methods

        private static int RunHelp<T>(ParserResult<T> parserResult)
        {
            var helpText = HelpText.AutoBuild(
                parserResult: parserResult,
                onError: h => h,
                onExample: e => e);

            Console.WriteLine(helpText);

            return ExitCodeHelpRequested;
        }

        private static int RunService(Options options)
        {
            int result;

            try
            {
                using var progressBar = new ProgressBar(
                    maxTicks: ProgressMaxTicks,
                    message: "Simplify SFA data.");

                var progressReport = progressBar.AsProgress<float>();

                void onProgressChange(double progress, string text)
                {
                    progressReport.Report((float)progress);
                    progressBar.Message = text;
                }

                var service = new Service(
                    options: options,
                    onProgressChange: onProgressChange);

                service.Run();

                result = ExitCodeSuccess;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception?.Message);

                result = exception?.InnerException?.HResult
                    ?? exception?.HResult
                    ?? -1;

                Debugger.Break();
            }

            return result;
        }

        #endregion Private Methods
    }
}