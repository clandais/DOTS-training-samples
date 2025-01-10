using Unity.Entities;
using Unity.Mathematics;

namespace AntsPheromones.Components
{
    public struct Pheromone : IBufferElementData
    {
        public float3 Value;
    }
}
