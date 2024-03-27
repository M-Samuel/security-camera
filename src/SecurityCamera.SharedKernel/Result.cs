namespace SecurityCamera.SharedKernel;

public class Result<TValue>
{
    public Result(TValue? value)
    {
        Value = value;
    }

    public TValue? Value { get; private set; }

    private readonly List<IError> _domainErrors = new();

    public bool HasError => _domainErrors.Count > 0;

    public List<IError> DomainErrors => _domainErrors.ToList();

    public Result<TValue> AddError(IError error)
    {
        _domainErrors.Add(error);
        return this;
    }

    public Result<TValue> AddErrorIf(Func<bool> predicate, IError errorToBeAddIfTrue)
    {
        if (predicate()) AddError(errorToBeAddIfTrue);
        return this;
    }

    public void UpdateValueIfNoError(TValue value)
    {
        if (!HasError) Value = value;
    }

}