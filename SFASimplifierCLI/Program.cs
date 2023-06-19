using SFASimplifier.Models;
using ShellProgressBar;

namespace SFASimplifierCLI
{
    internal static class Program
    {
        #region Internal Methods

        internal static void Main(string[] args)
        {
            var options = new Options();

            using var progressBar = new ProgressBar(
                maxTicks: 10000,
                message: "Simplify SFA data.");
            var progressReport = progressBar.AsProgress<float>();

            void onProgressChange(double progress, string text)
            {
                progressReport.Report((float)progress);
                progressBar.Message = text;
            }

            var service = new SFASimplifier.Service(
                options: options,
                onProgressChange: onProgressChange);

            service.Run(args[0], args[1]);
        }

        #endregion Internal Methods
    }
}