using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PRFramework.Core.Common;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees.SplitIterators;

namespace PRFramework.Clustering
{
    [Serializable]
    public class UD31SplitIteratorProvider : ISplitIteratorProvider
    {
        [NonSerialized]
        private Dictionary<Type, ISplitIterator> iterators;

        public int MinInstancesCount { get; set; } = 50;

        public UD31SplitIteratorProvider()
        {
            InitializeIterators();
        }

        private void InitializeIterators()
        {
            iterators = new Dictionary<Type, ISplitIterator>();
            iterators.Add(typeof(IntegerFeature), new MaxSeparationSplitIterator
            {
                CuttingStrategy = CuttingStrategy.OnPoint,
                MinInstancesCount = MinInstancesCount
            });
            iterators.Add(typeof(DoubleFeature),
                new MaxSeparationSplitIterator
                {
                    CuttingStrategy = CuttingStrategy.CenterBetweenPoints,
                    MinInstancesCount = MinInstancesCount
                });
        }

        public ISplitIterator GetSplitIterator(InstanceModel model, Feature feature, Feature classFeature)
        {
            if (iterators == null)
                InitializeIterators();
            ISplitIterator result;
            if (iterators.TryGetValue(feature.GetType(), out result))
            {
                result.Model = model;
                result.ClassFeature = classFeature;
                return result;
            }
            else
                return null;
        }
    }
}
