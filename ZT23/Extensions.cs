using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlayer
{
    public static class Extensions
    {
        internal static T Choose<T>(this Random random, IEnumerable<T> list) => list.ElementAt(random.Next(list.Count()));
        internal static int GetIndex<T>(this T enumValue) where T : Enum => Array.IndexOf(Enum.GetValues(enumValue.GetType()), enumValue);
        internal static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> list) => list.SelectMany(e => e);
        internal static IEnumerable<T> Maximise<T>(this IEnumerable<T> list, Func<T, double> getScore) => list.Maximise(e => e, getScore);

        internal static T ChooseWithWeights<T, U>(this Random random, IEnumerable<(T, int, U)> list)
        {
            var weightedAllocations = new List<T>();

            foreach ((var value, var weight, var _) in list)
            {
                for (var i = 0; i < weight; i++) weightedAllocations.Add(value);
            }

            return random.Choose(weightedAllocations);
        }

        internal static IEnumerable<ResultT> Maximise<StartT, ResultT>(this IEnumerable<StartT> list, Func<StartT, ResultT> mapElement, Func<StartT, double> getScore)
        {
            double bestScore = 0;

            return list.Aggregate(new List<ResultT>(), (maximalElements, e) =>
            {
                var score = getScore(e);

                if (score > bestScore)
                {
                    bestScore = score;
                    maximalElements.Clear();
                    maximalElements.Add(mapElement(e));
                }
                else if (score == bestScore)
                {
                    maximalElements.Add(mapElement(e));
                }

                return maximalElements;
            });
        }

        /**
         * Taken from https://ericlippert.com/2010/06/28/computing-a-cartesian-product-with-linq/
         */
        internal static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
              emptyProduct,
              (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Concat(new[] { item }));
        }
    }
}
