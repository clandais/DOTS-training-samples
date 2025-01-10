using Unity.Entities;
using Unity.Mathematics;

namespace AntsPheromones.Components
{
    public struct ObstaclesPositions : IBufferElementData
    {
        public float2 Value;
    }
}
