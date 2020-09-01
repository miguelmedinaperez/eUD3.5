using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PRFramework.Core.Common;
using PRFramework.Core.Core.SupervisedClassifiers.DecisionTrees.Vectors;

namespace PRFramework.Core.SupervisedClassifiers.DecisionTrees.ChildSelectors
{
    class MultivariateCutPointSelector : MultipleFeaturesSelector, IChildSelector
    {
        public int ChildrenCount { get { return 2; } }

        public double CutPoint { get; set; }
        public Dictionary<Feature, double> Weights { get; set; }


        public double[] Select(Instance instance)
        {
            if (Features.Any(p => p.FeatureType == FeatureType.Nominal))
                throw new InvalidOperationException("Cannot use cutpoint on nominal data");
            if (Features.Any(p => FeatureValue.IsMissing(instance[p])))
                return null;
            return VectorHelper.ScalarProjection(instance, Features, Weights) <= CutPoint ? new double[] { 1, 0 } : new double[] { 0, 1 };
        }

        public string ToString(InstanceModel model, int index)
        {
            throw new NotImplementedException();
        }



    }
}
