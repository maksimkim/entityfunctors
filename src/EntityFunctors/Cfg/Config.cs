namespace EntityFunctors.Cfg
{
    public static class Config
    {
        private static IReflectionOptimizer _reflectionOptimizer { get; set; }
        
        public static IReflectionOptimizer ReflectionOptimizer
        {
            get { return _reflectionOptimizer ?? (_reflectionOptimizer = new DefaultReflectionOptimizer()); }
        }
    }
}