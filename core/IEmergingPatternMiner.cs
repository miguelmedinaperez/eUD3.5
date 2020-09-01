using System.Collections.Generic;
using PRFramework.Core.Common;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns.Miners
{
    public interface IEmergingPatternMiner
    {
        IEnumerable<IEmergingPattern> Mine(InstanceModel model, IEnumerable<Instance> instances, Feature classFeature);
    }
}