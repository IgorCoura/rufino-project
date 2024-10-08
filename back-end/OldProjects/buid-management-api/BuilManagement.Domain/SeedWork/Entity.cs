﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.SeedWork
{
    public abstract class Entity
    {
        private Guid _Id;
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        public virtual Guid Id
        {
            get
            {
                return _Id;
            }
            protected set
            {
                _Id = value;
            }
        }

        protected Entity(Guid id)
        {
            Id = id;
        }
        protected Entity() { }
    }
}
