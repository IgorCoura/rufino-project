namespace PeopleManagement.Domain.ErrorTools;

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(string originName, TValue value)
        : base(originName) =>
        _value = value;

    protected internal Result(string originName, Error error)
        : base(originName, error) { }

    protected internal Result(string originName, List<Error> errors)
        : base(originName, errors) { }

    protected internal Result(string originName, Dictionary<string, List<object>> errors)
        : base(originName, errors) { }

    protected internal Result(DomainException exception)
        : base(exception) { }


    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}