﻿using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class CategoryDriversLicense : Enumeration
    {
        public static readonly CategoryDriversLicense A = new(1, nameof(A));
        public static readonly CategoryDriversLicense B = new(2, nameof(B));
        public static readonly CategoryDriversLicense C = new(3, nameof(C));
        public static readonly CategoryDriversLicense D = new(4, nameof(D));
        public static readonly CategoryDriversLicense E = new(5, nameof(E));

        private CategoryDriversLicense(int id, string name) : base(id, name)
        {

        }
        public static implicit operator CategoryDriversLicense(int id) => Enumeration.FromValue<CategoryDriversLicense>(id);
        public static implicit operator CategoryDriversLicense(string name) => Enumeration.FromDisplayName<CategoryDriversLicense>(name);
    }
}
