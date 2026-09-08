using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using KewlKommon.Components;

namespace KewlTewns.Components
{
	public class CommandQueue : PerGuildMonitor<CommandQueue>
	{
		ConcurrentQueue<CommandHandle> validationQueue = new ConcurrentQueue<CommandHandle>();
		ConcurrentQueue<CommandHandle> executionQueue = new ConcurrentQueue<CommandHandle>();

		public int cancelledCommandIndex { get; private set; }
		public int previousCommandIndex { get; private set; }
		public bool allCommandsCancelled { get { return previousCommandIndex <= cancelledCommandIndex; } }

		public int GetNextActionIndex() { return ++previousCommandIndex; }
		public void CancelCurrentActions() { cancelledCommandIndex = previousCommandIndex; }

		public void Enqueue(CommandHandle handle)
		{
			handle.state = CommandState.WaitingToValidate;
			validationQueue.Enqueue(handle);
		}

		public override void Monitor()
		{
			MonitorValidation();
			MonitorExecution();
		}
		private async void MonitorValidation()
		{
			while (true)
			{
				await Task.Delay(100);
				
				while (validationQueue.TryDequeue(out var handle))
				{
					if (handle.cancelled)
					{
						handle.state = CommandState.Done;
						continue;
					}

					if (handle.state != CommandState.WaitingToValidate)
					{
						handle.state = CommandState.Done;
						continue;
					}

					handle.state = CommandState.Validating;
					while (handle.state == CommandState.Validating && !handle.cancelled)
						await Task.Delay(100);

					if (handle.cancelled)
					{
						handle.state = CommandState.Done;
						continue;
					}

					if (handle.state == CommandState.WaitingToExecute)
						executionQueue.Enqueue(handle);
				}
			}
		}
		private async void MonitorExecution()
		{
			while (true)
			{
				await Task.Delay(100);

				while (executionQueue.TryDequeue(out var handle))
				{
					try
					{
						if (handle.cancelled)
							continue;

						if (handle.state != CommandState.WaitingToExecute)
							continue;

						handle.state = CommandState.Executing;
						while (handle.state == CommandState.Executing && !handle.cancelled)
							await Task.Delay(100);
					}
					finally
					{
						handle.state = CommandState.Done;
					}
				}
			}
		}
	}

	public class CommandHandle : IDisposable
	{
		public CommandState state = CommandState.New;
		public int index { get; private set; }
		public CommandQueue? queue { get; private set; }

		public bool cancelled { get { return queue == null || queue.cancelledCommandIndex >= index; } }

		public CommandHandle(CommandQueue? queue)
		{
			this.queue = queue;
			index = queue?.GetNextActionIndex() ?? -1;
		}

		public async Task<bool> WaitToValidate()
		{
			if (queue == null)
				return false;
			if (state != CommandState.New)
				return false;

			queue.Enqueue(this);
			while (state == CommandState.WaitingToValidate)
				await Task.Delay(100);

			return state == CommandState.Validating;
		}
		public async Task<bool> WaitToExecute()
		{
			if (state != CommandState.Validating)
				return false;

			state = CommandState.WaitingToExecute;
			while (state == CommandState.WaitingToExecute)
				await Task.Delay(100);

			return state == CommandState.Executing;
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			state = CommandState.Done;
		}
	}

	public enum CommandState
	{
		New,
		WaitingToValidate,
		Validating,
		WaitingToExecute,
		Executing,
		Done,
	}
}