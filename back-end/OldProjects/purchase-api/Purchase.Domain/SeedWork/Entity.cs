namespace Purchase.Domain.SeedWork
{
    public abstract class Entity
    {
        Guid _id;

        public virtual Guid Id
        {
            get
            {
                return _id;
            }
            protected set
            {
                _id = value;    
            }
        }

        protected Entity(Guid id)
        {
            _id = id;
        }
        protected Entity() { }
    }
}
