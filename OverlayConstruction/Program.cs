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
            foreach (var file in dataFiles)
            {
                var dataset = FileWorker.ReadDataset(file);
                var mixes = TopologyConstructor.CreateTopology(dataset);
                PathCalculator.CalculatePaths(mixes);
                FileWorker.WriteResults(mixes, file.Substring(file.LastIndexOf("\\")));
            }
        }
    }
}
