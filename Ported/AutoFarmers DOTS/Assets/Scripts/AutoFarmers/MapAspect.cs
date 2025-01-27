using AutoFarmers.Authoring;
using Unity.Collections;
using Unity.Entities;

namespace AutoFarmers
{
	public readonly partial struct MapAspect : IAspect
	{

		private readonly RefRW<Farm> _farm;
		private readonly RefRW<FarmCfg> _farmCfg;
		private readonly DynamicBuffer<RockedTile> _rockedTiles;
		public int MapSize => _farm.ValueRO.MapSize;


	//	public NativeArray<RockedTile> RockedTiles => _rockedTiles.AsNativeArray();

		public bool IsWalkable(int idx)
		{
			return !_rockedTiles[idx].IsOccupied;
		}
	}
}