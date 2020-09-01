using System;
using System.Collections.Generic;
using System.Linq;
using PRFramework.Core.Common;
using PRFramework.Core.DatasetInfo;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees.ChildSelectors;
using System.Collections;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns
{
    [Serializable]
    public class EmergingPatternCreator
    {
        private Dictionary<Type, ItemBuilder>
            builderForType = new Dictionary<Type, ItemBuilder>
                                 {
                                     {typeof (CutPointSelector), new CutPointBasedBuilder()},
                                     {typeof (MultivariateCutPointSelector), new MultivariateCutPointBasedBuilder()},
                                     {typeof (ValueAndComplementSelector), new ValueAndComplementBasedBuilder()},
                                     {typeof (MultipleValuesSelector), new MultipleValuesBasedBuilder()},
                                 };
    

        public EmergingPattern Create(IEnumerable<SelectorContext> contexes, InstanceModel model, Feature classFeature)
        {
            EmergingPattern result = new EmergingPattern(model, classFeature, 0);
            foreach (SelectorContext context in contexes)
            {
                IChildSelector childSelector = context.Selector;
                ItemBuilder builder;
                if (!builderForType.TryGetValue(childSelector.GetType(), out builder))
                    throw new InvalidOperationException(string.Format("Unknown selector: '{0}'", childSelector.GetType().Name));
                Item item = builder.GetItem(childSelector, context.Index);
                item.Model = model;
                result.Items.Add(item);
            }
            return result;
        }
        
        public List<EmergingPattern> ExtractPatterns(DecisionTreeClassifier tree,
            Feature classFeature)
        {
            List<EmergingPattern> result = new List<EmergingPattern>();
            ExtractPatterns(tree, ep => result.Add(ep), classFeature);
            return result;
        }

        public void ExtractPatterns(DecisionTreeClassifier tree, 
            Action<EmergingPattern> patternFound,
            Feature classFeature)
        {
            List<SelectorContext> context = new List<SelectorContext>();
            DoExtractPatterns(tree.DecisionTree.TreeRootNode, context, 
                tree.Model, patternFound, classFeature);
        }


        private void DoExtractPatterns(IDecisionTreeNode node, 
            List<SelectorContext> contexts, InstanceModel model, 
            Action<EmergingPattern> patternFound, 
            Feature classFeature)
        {
            if (node.IsLeaf)
            {
                EmergingPattern newPattern = Create(contexts, model, classFeature);
                newPattern.Counts = node.Data;
                newPattern.Supports = CalculateSupports(node.Data, classFeature);
                newPattern.ClassValue = newPattern.Supports.ArgMax();
                if (patternFound != null)
                    patternFound(newPattern);
            }
            else
            {
                for (int i = 0; i < node.Children.Length; i++)
                {
                    SelectorContext context = new SelectorContext
                                                  {
                                                      Index = i,
                                                      Selector = node.ChildSelector,
                                                  };
                    contexts.Add(context);
                    DoExtractPatterns(node.Children[i], contexts, model, patternFound,
                        classFeature);
                    contexts.Remove(context);
                }
            }
        }

        public static double[] CalculateSupports(double[] data, Feature classFeature)
        {
            NominalFeature feat = (NominalFeature)classFeature;
            var featureInformation = (feat.FeatureInformation as NominalFeatureInformation);
            double[] result = (double[])data.Clone();
            for (int i = 0; i < result.Length; i++)
                result[i] = featureInformation.Distribution[i] != 0
                                ? result[i]/featureInformation.Distribution[i]
                                : 0;
            return result;
        }

        public static double[] CalculateSupports(double[] data, double[] classDistribution)
        {
            double[] result = (double[])data.Clone();
            for (int i = 0; i < result.Length; i++)
                result[i] = classDistribution[i] != 0
                                ? result[i] / classDistribution[i]
                                : 0;
            return result;
        }

        private abstract class ItemBuilder
        {
            public abstract Item GetItem(IChildSelector generalSelector, int index);
        }

        private class ValueAndComplementBasedBuilder : ItemBuilder
        {
            public override Item GetItem(IChildSelector generalSelector, int index)
            {
                ValueAndComplementSelector selector = (ValueAndComplementSelector)generalSelector;
                if (index == 0)
                    return new EqualThanItem()
                    {
                        Feature = selector.Feature,
                        Value = selector.Value
                    };
                else if (index == 1)
                    return new DifferentThanItem()
                    {
                        Feature = selector.Feature,
                        Value = selector.Value
                    };
                else
                    throw new InvalidOperationException("Invalid index value for ValueAndComplementSelector");
            }
        }

        private class CutPointBasedBuilder : ItemBuilder
        {
            public override Item GetItem(IChildSelector generalSelector, int index)
            {
                CutPointSelector selector = (CutPointSelector)generalSelector;
                if (index == 0)
                    return new LessOrEqualThanItem
                    {
                        Feature = selector.Feature,
                        Value = selector.CutPoint
                    };
                else if (index == 1)
                    return new GreatherThanItem
                    {
                        Feature = selector.Feature,
                        Value = selector.CutPoint
                    };
                else
                    throw new InvalidOperationException("Invalid index value for CutPointSelector");
            }
        }
        private class MultivariateCutPointBasedBuilder : ItemBuilder
        {
            public override Item GetItem(IChildSelector generalSelector, int index)
            {
                MultivariateCutPointSelector selector = (MultivariateCutPointSelector)generalSelector;

                if (index == 0)
                    return new MultivariateLessOrEqualThanItem(selector.Weights)
                    {
                        Features = selector.Features,
                        Value = selector.CutPoint,
                        FeaturesHash = ((IStructuralEquatable)selector.Features).GetHashCode(EqualityComparer<Feature>.Default),
                    };
                else if (index == 1)
                    return new MultivariateGreatherThanItem(selector.Weights)
                    {
                        Features = selector.Features,
                        Value = selector.CutPoint,
                        FeaturesHash = ((IStructuralEquatable)selector.Features).GetHashCode(EqualityComparer<Feature>.Default),
                    };
                else
                    throw new InvalidOperationException("Invalid index value for CutPointSelector");
            }
        }

        private class MultipleValuesBasedBuilder : ItemBuilder
        {
            public override Item GetItem(IChildSelector generalSelector, int index)
            {
                MultipleValuesSelector selector = (MultipleValuesSelector) generalSelector;
                if (index < 0 || index >= selector.Values.Length)
                    throw new InvalidOperationException("Invalid index value for MultipleValuesSelector");
                return new EqualThanItem
                           {
                               Feature = selector.Feature,
                               Value = selector.Values[index]
                           };
            }
        }

        public EmergingPattern ExtractPattern(List<SelectorContext> currentContext, InstanceModel model,
            Feature classFeature, IChildSelector selector, int index)
        {
            return
                Create(
                    currentContext.Union(new List<SelectorContext>
                                             {
                                                 new SelectorContext
                                                     {
                                                         Selector = selector,
                                                         Index = index
                                                     }
                                             }), model,classFeature);
        }
    }
}
