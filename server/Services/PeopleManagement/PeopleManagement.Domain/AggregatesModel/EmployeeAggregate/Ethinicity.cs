﻿namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class Ethinicity : Enumeration
    {
        public static readonly Ethinicity White = new(1, nameof(White));
        public static readonly Ethinicity Black = new(2, nameof(Black));
        public static readonly Ethinicity Brown = new(3, nameof(Brown));
        public static readonly Ethinicity Yellow = new(4, nameof(Yellow));
        public static readonly Ethinicity Indigenous = new(5, nameof(Indigenous));

        private Ethinicity(int id, string name) : base(id, name)
        {
        }

        public static implicit operator Ethinicity(int id) => Enumeration.FromValue<Ethinicity>(id);
        public static implicit operator Ethinicity(string name) => Enumeration.FromDisplayName<Ethinicity>(name);
    }
}
