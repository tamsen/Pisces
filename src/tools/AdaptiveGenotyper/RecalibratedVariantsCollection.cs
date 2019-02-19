﻿using System;
using System.Collections.Generic;
using Pisces.IO.Sequencing;
using Pisces.Calculators;
using Pisces.Genotyping;

namespace AdaptiveGenotyper
{

    public class RecalibratedVariant
    {
        public int Dp { get; }
        public int Ad { get; }
        public MixtureModelResult MixtureModelResult { get; }

        public RecalibratedVariant(int dp, int ad, int category, int qScore, float[] gp)
        {
            Dp = dp;
            Ad = ad;
            MixtureModelResult = new MixtureModelResult();
            MixtureModelResult.GenotypeCategory = MixtureModelResult.IntToGentoptypeCategory(category);
            MixtureModelResult.QScore = qScore;
            MixtureModelResult.GenotypePosteriors = gp;
        }
    }
    public class RecalibratedVariantsCollection
    {
        private Dictionary<string, int> ChrIndexer = new Dictionary<string, int>();
        private Dictionary<int, string> NumIndexer = new Dictionary<int, string>();
        public int Count { get; private set; } = 0;
        public List<String> ReferenceName { get; } = new List<string>();
        public List<int> ReferencePosition { get; } = new List<int>();
        public IList<int> Dp { get; } = new List<int>();
        public IList<int> Ad { get; } = new SparseArray<int>();
        public IList<int> Categories { get; private set; }
        public IList<int> QScores { get; private set; }
        public IList<float[]> Gp { get; private set; }


        public RecalibratedVariant this[string key]
        {
            get
            {
                int i = ChrIndexer[key];
                return new RecalibratedVariant(Dp[i], Ad[i], Categories[i], QScores[i], Gp[i]);
            }
        }

        public bool ContainsKey(string key)
        {
            return ChrIndexer.ContainsKey(key);
        }

        public void AddMixtureModelResults(MixtureModel model)
        {
            if (Dp.Count != model.Clustering.Count)
                throw new Exception("Model does not come from same data source.");

            Categories = model.Clustering;
            Gp = model.PhredPosteriors;
            QScores = model.QScores;
        }

        public void AddLocus(VcfVariant variant)
        {
            ReferenceName.Add(variant.ReferenceName);
            ReferencePosition.Add(variant.ReferencePosition);
            ChrIndexer.Add(variant.ReferenceName + ":" + variant.ReferencePosition.ToString(), Count);
            NumIndexer.Add(Count, variant.ReferenceName + ":" + variant.ReferencePosition.ToString());
            int dp = VariantReader.ParseDepth(variant);
            if (dp < AdaptiveGenotyperQualityCalculator.MaxEffectiveDepth)
            {
                Dp.Add(dp);
                Ad.Add(VariantReader.ParseAlleleSupport(variant));
            }
            else
            {
                var (ad, depth) = AdaptiveGenotyperQualityCalculator.DownsampleVariant(
                    VariantReader.ParseAlleleSupport(variant), dp);
                Dp.Add(depth);
                Ad.Add(ad);
            }
            Count++;
        }

        public void RemoveLastEntry()
        {
            Count--;
            ReferenceName.RemoveAt(Count);
            ReferencePosition.RemoveAt(Count);
            ChrIndexer.Remove(NumIndexer[Count]);
            NumIndexer.Remove(Count);
            Ad.RemoveAt(Count);
            Dp.RemoveAt(Count);

            if (Categories != null)
            {
                Categories.RemoveAt(Count);
                QScores.RemoveAt(Count);
                Gp.RemoveAt(Count);
            }
        }
    }
}