using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PRFramework.Core.Common;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns
{
    public interface IPatternTest
    {
        bool Test(IEmergingPattern pattern);
    }

    public static class PatternTestLinq
    {
        public static bool Test(this IPatternTest test, double[] distribution, InstanceModel model, Feature classFeature)
        {
            var pattern = new EmergingPattern(model, classFeature, 0);
            pattern.Counts = distribution;
            pattern.Supports = EmergingPatternCreator.CalculateSupports(distribution, classFeature);
            pattern.ClassValue = pattern.Supports.ArgMax();
            return test.Test(pattern);
        }
    }
}
