using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees;

namespace PRFramework.Clustering
{
    [Serializable]
    public class UnsupervisedNumericDistributionEvaluator : IDistributionEvaluator
    {
        public double Evaluate(double[] parent, params double[][] children)
        {
            if (children.Length != 4)
                throw new ArgumentOutOfRangeException("Unable to evaluate UnsupervisedNumericDistributionEvaluator: invalid distribution array!");

            double parentIntervalLength = children[0][4] - children[0][3];
            double parentDispersion = children[0][2] > 0 ? children[0][2] / parentIntervalLength : 0;
            double leftChildDispersion = children[1][2] > 0 ? children[1][2] / (children[1][4] - children[1][3]) : 0;
            double rightChildDispersion = children[2][2] > 0 ? children[2][2] / (children[2][4] - children[2][3]) : 0;

            //return Math.Abs(leftChildDispersion - rightChildDispersion) / parentIntervalLength;

            //if (leftChildDispersion < parentDispersion && rightChildDispersion < parentDispersion)
            //if (children[0][2] - children[1][2] - children[2][2] > 0)
            double distributionDistance = (Math.Abs(children[1][0] - children[2][0]) - children[1][2] - children[2][2]) /
                                          parentIntervalLength;

            //return distributionDistance;

            //if (distDistance > 0)

            //double meanDistance = (children[2][0] - children[1][0])/parentIntervalLength;

            //if (counter < 10000)
            {
                counter++;
                return children[3][0];

                // Evaluación de Andrés
                //return Math.Abs(children[1][0] - children[2][0]) / parentIntervalLength;
            }
            //else
            //    return 0;
        }

        private static int counter = 0;
    }
}
