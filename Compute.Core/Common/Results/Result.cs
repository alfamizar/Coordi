namespace Compute.Core.Common.Results
{
    /// <summary>
    /// Either a value or the reason there isn't one.
    ///
    /// Replaces DotNext's <c>Result&lt;T, TError&gt;</c>. The library was carried for this one
    /// type, in the domain layer's dependency list, where a third-party package earns its place
    /// only by doing something we would otherwise have to think hard about. This does not.
    /// </summary>
    public readonly struct Result<TValue, TError> where TError : struct
    {
        private readonly TValue? _value;
        private readonly TError? _error;

        public Result(TValue value)
        {
            _value = value;
            _error = null;
        }

        public Result(TError error)
        {
            _value = default;
            _error = error;
        }

        /// <summary>True when this carries a value rather than an error.</summary>
        public bool IsSuccessful => _error is null;

        /// <summary>
        /// The value. Reading it on a failed result is a bug in the caller, not a state to
        /// handle, so it throws rather than handing back a default that would travel unnoticed.
        /// </summary>
        public TValue Value => IsSuccessful
            ? _value!
            : throw new InvalidOperationException(
                $"Result carries {typeof(TError).Name} '{_error}', not a value.");

        /// <summary>The failure reason, or null when this succeeded.</summary>
        public TError? Error => _error;

        /// <summary>Lets a method simply <c>return value;</c> on the happy path.</summary>
        public static implicit operator Result<TValue, TError>(TValue value) => new(value);

        /// <summary>And <c>return FaultCode.Something;</c> on the unhappy one.</summary>
        public static implicit operator Result<TValue, TError>(TError error) => new(error);

        public override string ToString() =>
            IsSuccessful ? $"Ok({_value})" : $"Error({_error})";
    }
}
