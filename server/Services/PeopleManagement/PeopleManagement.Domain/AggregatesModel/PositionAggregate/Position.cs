﻿namespace PeopleManagement.Domain.AggregatesModel.PositionAggregate
{
    public class Position : Entity
    {
        public Name Name { get; private set; } = null!;
        public Description Description { get; private set; } = null!;
        public CBO CBO { get; private set; } = null!;
        public Guid DepartmentId { get; private set; }

        public Position() { }

        public Position(Guid id, Name name, Description description, CBO cBO, Guid departmentId) : base(id)
        {
            Name = name;
            Description = description;
            CBO = cBO;
            DepartmentId = departmentId;
        }

        public static Position Create(Guid id, Name name, Description description, CBO cBO, Guid departmentId) => new(id, name, description, cBO, departmentId);

    }
}