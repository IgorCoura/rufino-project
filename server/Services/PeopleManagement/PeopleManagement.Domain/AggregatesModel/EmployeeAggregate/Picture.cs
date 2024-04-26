using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class Picture : ValueObject
    {
        private Image[] _images = [];
        public Image[] Images 
        {
            get => _images;
            private set
            {
                _images = value;
                _images = _images.Distinct().ToArray();
            }
        }
        private Picture(Image[] images)
        {
            Images = images;
        }

        public static Picture Create() => new([]);
        public Picture AddImage(Image image)
        {
            Image[] images = [.. Images, image];
            return new(images);
        }

        public bool HasValidImage => Images.Any(x => x.Valid);
        public Result CheckPendingIssues()
        {
            var error = new List<Error>();

            if (!HasValidImage)
                error.Add(DomainErrors.FieldIsRequired(nameof(Image)));

            return Result.Failure(this.GetType().Name, error);
        }
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            foreach(var image in Images)
            {
                yield return image;
            }
        }
    }
}
