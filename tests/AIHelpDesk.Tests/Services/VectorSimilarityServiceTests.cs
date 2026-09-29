using AIHelpDesk.Application.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Tests.Services
{
    public class VectorSimilarityServiceTests
    {
        [Fact]
        public void CalculateCosineSimilarity_ShouldReturnOne_ForIdenticalVectors()
        {
            var service = new VectorSimilarityService();

            var result = service.CalculateCosineSimilarity(new float[] { 1, 2, 3 }, new float[] { 1, 2, 3 });

            Assert.Equal(1, result, 5);
        }

        [Fact]
        public void CalculateCosineSimilarity_ShouldReturnZero_ForPerpendicularVectors()
        {
            var service = new VectorSimilarityService();

            var result = service.CalculateCosineSimilarity(new float[] { 1, 0 }, new float[] { 0, 1 });

            Assert.Equal(0, result, 5);
        }

        [Fact]
        public void CalculateCosineSimilarity_ShouldReturnZero_WhenFirstVectorIsZero()
        {
            var service = new VectorSimilarityService();

            var result = service.CalculateCosineSimilarity(new float[] { 0, 0 }, new float[] { 1, 2 });

            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculateCosineSimilarity_ShouldThrow_WhenDimensionsDoNotMatch()
        {
            var service = new VectorSimilarityService();

            Assert.Throws<ArgumentException>(() => service.CalculateCosineSimilarity(new float[] { 1, 2 }, new float[] { 1, 2, 3 }));
        }
    }
}
