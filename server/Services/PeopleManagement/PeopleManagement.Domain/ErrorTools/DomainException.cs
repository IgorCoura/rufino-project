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

        public DomainException(string origin, Result result)
        {
            Origin = origin;
            AddResult(result);
        }

        public DomainException(string origin, List<Result> results)
        {
            Origin = origin;
            AddResults(results);
        }
        public DomainException(string origin)
        {
            Origin = origin;
        }

        public DomainException(string origin, List<Error> errors)
        {
            Origin = origin;
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
            if (Errors.TryGetValue(Origin, out List<object>? value))
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
            if (result.IsFailure)
                AddErrorsDictionary(result.Errors);
        }

        public void AddResults(List<Result> result)
        {
            var errorDics = result.Where(x => x.IsFailure).Select(x => x.Errors).ToList() ?? [];
            AddErrorDictionaries(errorDics);
        }

        public void AddException(DomainException result)
        {
            if (result.HasError)
                AddErrorsDictionary(result.Errors);
        }

    }
}
