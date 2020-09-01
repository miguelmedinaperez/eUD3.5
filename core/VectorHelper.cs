using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PRFramework.Core.Common;


namespace PRFramework.Core.Core.SupervisedClassifiers.DecisionTrees.Vectors
{
    class VectorHelper
    {
        public static double ScalarProjection(Instance instance, Feature[] Features, IDictionary<Feature, double> Weights)
        {
            if (FeatureValue.IsMissing(instance[Features]))
                return double.NaN;

            double result = 0;
            foreach (var feature in Features)
            {
                result += Weights[feature] * instance[feature];
            }
            return result;
        }


    }
}
