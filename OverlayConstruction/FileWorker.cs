﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;

namespace OverlayConstruction
{
    public static class FileWorker
    {
        private static readonly string DataPath = ConfigurationManager.AppSettings["DataPath"];

        public static IEnumerable<UnderlayTopologyItem> ReadDataset(string filePath)
        {
            List<UnderlayTopologyItem> dataset = new List<UnderlayTopologyItem>();
            var lines = File.ReadLines(filePath);
            foreach (var line in lines)
            {
                var items = line.Split();
                if (items.Count() != 3) throw new Exception("Dataset item does not consist of 3 items");
                UnderlayTopologyItem datasetItem = new UnderlayTopologyItem();
                datasetItem.FirstNode = int.Parse(items[0].Trim());
                datasetItem.SecondNode = int.Parse(items[1].Trim());
                datasetItem.Latency = int.Parse(items[2].Trim());
                dataset.Add(datasetItem);
            }
            return dataset;
        }

        public static void WriteResults(IEnumerable<Mix> mixes, string filename)
        {
            var resultsDirectory = "results/" + TopologyConstructor.OverlayStrategy + "Overlay" + "_" + PathCalculator.PathSelectionStrategy + "Paths" + '/' + filename.Substring(0, filename.IndexOf('-'));
            Directory.CreateDirectory(resultsDirectory);
            var fileNameCommon = resultsDirectory + '/' + filename.Substring(0, filename.LastIndexOf('-') + 1);
            // create topo file
            var topologyFileName = fileNameCommon + "topo.dat";
            List<string> topologyResult = new List<string>();
            foreach (var mix in mixes)
            {
                var capacityTowardOneNeighbor = (double)mix.BandwidthCapacity / mix.NeighborsWithLatencies.Count;
                foreach (var neigbor in mix.NeighborsWithLatencies.Where(n => n.Key > mix.Id))
                {
                    var resultString = mix.Id.ToString() + "\t" +neigbor.Key.ToString() + "\t" + neigbor.Value.ToString() + "\t" + capacityTowardOneNeighbor.ToString();
                    topologyResult.Add(resultString);
                }
            }

            using (StreamWriter sw = new StreamWriter(topologyFileName))
            {
                topologyResult.ForEach(res => sw.WriteLine(res));
            }

            // create entries file
            var entriesFileName = fileNameCommon + "entries.dat";
            List<string> entriesResult = new List<string>();
            foreach (var mix in mixes)
            {
                var probabilityOfBeingEntry = 1.0 / mixes.Count();
                var resultString = mix.Id.ToString() + "\t" + probabilityOfBeingEntry.ToString() + "\t" + mix.BandwidthCapacity.ToString();
                entriesResult.Add(resultString);
            }

            using (StreamWriter sw = new StreamWriter(entriesFileName))
            {
                entriesResult.ForEach(res => sw.WriteLine(res));
            }

            // create rendezvous file
            var rendezvousFileName = fileNameCommon + "rendezvous.dat";
            List<string> rendezvousResult = new List<string>();
            foreach (var mix in mixes)
            {
                var probabilityOfBeingRendezvous = 1.0 / mixes.Count();
                var resultString = mix.Id.ToString() + "\t" + probabilityOfBeingRendezvous.ToString();
                rendezvousResult.Add(resultString);
            }

            using (StreamWriter sw = new StreamWriter(rendezvousFileName))
            {
                rendezvousResult.ForEach(res => sw.WriteLine(res));
            }

            // create paths file
            var pathsFileName = fileNameCommon + "paths.dat";
            List<string> pathsResult = new List<string>();
            foreach (var mix in mixes)
            {
                foreach (var path in mix.Paths.Where(path => path.RendezvousMix >= mix.Id))
                {
                    var secondRelay = path.SecondMiddleRelay.HasValue ? path.SecondMiddleRelay.ToString() + "\t" : "";
                    var resultString = mix.Id.ToString() + "\t" + path.RendezvousMix.ToString() + 
                         "\t" + path.FirstMiddleRelay.ToString() + "\t" + secondRelay + path.Probability.ToString();
                    pathsResult.Add(resultString);
                }
            }

            using (StreamWriter sw = new StreamWriter(pathsFileName))
            {
                pathsResult.ForEach(res => sw.WriteLine(res));
            }
        }

        public static IEnumerable<string> GetDataFileNames()
        {
            List<string> fileNames = new List<string>();
            fileNames = Directory.GetFiles(DataPath, "*edges.dat", SearchOption.AllDirectories).ToList();
            return fileNames;
        }
    }

}
