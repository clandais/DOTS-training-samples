using AutoFarmers.LifetimeScopes;
using Unity.Entities;
using UnityEngine;
using VContainer;

namespace AutoFarmers.Systems
{
	public partial class TestSystemBase : SystemBase
	{
		private TestService _testService;
		
		[Inject]
		public void Construct(TestService testService)
		{
			_testService = testService;
		}
		
		
		protected override void OnUpdate()
		{
			_testService.SayHello();
		}

		public void PrintMessage(string cmdMessage)
		{
			// Debug.Log( $"Message: {cmdMessage}");
		}
	}
}