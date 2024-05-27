using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PeopleManagement.Domain.ErrorTools
{
    public class DomainException : Exception
    {
        private string _origin = string.Empty;
        public string Origin
        {
            get => _origin;
            set => _origin = value;
        }
        public Dictionary<string, List<object>> Errors { get; private set; } = [];

        public DomainException(string origin)
        {
            Origin = origin;
        }
        public DomainException(object origin)
        {
            Origin = origin.GetType().Name;
        }
        public DomainException(string origin, Result result)
        {
            Origin = origin;
            AddResult(result);
        }
        public DomainException(object origin, Result result)
        {
            Origin = origin.GetType().Name;
            AddResult(result);
        }
        public DomainException(string origin, List<Result> results)
        {
            Origin = origin;
            AddResults(results);
        }
        public DomainException(object origin, List<Result> results)
        {
            Origin = origin.GetType().Name;
            AddResults(results);
        }        

        public DomainException(string origin, List<Error> errors)
        {
            Origin = origin;
            AddErrorsRange(errors);
        }

        public DomainException(object origin, List<Error> errors)
        {
            Origin = origin.GetType().Name;
            AddErrorsRange(errors);
        }

        public DomainException(string origin, Error error)
        {
            Origin = origin;
            AddError(error);
        }

        public DomainException(object origin, Error error)
        {
            Origin = origin.GetType().Name;
            AddError(error);
        }

        public bool HasError => Errors.Count > 0;

        public void AddError(Error error)
        {
            AddOrUpdateErrorsValue(Origin, [error]);
        }
        public void AddErrorsRange(List<Error> errors)
        {
            var list = errors.ConvertAll(x => (object)x);
            AddOrUpdateErrorsValue(Origin, list);
        }

        public void AddErrorsDictionary(Dictionary<string, List<object>> errorDic)
        {
            if (errorDic.Count == 1 && errorDic.Any(x => x.Key == Origin))
            {
                AddOrUpdateErrorsValue(Origin, errorDic[Origin]);
                return;
            }
            AddOrUpdateErrorsValue(Origin, [errorDic]);
        }

        public void AddResult(Result result)
        {
            if (result.IsFailure)
                AddErrorsDictionary(result.Errors);
        }

        public void AddResults(List<Result> result)
        {
            var errorDics = result.Where(x => x.IsFailure).Select(x => x.Errors).ToList() ?? [];
            foreach (var errorDic in errorDics)
            {
                AddErrorsDictionary(errorDic);
            }
        }

        private void AddOrUpdateErrorsValue(string key, List<object> value)
        {
            if (!Errors.TryAdd(key, value))
                Errors[key].AddRange(value);
        }

    }
}
