BuildTools Windows GUI
======================

The goal for this is, when it's finished, to be an easy to use, self-contained BuildTools GUI for Windows. It will
check for and automatically setup the proper environment for BuildTools to run in, as well as automatically check for
and download updates to the BuildTools jar itself. It is currently working on 64 bit systems, there seems to be some
issues when running with a 32 bit JVM. This seems to be a BuildTools.jar issue, though.

Downloading
-----------

You can download the most recent build directly from GitHub

No other setup needs to be done, just run the program once you've downloaded it.

Building
--------

Open the solution in Visual Studio.

To resolve the dependencies, open the NuGet Package Manager (Tools -> Library Package Manager -> Manage NuGet Packages
for Solution) and search for and install `Json.NET` and `DotNetZip`. 

Build it :)

Further Info
------------

Original author: DemonWav https://github.com/KMStev/BuildToolsGUI
