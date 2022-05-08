using System;

namespace PipeBuilder.Lods
{
    [Serializable]
    public struct LodInfo
    {
        public readonly int circleDetail;
        public readonly int trianglesAmount;

        public LodInfo(int circleDetail, int trianglesAmount)
        {
            this.circleDetail = circleDetail;
            this.trianglesAmount = trianglesAmount;
        }
    }
}