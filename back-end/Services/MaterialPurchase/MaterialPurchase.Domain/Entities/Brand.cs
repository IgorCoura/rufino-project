﻿using Commom.Domain.BaseEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.BaseEntities
{
    public class Brand : Entity
    {
        private string _name = string.Empty;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value.ToUpper();
            }
        }

        public string Description { get; set; } = string.Empty;
    }
}