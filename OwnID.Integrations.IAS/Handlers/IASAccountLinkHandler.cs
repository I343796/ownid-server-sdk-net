using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Link;
using System;
using System.Threading.Tasks;

namespace OwnID.Integrations.IAS.Handlers
{
    class IASAccountLinkHandler : IAccountLinkHandler
    {
        public Task<LinkState> GetCurrentUserLinkStateAsync(string payload)
        {
            throw new NotImplementedException();
        }

        public Task OnLinkAsync(string did, OwnIdConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
