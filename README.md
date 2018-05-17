# cassandra-local
[![NuGet Status](https://img.shields.io/nuget/v/SkbKontur.Cassandra.Local.svg)](https://www.nuget.org/packages/SkbKontur.Cassandra.Local/)
[![Build status](https://ci.appveyor.com/api/projects/status/fxjye45x38hgvamu?svg=true)](https://ci.appveyor.com/project/vostok/cassandra-local)

Apache Cassandra binaries for local testing on Windows.

Simplest usage:
```
var templateDirectory = DirectoryHelpers.FindDirectory(AppDomain.CurrentDomain.BaseDirectory, "cassandra-local/cassandra/v3.11.x");
var deployDirectory = Path.Combine(Path.GetTempPath(), "deployed_cassandra_v3.11.x");
var node = new LocalCassandraNode(templateDirectory, deployDirectory)
{
    RpcPort = 9160,
    CqlPort = 9042,
    HeapSize = "256M"
};
node.Retart();
...
node.Stop();
```
