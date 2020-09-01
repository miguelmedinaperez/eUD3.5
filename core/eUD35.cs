using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PRFramework.Core.Common;
using PRFramework.Core.DatasetInfo;
using PRFramework.Core.SupervisedClassifiers;
using PRFramework.Core.SupervisedClassifiers.EmergingPatterns;

namespace PRFramework.Clustering
{

    public class eUD35
    {
        public int ClusterCount { get; set; } = 2;

        public List<List<Instance>> FindClusters(InstanceModel model, List<Instance> instances, out List<IEmergingPattern> selectedPatterns)
        {
            NominalFeature classFeature = null;
            FeatureInformation backupFeatureInformation = null;
            string[] backupClassValues = null;
            double[] backupClassByInstance = null;
            bool isClassPresent = true;
            if (model.ClassFeature() == null)
            {
                isClassPresent = false;
                classFeature = new NominalFeature("class", model.Features.Length);
                var backupFeatures = model.Features;
                model.Features = new Feature[backupFeatures.Length + 1];
                for (int i = 0; i < backupFeatures.Length; i++)
                    model.Features[i] = backupFeatures[i];

                model.Features[backupFeatures.Length] = classFeature;
            }
            else
            {
                classFeature = model.ClassFeature() as NominalFeature;
                backupFeatureInformation = classFeature.FeatureInformation;
                backupClassValues = classFeature.Values;

                backupClassByInstance = new double[instances.Count];
                for (int i = 0; i < instances.Count; i++)
                {
                    backupClassByInstance[i] = instances[i][classFeature];
                    instances[i][classFeature] = 0;
                }
            }

            classFeature.FeatureInformation = new NominalFeatureInformation()
            {
                Distribution = new double[] { 1, 1, 1, 1, 1 },
                Ratio = new double[] { 1, 1, 1, 1, 1 },
                ValueProbability = new double[] { 1, 1, 1, 1, 1 }
            };

            classFeature.Values = new string[1] { "Unknown" };

            var Miner = new UnsupervisedRandomForestMiner() { ClusterCount = ClusterCount, TreeCount = 100 };

            var patterns = Miner.Mine(model, instances, classFeature);

            var instIdx = new Dictionary<Instance, int>();
            for (int i = 0; i < instances.Count; i++)
                instIdx.Add(instances[i], i);

            int[,] similarityMatrix = new int[instances.Count, instances.Count + 1];
            var coverSetByPattern = new Dictionary<IEmergingPattern, HashSet<Instance>>();
            foreach (var pattern in patterns)
                if (pattern != null)
                {
                    var currentCluster = new List<int>();
                    var currentCoverSet = new HashSet<Instance>();
                    for (int i = 0; i < instances.Count; i++)
                        if (pattern.IsMatch(instances[i]))
                        {
                            currentCluster.Add(i);
                            currentCoverSet.Add(instances[i]);
                        }

                    for (int i = 0; i < currentCluster.Count; i++)
                    {
                        for (int j = 0; j < currentCluster.Count; j++)
                        {
                            similarityMatrix[currentCluster[i], currentCluster[j]] += 1;
                            similarityMatrix[currentCluster[i], instances.Count] += 1;
                        }
                    }

                    coverSetByPattern.Add(pattern, currentCoverSet);
                }

            var kmeans = new KMeans() { K = ClusterCount, classFeature = classFeature, similarityMatrix = similarityMatrix, instIdx = instIdx };
            var clusterList = kmeans.FindClusters(instances);

            var patternClusterList = new List<List<IEmergingPattern>>();
            for (int i = 0; i < ClusterCount; i++)
                patternClusterList.Add(new List<IEmergingPattern>());

            foreach (var pattern in patterns)
                if (pattern != null)
                {
                    var bestIdx = 0;
                    var maxCoverCount = int.MinValue;
                    pattern.Supports = new double[ClusterCount];
                    pattern.Counts = new double[ClusterCount];
                    HashSet<Instance> bestCover = null;
                    for (int i = 0; i < ClusterCount; i++)
                    {
                        HashSet<Instance> currentCover = new HashSet<Instance>(coverSetByPattern[pattern].Intersect(clusterList[i]));
                        var currentCoverCount = currentCover.Count;
                        pattern.Counts[i] = currentCoverCount;
                        pattern.Supports[i] = 1.0 * currentCoverCount / clusterList[i].Count;
                        if (currentCoverCount > maxCoverCount)
                        {
                            maxCoverCount = currentCoverCount;
                            bestIdx = i;
                            bestCover = currentCover;
                        }
                    }
                    coverSetByPattern[pattern] = bestCover;

                    patternClusterList[bestIdx].Add(pattern);
                }

            selectedPatterns = FilterPatterns(instances, patternClusterList);

            if (isClassPresent)
            {
                classFeature.FeatureInformation = backupFeatureInformation;
                classFeature.Values = backupClassValues;
                for (int i = 0; i < instances.Count; i++)
                    instances[i][classFeature] = backupClassByInstance[i];
            }

            return clusterList;
        }

        private List<IEmergingPattern> FilterPatterns(List<Instance> instances, List<List<IEmergingPattern>> patternClusterList)
        {
            var confidenceByPattern = new Dictionary<IEmergingPattern, double>(instances.Count);
            for (int i = 0; i < ClusterCount; i++)
                foreach (var pattern in patternClusterList[i])
                    confidenceByPattern.Add(pattern, pattern.Counts[i] / pattern.Counts.Sum());

            EmergingPatternComparer epComparer = new EmergingPatternComparer(new ItemComparer());
            var selectedPatterns = new List<IEmergingPattern>();
            for (int i = 0; i < ClusterCount; i++)
            {
                patternClusterList[i].Sort(
                    (ep1, ep2) =>
                        confidenceByPattern[ep1] > confidenceByPattern[ep2]
                            ? -1
                            : confidenceByPattern[ep1] < confidenceByPattern[ep2]
                                ? 1
                                : ep1.Counts[i] > ep2.Counts[i]
                                    ? -1
                                    : ep1.Counts[i] < ep2.Counts[i]
                                        ? 1
                                        : ep1.Items.Count < ep2.Items.Count
                                            ? -1
                                            : ep1.Items.Count > ep2.Items.Count ? 1 : 0);

                var currentSelectedPatterns = new List<IEmergingPattern>();
                for (int j = 0; j < patternClusterList[i].Count; j++)
                {
                    var candidatePattern = patternClusterList[i][j];
                    bool generalPatternFound = false;
                    for (int k = 0; k < currentSelectedPatterns.Count && generalPatternFound == false; k++)
                    {
                        var patternRelation = epComparer.Compare(candidatePattern, currentSelectedPatterns[k]);
                        if (patternRelation == SubsetRelation.Subset || patternRelation == SubsetRelation.Equal)
                            generalPatternFound = true;
                    }

                    if (!generalPatternFound)
                    {
                        currentSelectedPatterns.Add(patternClusterList[i][j]);
                        selectedPatterns.Add(patternClusterList[i][j]);
                    }
                }
            }

            return selectedPatterns;
        }

        private class KMeans
        {
            public int[,] similarityMatrix;

            public NominalFeature classFeature;

            public Dictionary<Instance, int> instIdx;

            public List<List<Instance>> FindClusters(List<Instance> source)
            {
                if (source == null)
                    throw new ArgumentNullException("source", "Unable to apply K-Means clustering algorithm: Null data set.");

                // Selecting K random centers and associating empty clusters to those centers
                List<Instance> centers = Sample(source, K);
                var clusterByCenter = new Dictionary<Instance, List<Instance>>(K);

                // Iterative finding clusters while centers vary
                bool didCentersChanged = true;
                int itCount = 0;
                while (didCentersChanged && itCount++ < maxIterationCount)
                {
                    didCentersChanged = false;

                    // Finding clusters
                    clusterByCenter.Clear();
                    foreach (Instance instance in centers)
                        clusterByCenter.Add(instance, new List<Instance>());

                    foreach (var instance in source)
                    {
                        int maxSimilarity = int.MinValue;
                        Instance mostSimilarCenter = null;
                        int instanceIdx = instIdx[instance];
                        foreach (var center in centers)
                        {
                            int centerIdx = instIdx[center];
                            var currentSimilarity =
                                similarityMatrix[instanceIdx, centerIdx];
                            if (currentSimilarity > maxSimilarity || (currentSimilarity == maxSimilarity && instanceIdx == centerIdx))
                            {
                                maxSimilarity = currentSimilarity;
                                mostSimilarCenter = center;
                            }
                        }

                        if (mostSimilarCenter == null)
                            foreach (var center in centers)
                            {
                                int currentSimilarity = 0;
                                foreach (var clusterInstance in clusterByCenter[center])
                                    currentSimilarity +=
                                        similarityMatrix[instanceIdx, instIdx[clusterInstance]];

                                if (currentSimilarity > maxSimilarity)
                                {
                                    maxSimilarity = currentSimilarity;
                                    mostSimilarCenter = center;
                                }
                            }
                        if (mostSimilarCenter == null)
                            clusterByCenter[centers[random.Next(centers.Count)]].Add(instance);
                        else
                            clusterByCenter[mostSimilarCenter].Add(instance);
                    }

                    // Building centers
                    var newClusterByCenter = new Dictionary<Instance, List<Instance>>(K);
                    for (int j = 0; j < centers.Count; j++)
                    {
                        if (clusterByCenter[centers[j]].Count == 0)
                            throw new Exception("Empty cluster");

                        Instance newCenter = null;
                        foreach (var candidateCenter in GetCentroidList(clusterByCenter[centers[j]]))
                        //if (!newClusterByCenter.ContainsKey(candidateCenter))
                        {
                            newCenter = candidateCenter;
                            break;
                        }
                        if (newCenter == null)
                            throw new Exception("Two cluster with same center");

                        newClusterByCenter.Add(newCenter, clusterByCenter[centers[j]]);
                        if (centers[j] != newCenter)
                        {
                            didCentersChanged = true;
                            centers[j] = newCenter;
                        }
                    }

                    clusterByCenter = newClusterByCenter;
                }

                // Building resulting structure
                var resultClusters = new List<List<Instance>>();
                foreach (Instance center in centers)
                    resultClusters.Add(new List<Instance>(clusterByCenter[center]));

                return resultClusters;
            }

            private List<Instance> GetCentroidList(List<Instance> instances)
            {
                Instance clusterCenter = null;
                double maxSimilarity = double.MinValue;
                int[] similarities = new int[instances.Count];
                bool[] invalidCentroid = new bool[instances.Count];
                for (int i = 0; i < instances.Count; i++)
                    if (!invalidCentroid[i])
                    {
                        for (int j = i; j < instances.Count; j++)
                            if (!invalidCentroid[j])
                            {
                                var currentSimilarity =
                                    similarityMatrix[instIdx[instances[i]], instIdx[instances[j]]];
                                if (currentSimilarity == 0)
                                {
                                    invalidCentroid[i] = true;
                                    invalidCentroid[j] = true;
                                    similarities[i] = 0;
                                    similarities[j] = 0;
                                    break;
                                }

                                similarities[i] += currentSimilarity;
                                similarities[j] += currentSimilarity;
                            }

                        if (maxSimilarity < similarities[i])
                        {
                            maxSimilarity = similarities[i];
                            clusterCenter = instances[i];
                        }
                    }

                List<Tuple<Instance, int>> weightedInstanceList = new List<Tuple<Instance, int>>();
                for (int i = 0; i < instances.Count; i++)
                    weightedInstanceList.Add(new Tuple<Instance, int>(instances[i], similarities[i]));
                weightedInstanceList.Sort((a, b) => a.Item2 > b.Item2 ? -1 : a.Item2 == b.Item2 ? 0 : 1);

                var clusterCenters = new List<Instance>();
                for (int i = 0; i < weightedInstanceList.Count; i++)
                    clusterCenters.Add(weightedInstanceList[i].Item1);

                return clusterCenters;
            }

            private List<Instance> Sample(List<Instance> source, int count)
            {
                int bestCover = int.MinValue;
                int bestInstanceIndex = -1;
                for (int j = 0; j < source.Count; j++)
                {
                    int currentInstanceCover = (int)similarityMatrix[instIdx[source[j]], source.Count];

                    if (currentInstanceCover > bestCover)
                    {
                        bestCover = currentInstanceCover;
                        bestInstanceIndex = j;
                    }
                }

                //bestInstanceIndex = random.Next(source.Count);

                HashSet<int> bestInstanceIdx = new HashSet<int>();
                bestInstanceIdx.Add(bestInstanceIndex);

                for (int i = 1; i < count; i++)
                {
                    bestCover = int.MaxValue;
                    bestInstanceIndex = -1;
                    for (int j = 0; j < source.Count; j++)
                        if (!bestInstanceIdx.Contains(j))
                        {
                            int currentInstanceCover = 0;
                            foreach (var k in bestInstanceIdx)
                                currentInstanceCover += (int)similarityMatrix[instIdx[source[j]], instIdx[source[k]]];

                            if (currentInstanceCover < bestCover || (currentInstanceCover == bestCover &&
                                (int)similarityMatrix[instIdx[source[bestInstanceIndex]], source.Count] < (int)similarityMatrix[instIdx[source[j]], source.Count]))
                            {
                                bestCover = currentInstanceCover;
                                bestInstanceIndex = j;
                            }
                        }

                    bestInstanceIdx.Add(bestInstanceIndex);
                }

                List<Instance> sample = new List<Instance>(count);
                foreach (var i in bestInstanceIdx)
                    sample.Add(source[i]);

                return sample;

            }

            private List<Instance> SampleBackup(List<Instance> source, int count)
            {
                int bestCover = int.MaxValue;
                int bestInstanceIndex = -1;
                for (int j = 0; j < source.Count; j++)
                {
                    int currentInstanceCover = (int)similarityMatrix[instIdx[source[j]], source.Count];

                    if (currentInstanceCover < bestCover)
                    {
                        bestCover = currentInstanceCover;
                        bestInstanceIndex = j;
                    }
                }

                //bestInstanceIndex = random.Next(source.Count);

                HashSet<int> bestInstanceIdx = new HashSet<int>();
                bestInstanceIdx.Add(bestInstanceIndex);

                for (int i = 1; i < count; i++)
                {
                    bestCover = int.MaxValue;
                    bestInstanceIndex = -1;
                    for (int j = 0; j < source.Count; j++)
                        if (!bestInstanceIdx.Contains(j))
                        {
                            int currentInstanceCover = 0;
                            foreach (var k in bestInstanceIdx)
                                currentInstanceCover += (int)similarityMatrix[instIdx[source[j]], instIdx[source[k]]];

                            if (currentInstanceCover < bestCover || (currentInstanceCover == bestCover &&
                                (int)similarityMatrix[instIdx[source[bestInstanceIndex]], source.Count] < (int)similarityMatrix[instIdx[source[j]], source.Count]))
                            {
                                bestCover = currentInstanceCover;
                                bestInstanceIndex = j;
                            }
                        }

                    bestInstanceIdx.Add(bestInstanceIndex);
                }

                List<Instance> sample = new List<Instance>(count);
                foreach (var i in bestInstanceIdx)
                    sample.Add(source[i]);

                return sample;


                //List<Instance> Result = new List<Instance>();
                //ArrayList Idx = new ArrayList(source.Count);
                //int i;
                //for (i = 0; i < source.Count; i++)
                //    Idx.Add(i);
                //int CurrentCount = Math.Min(count, source.Count);
                //for (i = 0; i < CurrentCount; i++)
                //{
                //    int RndIdx = random.Next(0, Idx.Count);
                //    int ObjIdx = Convert.ToInt32(Idx[RndIdx]);

                //    Result.Add(source[ObjIdx]);
                //    Idx.RemoveAt(RndIdx);
                //}
                //return Result;
            }

            class DoubleComparer : Comparer<uint>
            {
                public override int Compare(uint x, uint y)
                {
                    return (x > y) ? -1 : (x == y) ? 0 : 1;
                }
            }

            private static Random random = new Random((int)DateTime.Now.Ticks);

            public int K
            {
                get { return k; }
                set
                {
                    if (value < 1)
                        throw new ArgumentOutOfRangeException("Unable to assign cluster count value: Cluster count can not be less than 1.");
                    k = value;
                }
            }

            public int MaxIterationCount
            {
                get { return maxIterationCount; }
                set
                {
                    if (value < 1)
                        throw new ArgumentOutOfRangeException("Invalid maximum iteration count.");
                    maxIterationCount = value;
                }
            }

            private int k = 2;

            private int maxIterationCount = 100;
        }
    }

}
