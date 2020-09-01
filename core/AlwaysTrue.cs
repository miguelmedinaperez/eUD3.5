using System;
using PRFramework.Core.Common;
using PRFramework.Core.SupervisedClassifiers.EmergingPatterns;
using PRFramework.Core.SupervisedClassifiers.EmergingPatterns.Classifiers;

namespace PRFramework.Core.SupervisedClassifiers.DecisionTrees.DistributionTesters
{
    [Serializable]
    public class AlwaysTrue : IPatternTest
    {
        public bool Test(IEmergingPattern patttern)
        {
            return true;
        }
    }
}