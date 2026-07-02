using Newtonsoft.Json.Linq;

namespace AxiExportPackage.Helpers
{
    public static class QueueDataHelper
    {
        public static string ExtractQueueData(string message)
        {
            JObject json = JObject.Parse(message);

            JToken queueData = json["queuedata"];

            if (queueData == null)
            {
                throw new Exception("queuedata not found in message.");
            }

            return queueData.ToString();
        }
    }
}