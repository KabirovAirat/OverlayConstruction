using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace OverlayConstruction
{
    class Program
    {
        static void Main(string[] args)
        {
            var dataFiles = FileWorker.GetDataFileNames();
            int i = 0;
            foreach (var file in dataFiles)
            {
                i++;
                var dataset = FileWorker.ReadDataset(file);
                var mixes = TopologyConstructor.CreateTopology(dataset);
                PathCalculator.CalculatePaths(mixes);
                FileWorker.WriteResults(mixes, file.Substring(file.LastIndexOf("\\")));
                Console.WriteLine($@"{i} files already processed");
            }
        }
    }
}
