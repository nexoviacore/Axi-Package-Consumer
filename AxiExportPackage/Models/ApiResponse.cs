namespace AxiExportPackage.Models
{
    public class ApiResponse
    {
        public bool Success { get; set; }

        public int StatusCode { get; set; }

        public string Message { get; set; }

        public string ResponseContent { get; set; }
    }
}