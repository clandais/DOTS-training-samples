using AutoFarmers.Systems;
using VContainer;
using VitalRouter;

namespace AutoFarmers.LifetimeScopes
{
	
	public struct TestCommand : ICommand
	{
		public string Message;
	}
	
	
	
	[Routes]
	public partial class TestPresenter 
	{

		[Inject] private TestSystemBase _testSystemBase;
		
		
		[Route]
		void On(TestCommand cmd)
		{
			_testSystemBase.PrintMessage(cmd.Message);
		}
	}
}