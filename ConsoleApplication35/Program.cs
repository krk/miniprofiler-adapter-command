using System;
using System.Data.Common;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.Storage;

namespace ConsoleApplication35
{
	class Program
	{
		static void Main(string[] args)
		{
			// Setup for console application example.
			MiniProfiler.Settings.Storage = new HttpRuntimeCacheStorage(TimeSpan.FromHours(1));
			MiniProfiler.Settings.ProfilerProvider = new SingletonProfilerProvider();

			var defaultFactory = DbProviderFactories.GetFactory("System.Data.SqlClient");

			var factory = new ProfiledDbProviderFactory(defaultFactory);

			try
			{
				DoStuff(factory);
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
			}

			var workedaroundFactory = new ProfiledDbProviderFactoryAlwaysProfiled(defaultFactory);

			DoStuff(workedaroundFactory);
		}

		private static void DoStuff(DbProviderFactory factory)
		{
			// This adapter is created in another layer which knows nothing about MiniProfiler.
			var adapter = factory.CreateDataAdapter();

			MiniProfiler.Start();

			using (MiniProfiler.StepStatic("Phase 2"))
			{
				// Somehow a profiled command ends up in adapter.SelectCommand
				var command = factory.CreateCommand();

				/* Even though type of SelectCommand is DbCommand, an SqlDataAdap
				 * An unhandled exception of type 'System.InvalidCastException' occurred in System.Data.dll
				 * Additional information: Unable to cast object of type 'StackExchange.Profiling.Data.ProfiledDbCommand' to type 'System.Data.SqlClient.SqlCommand'.
				 */
				adapter.SelectCommand = command;
			}

			MiniProfiler.Stop();
		}
	}

	public class ProfiledDbProviderFactoryAlwaysProfiled : ProfiledDbProviderFactory
	{
		private readonly DbProviderFactory _tail;

		public ProfiledDbProviderFactoryAlwaysProfiled(DbProviderFactory tail)
			: base(tail)
		{
			_tail = tail;
		}

		public override DbCommand CreateCommand()
		{
			DbCommand command = this._tail.CreateCommand();

			return new ProfiledDbCommand(command, null, MiniProfiler.Current);
		}

		public override DbConnection CreateConnection()
		{
			DbConnection connection = this._tail.CreateConnection();

			return new ProfiledDbConnection(connection, MiniProfiler.Current);
		}

		public override DbDataAdapter CreateDataAdapter()
		{
			DbDataAdapter dataAdapter = this._tail.CreateDataAdapter();

			return new ProfiledDbDataAdapter(dataAdapter, MiniProfiler.Current);
		}
	}
}
