namespace DellarteDellaGuerra.Configuration.Providers
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
         * <param name="T">
         * The type of the configuration.
         * </param>
         */
        public T? Config
        {
            get;
        }
    }
}
