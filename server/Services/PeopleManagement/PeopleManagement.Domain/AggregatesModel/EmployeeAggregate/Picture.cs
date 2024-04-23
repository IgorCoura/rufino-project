
using PeopleManagement.Domain.Exceptions;
using static System.Net.Mime.MediaTypeNames;

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

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            foreach(var image in Images)
            {
                yield return image;
            }
        }
    }
}
