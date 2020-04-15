namespace Decidehub.Core.Interfaces
{
    public interface ITenantProvider
    {
        string GetTenantId();

        string GetSubdomain();
    }
}