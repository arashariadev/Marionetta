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

Another motivation is the problem of ASP.NET's infrastructure being too large.
If ASP.NET were used as the endpoint for RPC invocation (related ASP.NET WebAPI),
its set of dependent libraries would be too large and require an up-to-date environment,
making it impossible to coexist with the aforementioned libraries.
.NET as the RPC calling endpoint, there is a high possibility that
it will not be able to coexist with the aforementioned libraries.

These issues led us to design Marionetta.

There are several other such libraries, commonly called "IPC" or "RPC" libraries.
Marionetta's goal is to be as easy to use as possible
and to eliminate the prerequisites, constraints, and background knowledge often associated
with "IPC" and "RPC" libraries.

Marionetta uses [DupeNukem](https://github.com/kekyo/DupeNukem) as the basis for back-end RPC transportion.
Briefly, it uses a JSON serializer (with which you are familiar [NewtonSoft.Json](https://www.newtonsoft.com/json)) to
anonymous pipe between the processes to allow asynchronous mutual method invocation on both sides.

TODO:

----

## How to start

TODO:

----

## License

Apache-v2.

----

## History

TODO:
