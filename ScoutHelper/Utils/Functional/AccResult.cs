using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ScoutHelper.Utils.Functional;

public class AccResults<T, E> : IAccResults<T, E> {

	private readonly T _value;
	private readonly IList<E> _errors;

	public T Value => _value;

	public IEnumerable<E> Errors => _errors;

	public AccResults(T value, IEnumerable<E> errors) {
		_value = value;
		_errors = errors.ToImmutableList();
	}
}
