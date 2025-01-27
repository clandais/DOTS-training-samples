using AutoFarmers.Systems;
using VContainer;
using VContainer.Unity;
using VitalRouter.VContainer;

namespace AutoFarmers.LifetimeScopes
{
	public class GameLifetimeScope : LifetimeScope
	{
		protected override void Configure(IContainerBuilder builder)
		{
			builder.Register<TestService>(Lifetime.Singleton);
			
			builder.UseDefaultWorld(systems =>
			{
				systems.Add<TestSystemBase>();
			});
			
			builder.RegisterVitalRouter(routing =>
			{
				routing.Map<TestPresenter>();
			});
		}
	}
}