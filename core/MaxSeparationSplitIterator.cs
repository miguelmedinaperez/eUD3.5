using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PRFramework.Core.Common;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees.ChildSelectors;
using PRFramework.Core.SupervisedClassifiers.DecisionTrees.SplitIterators;

namespace PRFramework.Clustering
{
    public class MaxSeparationSplitIterator : ISplitIterator
    {
        public MaxSeparationSplitIterator()
        {
            CuttingStrategy = CuttingStrategy.OnPoint;
        }

        public Feature ClassFeature { get; set; }
        public InstanceModel Model { get; set; }
        public CuttingStrategy CuttingStrategy { get; set; }
        public double[][] CurrentDistribution { get; set; }

        private Feature _feature;
        private double _selectorFeatureValue;
        private bool _initialized = false;
        private Tuple<Instance, double>[] _sorted;
        private IEnumerable<Tuple<Instance, double>> _instances;
        public int MinInstancesCount { get; set; } = 1;

        public void Initialize(Feature feature, IEnumerable<Tuple<Instance, double>> instances)
        {
            _instances = instances;

            if (Model == null)
                throw new InvalidOperationException("Model is null");
            if (feature.FeatureType != FeatureType.Integer && feature.FeatureType != FeatureType.Double)
                throw new InvalidOperationException("Cannot use this iterator on non-numeric feature");
            _feature = feature;

            CurrentDistribution = new double[4][];

            _sorted =
                _instances.Where(x => !FeatureValue.IsMissing(x.Item1[feature])).OrderBy(x => x.Item1[feature]).ToArray();

            if (_sorted.Length == 0)
                return;

            CurrentDistribution[0] = new double[5];

            _initialized = true;

            bestResultWasFound = false;
        }

        private bool bestResultWasFound = false;

        public bool FindNext()
        {
            if (!_initialized)
                throw new InvalidOperationException("Iterator not initialized");

            if (bestResultWasFound)
                return false;

            int bestIndex = -1;

            bestResultWasFound = true;

            var splitEval = FindMaxSeparatedSets();
            bestIndex = splitEval.Item1;
            if (bestIndex == -1)
                return false;

            CurrentDistribution[1] = new double[5];
            CurrentDistribution[2] = new double[5];
            CurrentDistribution[3] = new double[1];

            CurrentDistribution[3][0] = splitEval.Item2;

            var instance = _sorted[bestIndex].Item1;
            if (CuttingStrategy == CuttingStrategy.OnPoint)
                _selectorFeatureValue = instance[_feature];
            else
                _selectorFeatureValue = (instance[_feature] + _sorted[bestIndex + 1].Item1[_feature]) / 2;

            return true;
        }

        private Tuple<int, double> FindMaxSeparatedSets1()
        {
            if (_sorted.Length < 2 || _sorted[0].Item1[_feature] == _sorted[_sorted.Length - 1].Item1[_feature])
                return new Tuple<int, double>(-1, -1);

            int low = 0;
            int high = _sorted.Length - 1;
            while (high - low > 1)
            {
                if (Math.Abs(_sorted[low + 1].Item1[_feature] - _sorted[low].Item1[_feature]) <
                    Math.Abs(_sorted[high].Item1[_feature] - _sorted[high - 1].Item1[_feature]))
                    low++;
                else
                    high--;
            }

            double leftSilouheteSum = 0;

            double a = _sorted[low].Item1[_feature] - _sorted[0].Item1[_feature];
            double b = _sorted[low + 1].Item1[_feature] - _sorted[0].Item1[_feature];
            leftSilouheteSum += (b - a) / Math.Max(b, a);

            a = _sorted[low].Item1[_feature] - _sorted[0].Item1[_feature];
            b = _sorted[low + 1].Item1[_feature] - _sorted[low].Item1[_feature];
            leftSilouheteSum += (b - a) / Math.Max(b, a);

            double rightSilouheteSum = 0;

            a = _sorted[_sorted.Length - 1].Item1[_feature] - _sorted[low + 1].Item1[_feature];
            b = _sorted[low + 1].Item1[_feature] - _sorted[low].Item1[_feature];
            rightSilouheteSum += (b - a) / Math.Max(b, a);

            a = _sorted[_sorted.Length - 1].Item1[_feature] - _sorted[low + 1].Item1[_feature];
            b = _sorted[_sorted.Length - 1].Item1[_feature] - _sorted[low].Item1[_feature];
            rightSilouheteSum += (b - a) / Math.Max(b, a);

            double eval = ((low + 1) * leftSilouheteSum / 2 + (_sorted.Length - low - 1) * rightSilouheteSum / 2) / _sorted.Length;

            //double eval = _sorted.Length * (_sorted[high].Item1[_feature] - _sorted[low].Item1[_feature]) /
            //       (_sorted[_sorted.Length - 1].Item1[_feature] - _sorted[0].Item1[_feature]);

            return new Tuple<int, double>(low, eval);
        }

        private Tuple<int, double> FindMaxSeparatedSetsGood3()
        {
            double maxSilhouetteValue = 0;
            int bestSplitCursor = -1;

            if (_sorted[0].Item1[_feature] != _sorted[_sorted.Length - 1].Item1[_feature])
            {
                double leftSum = _sorted[0].Item1[_feature];
                double previousLeftMean = _sorted[0].Item1[_feature];
                double leftMean = previousLeftMean;
                int leftMeanCursor = 0;

                double rightSum = 0;
                for (int i = 1; i < _sorted.Length; i++)
                    rightSum += _sorted[i].Item1[_feature];
                double previousRightMean = rightSum / (_sorted.Length - 1);
                double rightMean = previousRightMean;
                int rightMeanCursor = 1;
                while (_sorted[rightMeanCursor].Item1[_feature] <= rightMean)
                    rightMeanCursor++;

                for (int i = 1; i < _sorted.Length - 1; i++)
                {
                    if (_sorted[i].Item1[_feature] > _sorted[i - 1].Item1[_feature])
                    {
                        double currentSilhouette = 0;

                        for (int j = 0; j < i; j++)
                        {
                            double sameClusterDistance = Math.Abs(_sorted[j].Item1[_feature] - leftMean);
                            double differentClusterAverageDistance = Math.Abs(_sorted[j].Item1[_feature] - rightMean);
                            currentSilhouette += (differentClusterAverageDistance - sameClusterDistance) /
                                             Math.Max(differentClusterAverageDistance, sameClusterDistance);
                        }

                        for (int j = i; j < _sorted.Length; j++)
                        {
                            double sameClusterDistance = Math.Abs(_sorted[j].Item1[_feature] - rightMean);
                            double differentClusterAverageDistance = Math.Abs(_sorted[j].Item1[_feature] - leftMean);
                            currentSilhouette += (differentClusterAverageDistance - sameClusterDistance) /
                                             Math.Max(differentClusterAverageDistance, sameClusterDistance);
                        }

                        currentSilhouette /= _sorted.Length;

                        if (currentSilhouette < -1 || currentSilhouette > 1)
                            throw new Exception("Invalid feature Mean Square Error");

                        if (currentSilhouette > maxSilhouetteValue)
                        {
                            maxSilhouetteValue = currentSilhouette;
                            bestSplitCursor = i - 1;
                        }
                    }

                    leftSum += _sorted[i].Item1[_feature];
                    leftMean = leftSum / (i);

                    rightSum -= _sorted[i].Item1[_feature];
                    rightMean = rightSum / (_sorted.Length - i);
                }
            }

            return new Tuple<int, double>(bestSplitCursor, maxSilhouetteValue);
        }

        private Tuple<int, double> FindMaxSeparatedSetsGood2()
        {
            int bestIndex = -1;
            double leftMean = 0;
            double rightMean = 0;
            double bestValidityIndex = 0;
            if (_sorted[0].Item1[_feature] != _sorted[_sorted.Length - 1].Item1[_feature])
            {
                double leftSum = _sorted[0].Item1[_feature];
                double rightSum = 0;
                for (int i = 1; i < _sorted.Length; i++)
                    rightSum += _sorted[i].Item1[_feature];

                for (int i = 1; i < _sorted.Length - 1; i++)
                {
                    if (_sorted[i].Item1[_feature] > _sorted[i - 1].Item1[_feature])
                    {
                        double currentSilhouette = 0;
                        var currentLeftMean = leftSum / (i);
                        var currentRightMean = rightSum / (_sorted.Length - i);

                        for (int j = 0; j < i; j++)
                        {
                            double sameClusterDistance = Math.Abs(_sorted[j].Item1[_feature] - currentLeftMean);
                            double differentClusterAverageDistance = Math.Abs(_sorted[j].Item1[_feature] - currentRightMean);
                            currentSilhouette += (differentClusterAverageDistance - sameClusterDistance) /
                                             Math.Max(differentClusterAverageDistance, sameClusterDistance);
                        }

                        for (int j = i; j < _sorted.Length; j++)
                        {
                            double sameClusterDistance = Math.Abs(_sorted[j].Item1[_feature] - currentRightMean);
                            double differentClusterAverageDistance = Math.Abs(_sorted[j].Item1[_feature] - currentLeftMean);
                            currentSilhouette += (differentClusterAverageDistance - sameClusterDistance) /
                                             Math.Max(differentClusterAverageDistance, sameClusterDistance);
                        }

                        currentSilhouette /= _sorted.Length;

                        if (currentSilhouette < -1 || currentSilhouette > 1)
                            throw new Exception("Invalid feature Mean Square Error");

                        if (currentSilhouette > bestValidityIndex)
                        {
                            bestValidityIndex = currentSilhouette;
                            bestIndex = i - 1;

                            leftMean = currentLeftMean;
                            rightMean = currentRightMean;
                        }
                    }

                    leftSum += _sorted[i].Item1[_feature];
                    rightSum -= _sorted[i].Item1[_feature];
                }
            }

            return new Tuple<int, double>(bestIndex, bestValidityIndex);
        }

        private Tuple<int, double> FindMaxSeparatedSets()
        {
            int bestIndex = -1;
            double bestSilhouetteSum = -1;
            double leftMean = 0;
            double rightMean = 0;
            if (_sorted[0].Item1[_feature] != _sorted[_sorted.Length - 1].Item1[_feature])
            {
                double leftSum = _sorted[0].Item1[_feature];
                double rightSum = 0;
                for (int i = 1; i < _sorted.Length; i++)
                    rightSum += _sorted[i].Item1[_feature];

                double bestValidityIndex = double.MinValue;
                for (int i = 1; i < _sorted.Length - 1; i++)
                {
                    if (_sorted[i].Item1[_feature] > _sorted[i - 1].Item1[_feature])
                    {
                        var currentLeftMean = leftSum / (i);
                        var currentRightMean = rightSum / (_sorted.Length - i);

                        double silouheteSum = 0;
                        int low = i - 1;
                        int high = i;

                        double a = currentLeftMean - _sorted[0].Item1[_feature];
                        double b = currentRightMean - _sorted[0].Item1[_feature];
                        double l1 = ((b - a) / Math.Max(b, a) + 1) / 2;

                        a = _sorted[low].Item1[_feature] - currentLeftMean;
                        b = currentRightMean - _sorted[low].Item1[_feature];
                        double l2 = ((b - a) / Math.Max(b, a) + 1) / 2;

                        a = currentRightMean - _sorted[high].Item1[_feature];
                        b = _sorted[high].Item1[_feature] - currentLeftMean;
                        double r1 = ((b - a) / Math.Max(b, a) + 1) / 2;

                        a = _sorted[_sorted.Length - 1].Item1[_feature] - currentRightMean;
                        b = _sorted[_sorted.Length - 1].Item1[_feature] - currentLeftMean;
                        double r2 = ((b - a) / Math.Max(b, a) + 1) / 2;

                        if (l1 < 0 || l2 < 0 || r1 < 0 || r2 < 0)
                            throw new Exception("Bad Silluete computation!");

                        double eval = i * (l1 + l2) / 2 + (_sorted.Length - i) * (r1 + r2) / 2;

                        if (eval > bestValidityIndex)
                        {
                            bestValidityIndex = eval;
                            bestIndex = i - 1;

                            leftMean = currentLeftMean;
                            rightMean = currentRightMean;
                        }
                    }

                    leftSum += _sorted[i].Item1[_feature];
                    rightSum -= _sorted[i].Item1[_feature];
                }
            }

            bestSilhouetteSum = 0;
            for (int j = 0; j <= bestIndex; j++)
            {
                double sameClusterDistance = Math.Abs(_sorted[j].Item1[_feature] - leftMean);
                double differentClusterDistance = Math.Abs(_sorted[j].Item1[_feature] - rightMean);
                bestSilhouetteSum += (differentClusterDistance - sameClusterDistance) /
                                 Math.Max(differentClusterDistance, sameClusterDistance);
            }

            for (int j = bestIndex + 1; j < _sorted.Length; j++)
            {
                double sameClusterDistance = Math.Abs(_sorted[j].Item1[_feature] - rightMean);
                double differentClusterDistance = Math.Abs(_sorted[j].Item1[_feature] - leftMean);
                bestSilhouetteSum += (differentClusterDistance - sameClusterDistance) /
                                 Math.Max(differentClusterDistance, sameClusterDistance);
            }

            return new Tuple<int, double>(bestIndex, bestSilhouetteSum / _sorted.Length);
        }

        private Tuple<int, double> FindMaxSeparatedSetsPipo()
        {
            int bestIndex = -1;
            double bestSilhouetteSum = -1;
            double leftMean = 0;
            double rightMean = 0;
            int startIdx = Convert.ToInt32(0.15 * _sorted.Length);
            if (_sorted[0].Item1[_feature] != _sorted[_sorted.Length - 1].Item1[_feature])
            {
                double leftSum = _sorted[0].Item1[_feature];
                double rightSum = 0;
                for (int i = 1; i < _sorted.Length; i++)
                    rightSum += _sorted[i].Item1[_feature];

                double bestValidityIndex = double.MinValue;
                for (int i = 1; i < _sorted.Length - startIdx; i++)
                {
                    if (i > startIdx && _sorted[i].Item1[_feature] > _sorted[i - 1].Item1[_feature])
                    {
                        var currentLeftMean = leftSum / (i);
                        var currentRightMean = rightSum / (_sorted.Length - i);

                        double eval = currentRightMean - currentLeftMean;

                        if (eval > bestValidityIndex)
                        {
                            bestValidityIndex = eval;
                            bestIndex = i - 1;

                            leftMean = currentLeftMean;
                            rightMean = currentRightMean;
                        }
                    }

                    leftSum += _sorted[i].Item1[_feature];
                    rightSum -= _sorted[i].Item1[_feature];
                }
            }

            return new Tuple<int, double>(bestIndex, rightMean - leftMean);
        }

        public IChildSelector CreateCurrentChildSelector()
        {
            return new CutPointSelector()
            {
                CutPoint = _selectorFeatureValue,
                Feature = _feature,
            };
        }
    }
}
