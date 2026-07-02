namespace AxiInstallConsumerPackage.Helpers
{
    public interface IPackageImportService
    {
        Task ProcessPackageImport(string queueData);
    }
}