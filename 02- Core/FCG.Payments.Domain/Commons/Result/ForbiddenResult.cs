namespace FCG.Payments.Domain.Commons.Result;

public class ForbiddenResult<T> : Result<T>
{
    private ForbiddenResult(IEnumerable<Error> errors) : base(default, success: false)
    {
        Errors = errors;
    }

    public IEnumerable<Error> Errors { get; }

    public static ForbiddenResult<T> Create(IEnumerable<Error> errors) => new(errors);

    public static ForbiddenResult<T> Create(Error error) => new([error]);
}
