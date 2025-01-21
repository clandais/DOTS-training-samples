using Unity.Collections;
using Unity.Entities;

namespace AntsPheromones.Extensions
{
	public static class EntityQueryHelperExtensions
	{
		public static EntityQuery CreateEntityQuery<T>(this ref SystemState state)
			where T : struct, IAspect, IAspectCreate<T>
		{
			return new EntityQueryBuilder(Allocator.Temp).WithAspect<T>()
				.Build(ref state);
		}
	}
}