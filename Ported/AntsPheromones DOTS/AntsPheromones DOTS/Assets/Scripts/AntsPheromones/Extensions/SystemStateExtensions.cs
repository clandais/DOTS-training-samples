using Unity.Entities;

namespace AntsPheromones.Extensions
{
	public static class SystemStateExtensions
	{
		public static void RequireAllForUpdate<T1, T2>(this ref SystemState state) 
			where T1 : struct, IComponentData
			where T2 : struct, IComponentData
		{
			state.RequireForUpdate<T1>();
			state.RequireForUpdate<T2>();
		}
		
		public static void RequireAllForUpdate<T1, T2, T3>(this ref SystemState state) 
			where T1 : struct, IComponentData
			where T2 : struct, IComponentData
			where T3 : struct, IComponentData
		{
			state.RequireForUpdate<T1>();
			state.RequireForUpdate<T2>();
			state.RequireForUpdate<T3>();
		}
		
		
		public static void RequireAllForUpdate<T1, T2, T3, T4>(this ref SystemState state) 
			where T1 : struct, IComponentData
			where T2 : struct, IComponentData
			where T3 : struct, IComponentData
			where T4 : struct, IComponentData
		{
			state.RequireForUpdate<T1>();
			state.RequireForUpdate<T2>();
			state.RequireForUpdate<T3>();
			state.RequireForUpdate<T4>();
		} 
		
		
		public static void RequireAllForUpdate<T1, T2, T3, T4, T5>(this ref SystemState state) 
			where T1 : struct, IComponentData
			where T2 : struct, IComponentData
			where T3 : struct, IComponentData
			where T4 : struct, IComponentData
			where T5 : struct, IComponentData
		{
			state.RequireForUpdate<T1>();
			state.RequireForUpdate<T2>();
			state.RequireForUpdate<T3>();
			state.RequireForUpdate<T4>();
			state.RequireForUpdate<T5>();
		} 
	}
}