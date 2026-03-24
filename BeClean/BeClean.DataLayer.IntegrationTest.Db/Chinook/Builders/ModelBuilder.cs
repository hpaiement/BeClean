namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.Builders
{
    public abstract class ModelBuilder
    {
        protected int? _seed = null;
        protected bool _noIdentity = false;
        private long _identity = 1;

        public ModelBuilder NoIdentity()
        {
            _noIdentity = true;
            return this;
        }

        protected int GetNextIdentityInt()
        {
            return Convert.ToInt32(_identity++);
        }

        protected long GetNextIdentityLong()
        {
            return _identity++;
        }
    }
}
