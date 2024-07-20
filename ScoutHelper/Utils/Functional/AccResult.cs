using System.Collections.Generic;
using System.Collections.Immutable;

namespace ScoutHelper.Utils.Functional;

public class AccResults<T, E> : IAccResults<T, E> {
	private readonly T _value;
	private readonly IList<E> _errors;

	public T Value => _value;

	public IEnumerable<E> Errors => _errors;

	internal AccResults(T value, IEnumerable<E> errors) {
		_value = value;
		_errors = errors.ToImmutableList();
	}

	public static implicit operator AccResults<T, E>(T value) => AccResults.From<T, E>(value);
	public static implicit operator AccResults<T, E>(E error) => AccResults.From<T, E>(default, error.AsSingletonList());
}

public static class AccResults {
	public static AccResults<T, E> From<T, E>(T value) => new(value, new List<E>());
	public static AccResults<T, E> From<T, E>(T value, IEnumerable<E> errors) => new(value, errors);
}
