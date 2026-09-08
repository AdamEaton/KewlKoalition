namespace KewlKommon.Components
{
	public interface IRegisteredListener
	{
		static abstract void AddListeners();
		static abstract void RemoveListeners();

		public static void AddRegisteredListener<T>()
			where T : IRegisteredListener
		{
			T.AddListeners();
		}
		public static void RemoveRegisteredListener<T>()
			where T : IRegisteredListener
		{
			T.RemoveListeners();
		}
	}
}
