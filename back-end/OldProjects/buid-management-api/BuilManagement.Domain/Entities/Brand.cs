﻿using BuildManagement.Domain.Entities.Purchase;
using BuildManagement.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Entities
{
    public class Brand: Entity
    {
        private string _name = string.Empty;
        public string Name
        {
            get
            {
                return _name;
            }
            protected set
            {
                _name = value.ToUpper();
            }
        }

        public string Description { get; set; } = string.Empty;
    }

}