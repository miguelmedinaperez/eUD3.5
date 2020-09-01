using System;
using System.Collections.Generic;
using PRFramework.Core.Common;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees.Builder;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees.DistributionTesters;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns.Miners
{
    public interface ITreeBasedMiner : IEmergingPatternMiner
    {
        IDecisionTreeBuilder DecisionTreeBuilder { get; set; }
        bool MinePatternsWhileBuildingTree { get; set; }
        IPatternTest EPTester { get; set; }
        SubsetRelation FilterRelation { get; set; }
    }

    [Serializable]
    public abstract class TreeBasedMiner : ITreeBasedMiner
    {
        protected TreeBasedMiner()
        {
            EPTester = new AlwaysTrue();
            FilterRelation = SubsetRelation.Superset;
        }

        public IDecisionTreeBuilder DecisionTreeBuilder { get; set; }
        public bool MinePatternsWhileBuildingTree { get; set; }
        public IPatternTest EPTester { get; set; }

        public SubsetRelation FilterRelation { get; set; }

        public IEnumerable<IEmergingPattern> Mine(InstanceModel model, IEnumerable<Instance> instances, Feature classFeature)
        {
            EmergingPatternCreator EpCreator = new EmergingPatternCreator();
            EmergingPatternComparer epComparer = new EmergingPatternComparer(new ItemComparer());
            IEmergingPatternSimplifier simplifier = new EmergingPatternSimplifier(new ItemComparer());
            FilteredCollection<IEmergingPattern> minimal =
                new FilteredCollection<IEmergingPattern>(epComparer.Compare, FilterRelation);

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
                                minimal.Add(simplifier.Simplify(ep));
                            }
                        }
                    };
                DoMine(model, instances, classFeature, EpCreator, null);
            }
            else
                DoMine(model, instances, classFeature, EpCreator, p =>
                                                                      {
                                                                          if (EPTester.Test(p.Counts, model, classFeature))
                                                                              minimal.Add(simplifier.Simplify(p));
                                                                      }
                    );
            return minimal.GetItems();
        }

        protected abstract void DoMine(InstanceModel model, IEnumerable<Instance> instances, Feature classFeature,
            EmergingPatternCreator epCreator, Action<EmergingPattern> action);

    }
}