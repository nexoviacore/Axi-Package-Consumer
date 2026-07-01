namespace AxiExportPackage.Services.Interfaces
{
    public interface IPackageExportService
    {
        Task ProcessPackageExport(string queueData);
    }
}