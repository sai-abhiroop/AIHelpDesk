using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Interfaces
{
    public interface IVectorSimilarityService
    {
        double CalculateCosineSimilarity(float[] first, float[] second);
    }
}
