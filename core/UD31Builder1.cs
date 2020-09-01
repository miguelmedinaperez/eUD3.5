using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PRFramework.Core.Common;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees.Builder;

namespace PRFramework.Clustering
{
    [Serializable]
    public class UD31Builder1
    {
        public UD31Builder1()
        {
            MinimalSplitGain = 0;
            MaxDepth = -1;
            InitialDistributionCalculator = FindDistribution;
        }

        public int ClusterCount { get; set; } = 30;

        public Action<IDecisionTreeNode, ISplitIterator, List<SelectorContext>> OnSplitEvaluation { get; set; }

        public IDistributionEvaluator DistributionEvaluator { get; set; }

        public Func<IChildSelector, int, bool> CanAcceptChildSelector = (x, level) => true;

        public ISplitIteratorProvider SplitIteratorProvider { get; set; }

        public double MinimalSplitGain { get; set; }

        public Func<IDecisionTreeNode, int, int> OnSelectingWhichBetterSplit { get; set; }
        public Func<IEnumerable<Feature>, int, IEnumerable<Feature>> OnSelectingFeaturesToConsider = (f, level) => level <= 10000000 ? f : new List<Feature>();
        public Func<IEnumerable<Tuple<Instance, double>>, InstanceModel, Feature, double[]> InitialDistributionCalculator { get; set; }

        public int MaxDepth { get; set; }

        public DecisionTree Build(InstanceModel model, IEnumerable<Instance> instances, Feature classFeature)
        {
            DecisionTree result = Build(model, instances.Select(x => Tuple.Create(x, 1d)), classFeature);
            result.Model = model;
            return result;
        }

        public DecisionTree Build(InstanceModel model, IEnumerable<Tuple<Instance, double>> objMembership,
            Feature classFeature)
        {
            List<SelectorContext> currentContext = new List<SelectorContext>();
            if (SplitIteratorProvider == null)
                throw new InvalidOperationException("SplitIteratorProvider not defined");

            DecisionTree result = new DecisionTree
            {
                Model = model,
            };

            //double[] parentDistribution = InitialDistributionCalculator(objMembership, model, classFeature);
            double[] parentDistribution = new double[] { 1, 1, 1, 1, 1 };
            result.TreeRootNode = new DecisionTreeNode(parentDistribution);

            var validityIndexByNode = new Dictionary<IDecisionTreeNode, double>();
            validityIndexByNode.Add(result.TreeRootNode, 0);
            var instancesByNode = new Dictionary<IDecisionTreeNode, IEnumerable<Tuple<Instance, double>>>();
            instancesByNode.Add(result.TreeRootNode, objMembership);
            var levelByNode = new Dictionary<IDecisionTreeNode, int>();
            levelByNode.Add(result.TreeRootNode, 0);
            int leafCount = 1;
            FillNode(ref validityIndexByNode, model, ref instancesByNode, classFeature, ref levelByNode, currentContext, ref leafCount);

            return result;
        }

        private double[] FindDistribution(IEnumerable<Tuple<Instance, double>> source, InstanceModel model,
            Feature classFeature)
        {
            if (classFeature.FeatureType != FeatureType.Nominal)
                throw new InvalidOperationException("Cannot find distribution for non-nominal class");
            double[] result = new double[((NominalFeature)classFeature).Values.Length];
            foreach (var tuple in source)
                if (!FeatureValue.IsMissing(tuple.Item1[classFeature]))
                {
                    int value = (int)tuple.Item1[classFeature];
                    result[value] += tuple.Item2;
                }
            return result;
        }

        private void FillNode(ref Dictionary<IDecisionTreeNode, double> validityIndexByNode, InstanceModel model, ref Dictionary<IDecisionTreeNode, IEnumerable<Tuple<Instance, double>>> instancesByNode,
            Feature classFeature, ref Dictionary<IDecisionTreeNode, int> levelByNode, List<SelectorContext> currentContext, ref int leafCount)
        {
            IDecisionTreeNode node = null;
            double bestIndexValue = Double.MinValue;
            foreach (var currentNode in validityIndexByNode.Keys)
                if (bestIndexValue < validityIndexByNode[currentNode])
                {
                    bestIndexValue = validityIndexByNode[currentNode];
                    node = currentNode;
                }

            if (node != null)
            {
                int level = levelByNode[node];
                var instances = instancesByNode[node];

                int whichBetterToFind = 1;
                if (OnSelectingWhichBetterSplit != null)
                    whichBetterToFind = OnSelectingWhichBetterSplit(node, level);
                WiningSplitSelector winingSplitSelector = new WiningSplitSelector(whichBetterToFind)
                {
                    CanAcceptChildSelector = this.CanAcceptChildSelector,
                };
                foreach (var feature in OnSelectingFeaturesToConsider(model.Features, level))
                    if (feature != classFeature)
                    {
                        ISplitIterator splitIterator = SplitIteratorProvider.GetSplitIterator(model, feature, classFeature);
                        if (splitIterator == null)
                            throw new InvalidOperationException(string.Format("Undefined iterator for feature {0}",
                                feature));
                        splitIterator.Initialize(feature, instances);
                        while (splitIterator.FindNext())
                        {
                            double currentGain = DistributionEvaluator.Evaluate(node.Data,
                                                                                splitIterator.CurrentDistribution);
                            if (currentGain > MinimalSplitGain || leafCount < ClusterCount)
                            {
                                if (OnSplitEvaluation != null)
                                    OnSplitEvaluation(node, splitIterator, currentContext);
                                winingSplitSelector.EvaluateThis(currentGain, splitIterator, level);
                            }
                        }
                    }

                if (winingSplitSelector.IsWinner())
                {
                    IChildSelector maxSelector = winingSplitSelector.WinningSelector;
                    node.ChildSelector = maxSelector;
                    node.Children = new IDecisionTreeNode[maxSelector.ChildrenCount];
                    var instancesPerChildNode =
                        childrenInstanceCreator.CreateChildrenInstances(instances, maxSelector, double.MinValue);

                    for (int i = 0; i < maxSelector.ChildrenCount; i++)
                    {
                        var childNode = new DecisionTreeNode { Parent = node };
                        node.Children[i] = childNode;
                        childNode.Data = winingSplitSelector.WinningDistribution[i];
                        SelectorContext context = null;
                        if (OnSplitEvaluation != null)
                        {
                            context = new SelectorContext
                            {
                                Index = i,
                                Selector = node.ChildSelector,
                            };
                            currentContext.Add(context);
                        }

                        double currentBestValidityIndex = double.MinValue;
                        foreach (var feature in OnSelectingFeaturesToConsider(model.Features, level))
                            if (feature != classFeature)
                            {
                                ISplitIterator splitIterator = SplitIteratorProvider.GetSplitIterator(model, feature, classFeature);
                                if (splitIterator == null)
                                    throw new InvalidOperationException(string.Format("Undefined iterator for feature {0}",
                                        feature));
                                splitIterator.Initialize(feature, instancesPerChildNode[i]);
                                while (splitIterator.FindNext())
                                {
                                    double currentGain = DistributionEvaluator.Evaluate(node.Data,
                                                                                        splitIterator.CurrentDistribution);
                                    if (currentGain > currentBestValidityIndex)
                                    {
                                        if (OnSplitEvaluation != null)
                                            OnSplitEvaluation(node, splitIterator, currentContext);

                                        currentBestValidityIndex = currentGain;
                                    }
                                }
                            }

                        if (currentBestValidityIndex > validityIndexByNode[node] || leafCount < ClusterCount)
                        {
                            validityIndexByNode.Add(childNode, currentBestValidityIndex);
                            instancesByNode.Add(childNode, instancesPerChildNode[i]);
                            levelByNode.Add(childNode, level + 1);
                        }

                        if (OnSplitEvaluation != null)
                            currentContext.Remove(context);
                    }

                    validityIndexByNode.Remove(node);
                    instancesByNode.Remove(node);
                    levelByNode.Remove(node);
                    leafCount++;

                    if (leafCount < 4 * ClusterCount)
                        FillNode(ref validityIndexByNode, model, ref instancesByNode, classFeature, ref levelByNode, currentContext, ref leafCount);
                }
            }
        }

        ChildrenInstanceCreator childrenInstanceCreator = new ChildrenInstanceCreator();
    }
}
