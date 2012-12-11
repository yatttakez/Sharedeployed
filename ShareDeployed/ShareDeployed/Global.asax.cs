﻿using log4net;
using ShareDeployed.Authorization;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using ShareDeployed.App_Start;

namespace ShareDeployed
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, // visit http://go.microsoft.com/?LinkId=9394801
	public class MvcApplication : System.Web.HttpApplication
	{
		private const string _WebApiPrefix = "api";
		private const string _WebApiController = "AuthToken";
		private static readonly string _WebApiExecutionPath = String.Format("~/{0}", _WebApiPrefix);

		public static ILog Logger = null;

		protected void Application_PostAuthorizeRequest()
		{
			if (IsWebApiRequest())
				HttpContext.Current.SetSessionStateBehavior(System.Web.SessionState.SessionStateBehavior.Required);
		}

		private bool IsWebApiRequest()
		{
			return (HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath.StartsWith(_WebApiExecutionPath)
				&& HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath.Contains(_WebApiController));
		}

		protected void Application_Start()
		{
			Logger = LogManager.GetLogger(typeof(MvcApplication).FullName);
			log4net.Config.XmlConfigurator.Configure();
			Logger.Info("Application_Start()");

			Infrastructure.Bootstrapper.PreAppStart();

			if (System.Web.WebPages.DisplayModeProvider.Instance.Modes != null)
				System.Web.WebPages.DisplayModeProvider.Instance.Modes.Insert(2, new System.Web.WebPages.DefaultDisplayMode("iPhone")
				{
					ContextCondition = (context => (context.Request.UserAgent != null &&
													context.Request.UserAgent.IndexOf("iPhone", StringComparison.OrdinalIgnoreCase) >= 0))
				});

			AreaRegistration.RegisterAllAreas();
			AuthConfig.RegisterAuth();
			WebApiConfig.Register(GlobalConfiguration.Configuration);
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);
			//This line has been added to support specific ASP.MVC 4 mobile
			BundleMobileConfig.RegisterBundles(BundleTable.Bundles);
			
			//Setup MVC services
			ControllerBuilder.Current.SetControllerFactory(typeof(ControllerFactory.DefaultMVCFactory));
			System.Web.Mvc.DependencyResolver.SetResolver(new DependencyResolvers.MvcDependencyResolver(Infrastructure.Bootstrapper.Kernel));

			// Cache never expires.You must restart application pool when you add/delete a view.A non-expiring cache can lead to heavy server memory load. 
			ViewEngines.Engines.OfType<RazorViewEngine>().First().ViewLocationCache =
			new DefaultViewLocationCache(System.Web.Caching.Cache.NoSlidingExpiration);

			var webFormsVE = ViewEngines.Engines.OfType<WebFormViewEngine>().FirstOrDefault();
			if (webFormsVE != null)
				ViewEngines.Engines.Remove(webFormsVE);

			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			Infrastructure.Bootstrapper.DoMigrations();
			//Infrastructure.Bootstrapper.DoSomeeMigrations();

			try
			{
				Database.SetInitializer<DataAccess.ShareDeployedContext>(new DataAccess.Initializer.ShareDbInitializer());
				Database.SetInitializer(new DropCreateDatabaseIfModelChanges<DataAccess.ShareDeployedContext>());
			}
			catch (Exception ex)
			{
				Logger.Error("Error has been occurred in Database.Setinitializer section", ex);
			}
			
			if (!Database.Exists("Somee"))
				Logger.Error("Databse is not exist");

			//Make fake initialization
			if (Authorization.AuthTokenManagerEx.Instance != null)
				Logger.Info("Module AuthTokenManagerEx is initialized");
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Logger.Error("UnhandledException", e.ExceptionObject as Exception);
		}

		void Session_Start(object sender, EventArgs e)
		{
			if (HttpContext.Current == null || HttpContext.Current.Session == null)
				return;

			HttpContext.Current.Session.Add("_MyAppSession", HttpContext.Current.Session.SessionID);
			System.Diagnostics.Debug.WriteLine("Added " + HttpContext.Current.Session["_MyAppSession"] as string);
		}

		void Session_End(object sender, EventArgs e)
		{
			if (HttpContext.Current != null && HttpContext.Current.Session != null
				&& HttpContext.Current.Session["_MyAppSession"] != null)
			{
				System.Diagnostics.Debug.WriteLine("Removed " + HttpContext.Current.Session["_MyAppSession"] as string);
				//AuthTokenManager.Instance.RemoveToken(HttpContext.Current.Session["_MyAppSession"] as string);
				HttpContext.Current.Session.Remove("_MyAppSession");
			}
		}

		protected void Application_End()
		{
			Authorization.AuthTokenManagerEx.Instance.StopCleaning();
			Authorization.AuthTokenManagerEx.Instance.Dispose();
			Logger.Info("Application_End()");
			LogManager.Shutdown();
		}

		protected void Application_Error(object sender, EventArgs e)
		{
			Exception exception = Server.GetLastError();
			HttpException ex = exception as HttpException;

			if (ex != null)
				Logger.Error("Application_Error", ex);
			else
				Logger.Error("Application_Error", exception);
		}
	}
}