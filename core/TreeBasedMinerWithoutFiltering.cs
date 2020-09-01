using System;
using System.Collections.Generic;
using PRFramework.Core.Common;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees.Builder;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees.DistributionTesters;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns.Miners
{
    [Serializable]
    public abstract class TreeBasedMinerWithoutFiltering : ITreeBasedMiner
    {
        protected TreeBasedMinerWithoutFiltering()
        {
            EPTester = new AlwaysTrue();
            FilterRelation = SubsetRelation.Superset;
        }

        public IDecisionTreeBuilder DecisionTreeBuilder { get; set; }
        public bool MinePatternsWhileBuildingTree { get; set; }
        public IPatternTest EPTester { get; set; }

        public SubsetRelation FilterRelation { get; set; }

        public bool Multivariate = false;
        public IEnumerable<IEmergingPattern> Mine(InstanceModel model, IEnumerable<Instance> instances, Feature classFeature)
        {
            EmergingPatternCreator EpCreator = new EmergingPatternCreator();
            IEmergingPatternSimplifier simplifier;

            if (Multivariate)
                simplifier = new EmergingPatternSimplifier(new MultivariateItemComparer());
            else
                simplifier = new EmergingPatternSimplifier(new ItemComparer());

            List<IEmergingPattern> patternsList = new List<IEmergingPattern>();

            if (MinePatternsWhileBuildingTree)
            {
                DecisionTreeBuilder.OnSplitEvaluation =
                    delegate(IDecisionTreeNode node, ISplitIterator iterator, List<SelectorContext> currentContext)
                    {
                        IChildSelector currentSelector = null;
                        for (int i = 0; i < iterator.CurrentDistribution.Length; i++)
                        {
                            double[] distribution = iterator.CurrentDistribution[i];
                            if (EPTester.Test(distribution, model, classFeature))
                            {
                                if (currentSelector == null)
                                    currentSelector = iterator.CreateCurrentChildSelector();
                                EmergingPattern ep = EpCreator.ExtractPattern(currentContext, model, classFeature,
                                                                              currentSelector, i);
                                ep.Counts = (double[])distribution.Clone();
                                patternsList.Add(simplifier.Simplify(ep));
                            }
                        }
                    };
                DoMine(model, instances, classFeature, EpCreator, null);
            }
            else
                DoMine(model, instances, classFeature, EpCreator, p =>
                                                                      {
                                                                          if (EPTester.Test(p.Counts, model, classFeature))
                                                                              patternsList.Add(simplifier.Simplify(p));
                                                                      }
                    );
            return patternsList;
        }

        protected abstract void DoMine(InstanceModel model, IEnumerable<Instance> instances, Feature classFeature,
            EmergingPatternCreator epCreator, Action<EmergingPattern> action);

    }
}