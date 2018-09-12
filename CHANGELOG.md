# Changelog

## v1.1 - 2018.09.13
- Set TargetFramework to net472.
- Use [Nerdbank.GitVersioning](https://github.com/AArnott/Nerdbank.GitVersioning) to automate generation of assembly 
  and nuget package versions.

## v1.0 - 2018.05.24
- Initial release with Cassandra [v2.2.12](http://archive.apache.org/dist/cassandra/2.2.12/) and 
  [v3.11.2](http://archive.apache.org/dist/cassandra/3.11.2/) support.
- Both Cassandra versions are patched to use "old" thread pool due to low system timer resolution on Windows.
  Sources for patched Cassandra v2.2.12 are [here](https://github.com/skbkontur/cassandra/tree/cassandra-2.2.12-oldThreadPool)
  and for v3.11.2 are [there](https://github.com/skbkontur/cassandra/tree/cassandra-3.11.2-oldThreadPool).
