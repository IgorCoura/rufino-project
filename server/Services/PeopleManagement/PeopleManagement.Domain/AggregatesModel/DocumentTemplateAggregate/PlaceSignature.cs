﻿namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class PlaceSignature : ValueObject
    {
        

        public TypeSignature Type { get; private set; } = null!;
        public Number Page { get; private set; } = null!;
        public Number RelativePositionBotton { get; private set; } = null!;
        public Number RelativePositionLeft { get; private set; } = null!;
        public Number RelativeSizeX { get; private set; } = null!;
        public Number RelativeSizeY { get; private set; } = null!;

        private PlaceSignature() { }
        private PlaceSignature(TypeSignature type, Number page, Number relativePositionBotton, Number relativePositionLeft, Number relativeSizeX, Number relativeSizeY)
        {
            Type = type;
            Page = page;
            RelativePositionBotton = relativePositionBotton;
            RelativePositionLeft = relativePositionLeft;
            RelativeSizeX = relativeSizeX;
            RelativeSizeY = relativeSizeY;
        }

        public static PlaceSignature Create(TypeSignature type, Number page, Number relativePositionBotton, Number relativePositionLeft, Number relativeSizeX, Number relativeSizeY) => new(type, page, relativePositionBotton, relativePositionLeft, relativeSizeX, relativeSizeY);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Type;
            yield return Page;
            yield return RelativePositionBotton;
            yield return RelativePositionLeft;
            yield return RelativeSizeX;
            yield return RelativeSizeY;
        }
    }
}
