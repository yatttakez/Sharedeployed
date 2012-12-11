
//[assembly: WebActivator.PreApplicationStartMethod(typeof(ShareDeployed.App_Start.NinjectWebCommon), "Start")]
//[assembly: WebActivator.ApplicationShutdownMethodAttribute(typeof(ShareDeployed.App_Start.NinjectWebCommon), "Stop")]

namespace ShareDeployed.App_Start
{
	using System;
	using System.Web;

	using Microsoft.Web.Infrastructure.DynamicModuleHelper;

	using Ninject;
	using Ninject.Web.Common;
	using ShareDeployed.Common;
	using ShareDeployed.Common.Models;

	public static class NinjectWebCommon
	{
		private static readonly Bootstrapper bootstrapper = new Bootstrapper();

		/// <summary>
		/// Starts the application
		/// </summary>
		public static void Start()
		{
			DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
			DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
			bootstrapper.Initialize(CreateKernel);
		}

		/// <summary>
		/// Stops the application.
		/// </summary>
		public static void Stop()
		{
			bootstrapper.ShutDown();
		}

		/// <summary>
		/// Creates the kernel that will manage your application.
		/// </summary>
		/// <returns>The created kernel.</returns>
		private static IKernel CreateKernel()
		{
			var kernel = new StandardKernel();
			kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
			kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

			RegisterServices(kernel);

			System.Web.Http.GlobalConfiguration.Configuration.DependencyResolver = new NinjectResolver(kernel);

			return kernel;
		}

		/// <summary>
		/// Load your modules or register your services here!
		/// </summary>
		/// <param name="kernel">The kernel.</param>
		private static void RegisterServices(IKernel kernel)
		{
			//kernel.Bind<IRepositoryEx<Word>>().To<Repositories.WordsRepository>();
		}

		public static T GetInstanceOf<T>()
		{
			return (T)bootstrapper.Kernel.GetService(typeof(T));
		}
	}
}
