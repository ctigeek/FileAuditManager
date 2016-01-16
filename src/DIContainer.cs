using StructureMap;

namespace FileAuditManager
{
    static class DIContainer
    {
        private static IContainer _container;
        public static IContainer Container
        {
            get
            {
                if (_container == null)
                {
                    BuildContainer();
                }
                return _container;
            }
        }

        private static void BuildContainer()
        {
            _container = new Container(c =>
            {
                c.IncludeRegistry<StructureMapRegistry>();
            });
        }
    }
}
