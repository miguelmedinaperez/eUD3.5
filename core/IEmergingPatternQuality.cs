using System.Collections.Generic;
using PRFramework.Core.Common;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns
{
    public interface IEmergingPatternQuality
    {
        double GetQuality(IEmergingPattern pattern);
    }
}