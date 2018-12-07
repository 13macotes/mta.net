# mta.net
Unofficial MTA Bus Time API using C#.net library

# Usage
The library needs a txt file with it called token.txt with an API key. There is one already included with the library.

```c#
//using MTAapi

BusTimeData[] entries = GetRequest("308214") //"308214"; = bus reference
if(entries.count > 0) {
  BusTimeData data = entries[0];
  Print(busResult.name);
  Print(busResult.location.ToReadable());
  Print(busResult.stopData.DistanceText());
}
```
