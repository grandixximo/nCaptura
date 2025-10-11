using Captura.Models;
using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;

namespace Captura
{
    [Verb("upload", HelpText = "Upload a file to a specified service.")]
    class UploadCmdOptions : ICmdlineVerb
    {
        [Value(0, HelpText = "The service to upload to")]
        public UploadService Service { get; set; }

        [Value(1, HelpText = "The file to upload")]
        public string FileName { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield break;
            }
        }

        public void Run()
        {
            if (!File.Exists(FileName))
            {
                Console.WriteLine("File not found");
                return;
            }

            // No upload services currently implemented
            Console.WriteLine("No upload services available");
        }
    }
}
