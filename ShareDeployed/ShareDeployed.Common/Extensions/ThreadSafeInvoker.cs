﻿using System;
using System.Threading;

namespace ShareDeployed.Common.Extensions
{
	internal struct ThreadSafeInvoker
	{
		private int _invoked;

		public bool Invoke(Action action)
		{
			if (Interlocked.Exchange(ref _invoked, 1) == 0)
			{
				action();
				return true;
			}
			return false;
		}

		public bool Invoke<T>(Action<T> action, T arg)
		{
			if (Interlocked.Exchange(ref _invoked, 1) == 0)
			{
				action(arg);
				return true;
			}
			return false;
		}

		public bool Invoke<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
		{
			if (Interlocked.Exchange(ref _invoked, 1) == 0)
			{
				action(arg1, arg2);
				return true;
			}
			return false;
		}
	}
}