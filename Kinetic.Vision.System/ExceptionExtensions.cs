using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;

namespace System
{
	public static class ExceptionExtensions
	{
		public static IEnumerable<TSource> FromHierarchy<TSource>(this TSource source, Func<TSource, TSource> nextItem, Func<TSource, bool> canContinue)
		{
			for (var current = source; canContinue(current); current = nextItem(current))
			{
				yield return current;
			}
		}

		public static IEnumerable<TSource> FromHierarchy<TSource>(this TSource source, Func<TSource, TSource> nextItem) where TSource : class
		{
			return FromHierarchy(source, nextItem, s => s != null);
		}

		public static string GetAllMessages(this Exception exception)
		{
			var validationException = exception as DbEntityValidationException;

			if (validationException != null)
			{
				var messages = exception.FromHierarchy(ex => ex.InnerException).Select(ex => ex.Message).ToList();
				foreach (var item in validationException.EntityValidationErrors)
				{
					foreach (var error in item.ValidationErrors)
					{
						messages.Add(string.Format("Property Name: [{0}], Error: [{1}]", error.PropertyName, error.ErrorMessage));
					}
				}

				return String.Join(Environment.NewLine, messages);
			}
			else
			{
				var messages = exception.FromHierarchy(ex => ex.InnerException).Select(ex => ex.Message);
				return String.Join(Environment.NewLine, messages);
			}

		}
	}
}
