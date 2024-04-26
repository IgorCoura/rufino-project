using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PeopleManagement.Domain.ErrorTools;
public class Result
{
    private string _origin = string.Empty;
    public string Origin 
    {
        get => string.IsNullOrWhiteSpace(_origin) ? throw new ArgumentException("It is necessary to declare an origin.") : _origin;
        set => _origin = value; 
    }
    protected internal Result(string origin)
    {
        Origin = origin; 
    }

    protected internal Result(string origin, Error errors)
    {
        Origin = origin;
        AddError(errors);
    }


    protected internal Result(string origin, List<Error> errors)
    {
        Origin = origin;
        AddErrorsRange(errors);
    }

    protected internal Result(string origin, Dictionary<string, List<object>> errorDic)
    {
        Origin = origin;
        AddErrorsDictionary(errorDic);
    }

    protected internal Result(DomainException domainException)
    {
        Errors = domainException.Errors;
    }

    public bool IsSuccess => !IsFailure;

    public bool IsFailure => HasError;

    public bool HasError => Errors.Count > 0;
    public Dictionary<string, List<object>> Errors { get; private set; } = [];

    public void AddError(Error error)
    {
        if(Errors.TryGetValue(Origin, out List<object>? value))
        {
            value.Add(error);
            return;
        }            
        Errors.Add(Origin, [error]);
    }
    public void AddErrorsRange(List<Error> errors)
    {
        if (!Errors.ContainsKey(Origin))
            Errors.Add(Origin, []);
        Errors[Origin].AddRange(errors);
    }

    public void AddErrorsDictionary(Dictionary<string, List<object>> errorDic)
    {
        if (Errors.TryGetValue(Origin, out List<object>? value))
        {
            value.Add(errorDic);
            return;
        }            
        Errors.Add(Origin, [errorDic]);
    }

    public void AddErrorDictionaries(List<Dictionary<string, List<object>>> errorDics)
    {
        if (!Errors.ContainsKey(Origin))
            Errors.Add(Origin, []);
        Errors[Origin].AddRange(errorDics);
    }

    public void AddResult(Result result)
    {
        if(result.IsFailure)
            AddErrorsDictionary(result.Errors);
    }

    public void AddResults(List<Result> result)
    {
        var errorDics = result.Where(x => x.IsFailure).Select(x => x.Errors).ToList() ?? [];
        AddErrorDictionaries(errorDics);  
    }

    public static Result Success() => new("");
    public static Result Success(string origin) => new(origin);

    public static Result<TValue> Success<TValue>(TValue value) => new("", value);

    public static Result Failure(string origin, List<Error> errors) => new(origin, errors);
    public static Result Failure(string origin, Error error) => new(origin, error);
    public static Result<TValue> Failure<TValue>(string origin, List<Error> errors) => new(origin, errors);
    public static Result<TValue> Failure<TValue>(string origin, Dictionary<string, List<object>> errorDic) => new(origin, errorDic);
    public static Result<TValue> Failure<TValue>(string origin, Error error) => new(origin, error);
    public static Result<TValue> Failure<TValue>(DomainException exception) => new(exception);

}
