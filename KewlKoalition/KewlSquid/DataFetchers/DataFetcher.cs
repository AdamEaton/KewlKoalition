using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KewlKommon.Interaction;
using KewlSquid.Utilities;

namespace KewlSquid.DataFetchers
{
	public abstract class DataFetcher
	{
		protected abstract string EmbedTitle { get; }
		protected abstract string? EmbedFooter { get; }

		protected abstract string BaseRotationTitle { get; }
		protected abstract string CachedImageName { get; }
		protected string CachedImagePath { get { return ResourcesUtil.GetResourcePath(CachedImageName); } }

		public abstract Task Execute(IResponseHandler responseHandler);

		public abstract void CacheRotationImage();
	}
	public abstract class DataFetcher<TRotation> : DataFetcher
		where TRotation : RotationData
	{
		protected string GetRotationTitle(TRotation rotation, bool isFirst)
		{
			if (isFirst)
			{
				if (rotation.startTime <= DateTimeOffset.UtcNow)
				{
					return "Current " + BaseRotationTitle;
				}
				else
				{
					return "Upcoming " + BaseRotationTitle;
				}
			}
			else
			{
				return "Following " + BaseRotationTitle;
			}
		}

		protected abstract IEnumerable<TRotation> GetRotations();
		protected TRotation? GetMainRotation() { return GetRotations().FirstOrDefault(); }
	}
	public abstract class RotationData
	{
		public DateTimeOffset startTime = DateTimeOffset.MinValue;
		public DateTimeOffset endTime = DateTimeOffset.MinValue;
	}
}