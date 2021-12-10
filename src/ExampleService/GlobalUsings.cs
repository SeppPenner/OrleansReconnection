#pragma warning disable IDE0065 // Die using-Anweisung wurde falsch platziert.
global using System.Diagnostics.CodeAnalysis;
global using System.Net;
global using System.Reflection;
global using System.Text;

global using ExampleGrainInterfaces;

global using Orleans;
global using Orleans.Hosting;

global using Serilog;
global using Serilog.Events;
global using Serilog.Exceptions;

global using ClusterOptions = Orleans.Configuration.ClusterOptions;
global using ILogger = Serilog.ILogger;
#pragma warning restore IDE0065 // Die using-Anweisung wurde falsch platziert.
