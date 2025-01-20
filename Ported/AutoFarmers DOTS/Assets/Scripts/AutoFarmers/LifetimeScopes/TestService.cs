using UnityEngine;
using VContainer;
using VitalRouter;

namespace AutoFarmers.LifetimeScopes
{
	public class TestService
	{
		[Inject] private ICommandPublisher _commandPublisher;
		
		public void SayHello()
		{
			// Debug.Log("Hello from TestService!");
			_commandPublisher.PublishAsync(new TestCommand { Message = "Hello from TestService!" });
		}
	}
}