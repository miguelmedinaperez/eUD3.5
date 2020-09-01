using PRFramework.Clustering;
using PRFramework.Core.Common;
using PRFramework.Core.IO;
using PRFramework.Core.SupervisedClassifiers;
using PRFramework.Core.SupervisedClassifiers.EmergingPatterns;
using PRFramework.Core.SupervisedClassifiers.EmergingPatterns.Classifiers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BaggingRandomMinerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var dataset = CsvLoader.Load(@"dataset.csv", out InstanceModel model).ToList();

            var instanceIndex = new Dictionary<Instance, int>();
            for (int i = 0; i < dataset.Count; i++)
                instanceIndex.Add(dataset[i], i);

            var clusteringAlgorithm = new eUD35() { ClusterCount = 3 };
            var clusters = clusteringAlgorithm.FindClusters(model, dataset, out List<IEmergingPattern> patterns).ToList();

            var clusterPerInstanceIdx = new Dictionary<int, string>();
            for (int i = 0; i < clusters.Count; i++)
                foreach (var instance in clusters[i])
                {
                    clusterPerInstanceIdx.Add(instanceIndex[instance], $"cluster{i}");
                }

            Console.WriteLine("***************** Cluster per instance *****************");
            for (int i = 0; i < dataset.Count; i++)
                Console.WriteLine(clusterPerInstanceIdx[i]);

            Console.WriteLine("\n***************** Patterns *****************");
            foreach (var p in patterns)
                Console.WriteLine(p);

            Console.ReadLine();
        }
    }
}
