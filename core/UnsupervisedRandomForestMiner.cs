using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PRFramework.Core.Common;
using PRFramework.Core.Samplers;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees.Builder;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees.DistributionTesters;
using PRFramework.Core.SupervisedClassifiers.EmergingPatterns;

namespace PRFramework.Clustering
{
    
    [Serializable]
    public class UnsupervisedRandomForestMiner
    {
        public int TreeCount { get; set; }

        public int FeatureCount { get; set; }

        public DecisionTreeBuilder DecisionTreeBuilder { get; set; }

        public bool MinePatternsWhileBuildingTree { get; set; }

        public AlwaysTrue EPTester { get; set; }

        public int ClusterCount
        {
            get
            {
                return _clusterCount;
            }
            set
            {
                unsupervisedDecisionTreeBuilder.ClusterCount = value;
                _clusterCount = value;
            }
        }

        public UnsupervisedRandomForestMiner()
        {
            unsupervisedDecisionTreeBuilder = new UD31Builder1
            {
                DistributionEvaluator = new UnsupervisedNumericDistributionEvaluator(),
                SplitIteratorProvider = new UD31SplitIteratorProvider(),
                MinimalSplitGain = 0
            };

            TreeCount = 100;
            EPTester = new AlwaysTrue();
            FeatureCount = -1;
        }

        public IEnumerable<IEmergingPattern> MineTest(InstanceModel model, IEnumerable<Instance> instances, Feature classFeature)
        {
            EmergingPatternCreator EpCreator = new EmergingPatternCreator();
            IEmergingPatternSimplifier simplifier = new EmergingPatternSimplifier(new ItemComparer());

            List<Feature> featuresToConsider = model.Features.Where(f => f != classFeature).ToList();
            int featureCount = (FeatureCount != -1) ? FeatureCount : (int)Math.Max(Math.Log(featuresToConsider.Count, 2) + 1, 0.63 * featuresToConsider.Count);
            var resultPatterns = new List<IEmergingPattern>();

            featureUseCount = new Dictionary<Feature, int>();
            foreach (var feature in featuresToConsider)
                featureUseCount.Add(feature, 0);

            allFeaturesUseCount = 0;
            var instanceCount = instances.Count();
            for (int i = 0; i < TreeCount; i++)
            {
                cumulativeProbabilities = new List<double>();
                double max = 0;
                for (int j = 0; j < featuresToConsider.Count; j++)
                    if (featureUseCount[featuresToConsider[j]] > max)
                        max = featureUseCount[featuresToConsider[j]];
                double sum = 0;
                for (int j = 0; j < featuresToConsider.Count; j++)
                {
                    cumulativeProbabilities.Add(allFeaturesUseCount == 0
                        ? 1.0 / featuresToConsider.Count
                        : 1.0 * (max - featureUseCount[featuresToConsider[j]]) / max);

                    //cumulativeProbabilities.Add(allFeaturesUseCount == 0
                    //    ? 1.0 / featuresToConsider.Count
                    //    : 1.0 * (featureUseCount[featuresToConsider[j]]) / allFeaturesUseCount);


                    sum += cumulativeProbabilities[j];

                    if (j > 0)
                        cumulativeProbabilities[j] += cumulativeProbabilities[j - 1];

                    if (sum != cumulativeProbabilities[j])
                        throw new Exception("Error computing cumalitive probabilities!");
                }
                for (int j = 0; j < featuresToConsider.Count; j++)
                    cumulativeProbabilities[j] /= sum;

                unsupervisedDecisionTreeBuilder.OnSelectingFeaturesToConsider =
                (features, level) => SampleWithDistribution(featuresToConsider, featureCount);

                DecisionTree tree = unsupervisedDecisionTreeBuilder.Build(model, instances, classFeature);
                DecisionTreeClassifier treeClassifier = new DecisionTreeClassifier(tree);

                if (treeClassifier.DecisionTree.Leaves > 1)
                    EpCreator.ExtractPatterns(treeClassifier,
                        delegate (EmergingPattern p)
                        {
                            if (EPTester.Test(p.Counts, model, classFeature))
                            {
                                foreach (Item item in p.Items)
                                {
                                    featureUseCount[item.Feature]++;
                                    allFeaturesUseCount++;
                                }

                                resultPatterns.Add(simplifier.Simplify(p));
                            }
                        },
                        classFeature);

                resultPatterns.Add(null);
            }

            foreach (var ep in resultPatterns)
                if (ep != null)
                {
                    ep.Counts = new double[1];
                    foreach (var instance in instances)
                        if (ep.IsMatch(instance))
                            ep.Counts[0]++;

                    ep.Supports = new double[1];
                    ep.Supports[0] = ep.Counts[0] / instanceCount;
                }

            return resultPatterns;
        }

        private IEnumerable<Feature> SampleWithDistribution(List<Feature> featuresToConsider, int featureCount)
        {
            var selectedIndexes = new HashSet<int>();
            for (int i = 0; i < featuresToConsider.Count; i++)
            {
                double probability = _randomGenerator.NextDouble();

                int idx = BinarySearch(probability, cumulativeProbabilities);
                selectedIndexes.Add(idx);
            }

            return selectedIndexes.Select(selectedIndex => featuresToConsider[selectedIndex]);
        }

        private int BinarySearch(double value, List<double> cumulativeProbabilities)
        {
            int low = 0;
            int high = cumulativeProbabilities.Count - 1;
            bool found = false;
            int iniIdx = 0;
            while (low < high && !found)
            {
                int mid = (low + high) / 2;
                if (cumulativeProbabilities[mid] > value)
                    high = mid - 1;
                else if (cumulativeProbabilities[mid] < value)
                    low = mid + 1;
                else
                {
                    found = true;
                    iniIdx = mid + 1;
                }
            }
            if (!found)
                iniIdx = low;

            return iniIdx;
        }

        public IEnumerable<IEmergingPattern> Mine(InstanceModel model, IEnumerable<Instance> instances, Feature classFeature)
        {
            EmergingPatternCreator EpCreator = new EmergingPatternCreator();
            IEmergingPatternSimplifier simplifier = new EmergingPatternSimplifier(new ItemComparer());

            List<Feature> featuresToConsider = model.Features.Where(f => f != classFeature).ToList();
            //int featureCount = (FeatureCount != -1) ? FeatureCount : Convert.ToInt32(Math.Max((int)Math.Log(featuresToConsider.Count, 2) + 1, 0.63* featuresToConsider.Count));
            int featureCount = (FeatureCount != -1) ? FeatureCount : (int)Math.Log(featuresToConsider.Count, 2) + 1;
            var resultPatterns = new List<IEmergingPattern>();

            var instanceCount = instances.Count();
            for (int i = 0; i < TreeCount; i++)
            {
                unsupervisedDecisionTreeBuilder.OnSelectingFeaturesToConsider =
                    (features, level) => _sampler.SampleWithoutRepetition(featuresToConsider, featureCount);

                DecisionTree tree = unsupervisedDecisionTreeBuilder.Build(model, instances, classFeature);
                DecisionTreeClassifier treeClassifier = new DecisionTreeClassifier(tree);

                if (treeClassifier.DecisionTree.Leaves > 1)
                    EpCreator.ExtractPatterns(treeClassifier,
                        delegate (EmergingPattern p)
                        {
                            if (EPTester.Test(p.Counts, model, classFeature))
                                resultPatterns.Add(simplifier.Simplify(p));
                        },
                        classFeature);

                resultPatterns.Add(null);
            }

            foreach (var ep in resultPatterns)
                if (ep != null)
                {
                    ep.Counts = new double[1];
                    foreach (var instance in instances)
                        if (ep.IsMatch(instance))
                            ep.Counts[0]++;

                    ep.Supports = new double[1];
                    ep.Supports[0] = ep.Counts[0] / instanceCount;
                }

            return resultPatterns;
        }

        private static RandomSampler _sampler = new RandomSampler() { RandomGenerator = new Random((int)DateTime.Now.Ticks) };

        private UD31Builder1 unsupervisedDecisionTreeBuilder;

        private Dictionary<Feature, int> featureUseCount;

        private int allFeaturesUseCount;

        private List<double> cumulativeProbabilities;

        private RandomGenerator _randomGenerator = new RandomGenerator(DateTime.Now.Millisecond);

        private int _clusterCount = 50;
    }
    
}
