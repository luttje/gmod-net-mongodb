# Gmod MongoDB Wrapper (Abandoned)

> ## 🚧 Abandoned! Unfinished and Unstable Code
> This project is no longer maintained. It is publicly archived in the hope that it may be a useful reference to those working with [Gmod.NET](https://github.com/GmodNET/GmodDotNet).

This is a Garry's Mod module that has the goal to expose all [.NET MongoDB Driver](https://docs.mongodb.com/drivers/csharp/) functionality to Lua. We do this by automatically binding all public classes, methods, properties and fields in the MongoDB .NET Driver. [&raquo; See 'Differences with the MongoDB .NET Driver'](https://luttje.github.io/gmod-net-mongodb/#differences-with-the-mongodb-net-driver)

This module is built using [Gmod.NET](https://github.com/GmodNET/GmodDotNet).

## Requirements

* Install at least version `1.0.0` of [Gmod.NET](https://github.com/GmodNET/GmodDotNet).    
    [&raquo; Check the dependencies for the version](https://github.com/luttje/gmod-net-mongodb/network/dependencies)
* Garry's Mod must be configured to use the `x86_64` beta branch.
* You must have a MongoDB Server installed and running.    
    [&raquo; Get the MongoDB Community Server here](https://www.mongodb.com/try/download/community)

    *Tip: Install **MongoDB Compass** when the Community Server Installer recommends it to you.*

## Installation

1. Download [a release](https://github.com/luttje/gmod-net-mongodb/releases)
2. Unzip the downloaded release to your `garrysmod\lua\bin\Modules\GmodMongoDb` directory (create the `bin`, `bin\Modules` and/or `bin\Modules\GmodMongoDb` directories if they dont exist)

## 📚 Documentation

* [💻 Learn how to use this module from the documentation](https://luttje.github.io/gmod-net-mongodb)

* [🧪 Learn by example from the tests](./GmodMongoDb/Tests/Lua/gmodmongodb/)

The documentation contains examples, explanations and (automatically generated) references for all the exposed functionality.

The API Documentation is generated when a contributor pushes to the `main` branch. This is done using a GitHub Workflow with [DocFx](https://dotnet.github.io/docfx/). You can find those docs on [this GitHub Pages Environment](https://luttje.github.io/gmod-net-mongodb/).
