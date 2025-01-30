using AutoFarmers.Authoring;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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
		
		public NativeArray<(bool, RectInt)> GetWalkableArray()
		{
			var walkable = new NativeArray<(bool, RectInt)>(_rockedTiles.Length, Allocator.TempJob);
			for (int i = 0; i < _rockedTiles.Length; i++)
			{
				bool walkableValue = !_rockedTiles[i].IsOccupied;
				var rect = _rockedTiles[i].Rock.Rect;
				
				walkable[i] = (walkableValue, rect);
			}

			return walkable;
		}
	}
}