using CommandLine;
using Captura.Windows.MediaFoundation;
using Newtonsoft.Json;
using static System.Console;

namespace Captura
{
    [Verb("mf-hw", HelpText = "Print Media Foundation hardware codec support as JSON.")]
    class HwMfCmdOptions : ICmdlineVerb
    {
        public void Run()
        {
            var matrix = MfHardwareProbe.Probe();
            var json = JsonConvert.SerializeObject(matrix, Formatting.Indented);
            WriteLine(json);
        }
    }
}
