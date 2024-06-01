namespace DellarteDellaGuerra.Infrastructure.Configuration.Providers
{
    /**
     * <summary>
     *  The interface for a configuration provider.
     * </summary>
     * <typeparam name="T">
     *  The type of the configuration.
     * </typeparam>
     */
    public interface IConfigurationProvider<out T>
    {

        /**
         * <summary>
         *  Gets the configuration.
         * </summary>
         * <returns>
         * The configuration or null if the configuration is not available.
         * </returns>
         */
        public T? Config
        {
            get;
        }
    }
}

