using System;
using System.Collections.Generic;
using System.Linq;
using PRFramework.Core.Common;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns
{
    public interface IEmergingPattern
    {
        double[] Counts { get; set; }
        double[] Supports { get; set; }
        InstanceModel Model { get; set; }
        Feature ClassFeature { get; set; }

        int ClassValue { get; set; }
        bool IsMatch(Instance obj);
        IEmergingPattern Clone();
        List<Item> Items { get; set; }
        void UpdateCountsAndSupport(IEnumerable<Instance> instances);

        void UpdateCountsAndSupport(IEnumerable<Instance> instances, Feature classFeature);
    }

    [Serializable]
    public class EmergingPattern : IEmergingPattern, IFormattable
    {

        public List<Item> Items { get; set; }

        public EmergingPattern(InstanceModel model, Feature classFeature, int classValue, IEnumerable<Item> collection = null)
        {
            Model = model;
            ClassFeature = classFeature;
            ClassValue = classValue;
            Items = new List<Item>();
            if (collection != null)
                Items.AddRange(collection);
        }

        public void UpdateCountsAndSupport(IEnumerable<Instance> instances)
        {
            var matchCount = new double[(ClassFeature as NominalFeature).Values.Length];

            foreach (var instance in instances)
                if (IsMatch(instance))
                    matchCount[(int) instance[ClassFeature]]++;

            Counts = matchCount;
            Supports = EmergingPatternCreator.CalculateSupports(matchCount, ClassFeature);
        }

        public void UpdateCountsAndSupport(IEnumerable<Instance> instances, Feature classFeature)
        {
            var matchCount = new double[(classFeature as NominalFeature).Values.Length];

            foreach (var instance in instances)
                if (IsMatch(instance))
                    matchCount[(int)instance[classFeature]]++;

            Counts = matchCount;
            Supports = EmergingPatternCreator.CalculateSupports(matchCount, classFeature);
        }

        public double[] Counts { get; set; }
        public double[] Supports { get; set; }


        public Feature ClassFeature { get; set; }
        public int ClassValue { get; set; }
        public InstanceModel Model { get; set; }

        public bool IsMatch(Instance obj)
        {
            foreach (Item item in Items)
                if (!item.IsMatch(obj))
                    return false;
            return true;
        }
        public IEmergingPattern Clone()
        {
            EmergingPattern result = new EmergingPattern(Model, ClassFeature, ClassValue, Items)
                                         {
                                             Supports = (double[])Supports.Clone(),
                                             Counts = (double[])Counts.Clone(),
                                         };
            return result;
        }
        public override string ToString()
        {
            string result = BaseToString();
            string supportInfo = SupportInfo();
            return result + (supportInfo == "" ? "" : supportInfo);
        }

        private string SupportInfo()
        {
            return string.Format("{0} {1}", Counts == null ? "" : Counts.ToStringEx(0, " "),
                Supports == null ? "" : Supports.ToStringEx(4, " "));
        }

        private string BaseToString()
        {
            return string.Format("{0}",
                string.Join(" AND ", (from item in Items select $"({item.ToString()})").ToArray()));
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            switch (format)
            {
                case "P":
                    return ToString();
                case "p":
                    return BaseToString();
                case "s":
                    return SupportInfo();
            }
            return ToString();
        }
    }


}