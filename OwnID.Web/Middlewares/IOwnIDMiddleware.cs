using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace OwnID.Web.Middlewares
{
    public interface IOwnIDMiddleware
    {
        Task InvokeAsync(HttpContext httpContext);
    }
}