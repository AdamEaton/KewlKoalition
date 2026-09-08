using System.Collections.Concurrent;

namespace KewlKommon.Utilities
{
	public class EventInstance { }

	public static class EventUtil
	{
		public delegate void EventDelegate<T>(T e) where T : EventInstance;

		interface IInvokable
		{
			void Invoke(object e);
		}
		class Invokable<T> : IInvokable where T : EventInstance
		{
			public event EventDelegate<T>? invokeEvent;

			public void Invoke(object e)
			{
				if (e is not T t)
					return;

				invokeEvent?.Invoke(t);
			}
		}

		static ConcurrentDictionary<System.Type, IInvokable> listeners = new ConcurrentDictionary<System.Type, IInvokable>();

		public static void AddListener<T>(EventDelegate<T> listener) where T : EventInstance
		{
			var t = typeof(T);

			var invokable = listeners.GetOrAdd(t, new Invokable<T>()) as Invokable<T>;

			if (invokable is not Invokable<T> typed)
				return;
			typed.invokeEvent += listener;
		}
		public static void RemoveListener<T>(EventDelegate<T> listener) where T : EventInstance
		{
			var t = typeof(T);

			if (!listeners.TryGetValue(t, out var invokable))
				return;

			if (invokable is not Invokable<T> typed)
				return;
			typed.invokeEvent -= listener;
		}

		public static void Dispatch<T>(T e) where T : EventInstance
		{
			var t = typeof(T);

			while (t != null)
			{
				if (listeners.TryGetValue(t, out var invokable))
					invokable.Invoke(e);

				if (t == typeof(EventInstance))
					break;

				t = t.BaseType;
			}
		}
	}
}