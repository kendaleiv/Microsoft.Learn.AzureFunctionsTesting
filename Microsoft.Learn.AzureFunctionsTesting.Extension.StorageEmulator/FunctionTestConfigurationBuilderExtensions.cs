using Microsoft.Learn.AzureFunctionsTesting.Core;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.StorageEmulator
{
    public static class FunctionTestConfigurationBuilderExtensions
    {
        /// <summary>
        /// Configures the test run to use the Azure Storage Emulator (Azurite).
        /// When <paramref name="autoStart"/> is <see langword="true"/> (the default), the plugin checks
        /// whether port 10000 is already in use. If not, it starts Azurite automatically and stops it
        /// when the test run finishes. If the port is already in use the existing instance is reused
        /// and will not be stopped at the end of the test run.
        /// </summary>
        /// <param name="builder">The configuration builder.</param>
        /// <param name="autoStart">
        /// When <see langword="true"/> (default), Azurite is started if it is not already running.
        /// When <see langword="false"/>, the plugin assumes an external storage emulator is available
        /// and does not attempt to start or stop one.
        /// </param>
        /// <returns>The Azurite connection string (<c>UseDevelopmentStorage=true</c>).</returns>
        public static string UseStorageEmulator(this IFunctionTestConfigurationBuilder builder, bool autoStart = true)
        {
            var plugin = new StorageEmulatorPlugin(StorageEmulatorPlugin.DefaultName, autoStart);
            builder.RegisterPlugin(plugin, plugin.Name);
            return StorageEmulatorPlugin.ConnectionString;
        }
    }
}
