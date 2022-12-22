using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.Domain.SeedWork
{
    public abstract class Entity
    {
        private Guid _Id;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public virtual Guid Id
        {
            get
            {
                return _Id;
            }
            set
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
