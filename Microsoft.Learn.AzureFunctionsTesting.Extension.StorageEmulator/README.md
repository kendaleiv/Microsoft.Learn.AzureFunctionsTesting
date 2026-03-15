# Azure Functions Integration Testing Framework - Storage Emulator (Azurite)

If your Azure Functions app uses Azure Storage (Blobs, Queues, or Tables), you can use this package to automatically start and stop [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) during your test runs.

## Prerequisites

- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) installed globally:

		npm install -g azurite

## Setup

In your `TestStartup`, call `builder.UseStorageEmulator()`. This returns the connection string (`UseDevelopmentStorage=true`) that you can pass to your function app:

	var storageConnectionString = builder.UseStorageEmulator();

	builder.ConfigureEnvironmentVariables(env =>
	{
		env.Add("AzureWebJobsStorage", storageConnectionString);
	});

### How it works

When `autoStart` is `true` (the default):

1. At test startup the plugin checks whether port **10000** (the Azurite blob storage port) is already in use.
2. If the port is **not** in use, Azurite is started automatically using the executable found on the machine.
3. If the port **is** already in use, the existing instance is reused and will **not** be stopped when the tests finish.
4. When the test run ends, any Azurite process that was started by the plugin is stopped and its temporary working directory is cleaned up.

### Disabling auto-start

If you manage the Azurite process yourself (e.g. in a CI pipeline), you can set `autoStart` to `false`:

	builder.UseStorageEmulator(autoStart: false);

In this mode the plugin simply returns the connection string without touching the emulator process.

## Configuring the executable path

The plugin searches for the Azurite executable in the following locations (in order):

1. The `AzuriteExePath` environment variable (useful for CI overrides).
2. `%APPDATA%\npm\azurite.cmd` (Windows – global npm install).
3. `C:\npm\prefix\azurite.cmd` (Windows – Azure DevOps hosted agents).
4. `/usr/local/bin/azurite` (macOS/Linux – global npm install).
5. `/opt/homebrew/bin/azurite` (macOS – Homebrew install).
6. `/usr/bin/azurite` (Linux).

To override the path in a DevOps pipeline:

	variables:
	  AzuriteExePath: 'C:\custom\path\azurite.cmd'
