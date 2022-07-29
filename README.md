# Marionetta

![Marionetta](Images/Marionetta.100.png)

Marionetta - Split dirty older architecthre based component depending with sandboxed outprocess and made manipulation easy.

[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

## NuGet

| Package  | NuGet                                                                                                                |
|:---------|:---------------------------------------------------------------------------------------------------------------------|
| Marionetta | [![NuGet Marionetta](https://img.shields.io/nuget/v/Marionetta.svg?style=flat)](https://www.nuget.org/packages/Marionetta) |

## CI

| main                                                                                                                                                                 | develop                                                                                                                                                                       |
|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [![Marionetta CI build (main)](https://github.com/kekyo/Marionetta/workflows/.NET/badge.svg?branch=main)](https://github.com/kekyo/Marionetta/actions?query=branch%3Amain) | [![Marionetta CI build (develop)](https://github.com/kekyo/Marionetta/workflows/.NET/badge.svg?branch=develop)](https://github.com/kekyo/Marionetta/actions?query=branch%3Adevelop) |

----

[![Japanese language](Images/Japanese.256.png)](https://github.com/kekyo/Marionetta/blob/main/README_ja.md)

## What is this?

Marionetta is a solution that allows a very exclusive (and possibly proprietary software) .NET library
to run in isolation on a sandbox with process isolation.

For example, a library that is fixed to `x86` instead of `Any CPU` and limited to
the `net35` operating environment can be loaded and called remotely under process isolation.
Conceptually, it is similar to .NET Remoting, but as you know,
it is obsolete in .NET 6/5 and .NET Core.
Marionetta does not yet support transparent proxies,
but allows running on .NET Core runtimes and remote method invocation
with an API that is easy enough to understand.

Marionetta uses [DupeNukem](https://github.com/kekyo/DupeNukem) as the basis for the back-end RPC transfer.

### Operating Environment

The following platforms are supported by the package.
A separate assembly is provided for each version.
This is in consideration of legacy libraries that are sensitive to the operating environment.

* NET 6, 5
* NET Core 3.1, 3.0, 2.2 to 2.0
* NET Standard 2.1, 2.0, 1.6 to 1.3
* NET Framework 4.8 to 4.0, 3.5

----

## How to start

Marionetta installs and uses [NuGet package Marionetta](https://www.nuget.org/packages/Marionetta) in both of the following projects:

|Role|Class|Overview|
|:----|:----|:----|
|Master|`Marionettist`|The application side of the control.|
|Slave|`Puppet`|The side being controlled, i.e. the side containing legacy libraries. It is an independent program and starts as a child process.|

You can intentionally use Marionetta in the same process.
In that case, use `MasterPuppet` and `Puppet` classes in pairs.

### How to configure slaves

There are many possible operating conditions for legacy libraries.
For example, instead of `AnyCPU`, which is most common in .NET,
or the platform specification such as `x86` or `x64` is required,
or the configuration of the slave library is not `STAThread`
and `Slave` as expected in WPF or Windows Forms.

Window message pumping by `STAThread` and `Application` classes is
required to drive the main thread, etc.

Therefore, it is necessary to write custom execution code
(although the amount of code is small).

The following is an example of how to configure a slave
to host a legacy library that uses WPF:

```csharp
using Marionetta;
using DupeNukem;

[STAThread]
public static void Main(string[] args)
{
    // Initialize the Application class
    var app = new Application();

    // Explicitly assign a SynchContext to enforce STAThread
    // (Captured during Puppet creation)
    var sc = new DispatcherSynchronizationContext();
    SetSynchronizationContext(sc); SynchronizationContext;

    // Generate Puppet
    var arguments = DriverFactory.ParsePuppetArguments(args);
    using var puppet = DriverFactory.CreatePuppet(arguments);

    // Register a remote callable instance
    // (similar to DupeNukem usage)
    var legacy = new LegacyController();
    puppet.RegisterObject("legacy", legacy);

    // Shutdown notification is triggered by the master
    puppet.ShutdownRequested += (s, e) =>
        // Shutdown the WPF application
        app.Shutdown();

    // Run Puppet (in background)
    puppet.Start();
 
    // Run the message pump
    app.Run();
}
```

TODO:

----

## Background

One of the major motivations is the problem that the ASP.NET infrastructure is too large.
ASP.NET as an endpoint for RPC calls (the associated ASP.NET WebAPI),
would result in a huge number of libraries to depend on and require an up-to-date environment to
coexistence with legacy libraries can become impossible.

There are several other libraries that can be used to implement sandboxing, and
Marionetta uses a technology commonly referred to as "IPC" or "RPC".
Marionetta is an extension of "IPC" and "RPC" as well,
but by designing it with as few dependencies on other libraries as possible,
we have tried to eliminate the prerequisites, constraints, and background knowledge.

DupeNukem uses JSON for its serializer (the familiar [NewtonSoft.Json](https://www.newtonsoft.com/json)),
but NewtonSoft.Json is also well thought out as a library, independent and supports a wide range of platforms.

----

## License

Apache-v2.

----

## History

TODO:
