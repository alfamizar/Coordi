namespace JustCompute.Shared.Abstractions.Navigation
{
    public interface IQueryParameter
    {
        /// <summary>The key the parameter travels under in Shell's navigation parameters.</summary>
        const string Key = "parameter";

        void ApplyQueryParameter(object? parameter);
    }
}
