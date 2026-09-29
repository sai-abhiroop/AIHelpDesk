using AIHelpDesk.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Services
{
    public class VectorSimilarityService : IVectorSimilarityService
    {
        public double CalculateCosineSimilarity(float[] first, float[] second)
        {
            if (first.Length != second.Length)
            {
                throw new ArgumentException("Vectors must have the same dimension.");
            }

            var dotProduct = 0.0;
            var firstMagnitude = 0.0;
            var secondMagnitude = 0.0;

            for(var i=0; i<first.Length; i++)
            {
                dotProduct += first[i] * second[i];
                firstMagnitude += first[i] * first[i];
                secondMagnitude+= second[i] * second[i];
            }

            if(firstMagnitude == 0 || secondMagnitude == 0)
            {
                return 0;
            }

            return dotProduct/(Math.Sqrt(firstMagnitude) * Math.Sqrt(secondMagnitude));
        }
    }
}
