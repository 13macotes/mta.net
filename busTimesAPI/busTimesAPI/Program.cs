using System;
using System.Net.Http;
using System.Collections.Generic;
using System.IO;
using System.Json;

namespace MTAapi
{
    class Program
    {
        public static string URL = "http://bustime.mta.info/api/siri/";
        public static HttpClient httpClient = new HttpClient();
        public static string token = File.ReadAllText("token.txt");


        //static void Main(string[] args)
        //{
        //    token = File.ReadAllText("token.txt");
        //    if (string.IsNullOrEmpty(token)) { Print("No token in token.txt"); Console.ReadKey(); return; }
        //    Print("Token: " + token);
        //    char key = ' ';
        //    while(!(key=='Q')||!(key=='q')) {
        //        BusTimeData busResult = GetRequest("308214")[0];

        //        Print(busResult.name);
        //        Print(busResult.location.ToReadable());
        //        Print(busResult.stopData.DistanceText());

        //        key=Console.ReadKey().KeyChar;

        //    }

        //    Console.ReadKey();

        //}

        static string CreateGetURL(string baseURL, Dictionary<string, string> args, bool verbose = false)
        {
            string r = baseURL + "stop-monitoring.json?";
            foreach (string key in args.Keys)
            {
                r += key + "=" + args[key] + "&";
            }
            if (verbose) Print(r);
            return r;
        }

        public static BusTimeData[] GetRequest(string monitoringRef)
        {
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                {"key",token},
                {"version","2"},
                {"MonitoringRef",monitoringRef},
                {"OperatorRef","MTA"},
                {"StopMonitoringDetailLevel","minimum"},
                {"MaximumNumberOfCallsOnwards","5"},
                {"MaximumStopVisits","5"},
                {"MinimumStopVisitsPerLine","5"}
            };
            System.Threading.Tasks.Task<HttpResponseMessage> response;
            response = httpClient.GetAsync(CreateGetURL(URL, args, false));

            string c = response.Result.Content.ReadAsStringAsync().Result;

            return CreateBusTimeData(c);
        }
        

        static void Print(string s, bool newLine = true)
        {
            if (newLine) Console.WriteLine(s);
            else Console.Write(s);
        }

        public struct BusTimeData {
            public string name;
            public string destinationName;

            public string departureTime;

            public Location location;

            public bool monitored;

            public SeatsType occupancy;

            public string vehicleRef;

            public MonitoredCall stopData;
            //public int OriginAimedDepartureTime;
        }

        public enum SeatsType{
            full,
            seatsAvailable,
            standingAvailable,
            notAvailable
        }

        public struct Location {
            public string ToReadable()
            {
                return "("+bearing+")\nLatitude: " + latitude + "\nLongitude: " + longitude;
            }
            public float longitude;
            public float latitude;
            public float bearing;
        }

        public struct MonitoredCall {
            public int distanceFromStop;
            public int numberOfStopsAway;
            public int visitNumber;
            public string DistanceText () {
                return Math.Round((double)distanceFromStop/1852,1)+" miles away";
            }
        }

        static string TryGetJsonValue(JsonValue json, string v) {
            return json.ContainsKey(v) ? json[v].ToString() : null;
        }

        static BusTimeData[] CreateBusTimeData(string jsonString) {
            JsonValue json = JsonValue.Parse(jsonString)["Siri"]["ServiceDelivery"]["StopMonitoringDelivery"][0];
            if (json == null) { Print("NULL RESULT."); return null; }
            Print("Successful Request");

            List<BusTimeData> results = new List<BusTimeData>();
            foreach(JsonValue j in json["MonitoredStopVisit"]) {
                JsonValue stopVisit = j["MonitoredVehicleJourney"];

                SeatsType occupancy = SeatsType.notAvailable;
                if (stopVisit.ContainsKey("Occupancy"))
                {
                    switch ((string)stopVisit["Occupancy"])
                    {
                        case "full":
                            occupancy = SeatsType.full;
                            break;
                        case "seatsAvailable":
                            occupancy = SeatsType.seatsAvailable;
                            break;
                        case "standingAvailable":
                            occupancy = SeatsType.standingAvailable;
                            break;
                    }
                }

                BusTimeData result = new BusTimeData
                {
                    name = stopVisit["PublishedLineName"][0].ToString(),
                    destinationName = stopVisit["DestinationName"][0].ToString(),
                    departureTime = TryGetJsonValue(stopVisit, "OriginAimedDepartureTime"),

                    location = new Location
                    {
                        longitude = stopVisit["VehicleLocation"]["Longitude"],
                        latitude = stopVisit["VehicleLocation"]["Latitude"],
                        bearing = stopVisit["Bearing"]
                    },

                    occupancy = occupancy,

                    stopData = new MonitoredCall
                    {
                        distanceFromStop = stopVisit["MonitoredCall"]["DistanceFromStop"],
                        numberOfStopsAway = stopVisit["MonitoredCall"]["NumberOfStopsAway"],
                        visitNumber = stopVisit["MonitoredCall"]["VisitNumber"]
                    },

                    monitored = stopVisit["Monitored"],
                    vehicleRef = stopVisit["VehicleRef"].ToString()
                    };
                results.Add(result);
            }
            return results.ToArray();
        }
    }
}
