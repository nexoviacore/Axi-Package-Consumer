namespace AxiExportPackage.Models
{
    public class ExportResult
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; }

        public string StepName { get; set; }

        public DateTime ProcessedTime { get; set; }
    }
}