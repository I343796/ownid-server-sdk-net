using System.Globalization;
using OwnIdSdk.NetCore3.Extensibility.Flow.Contracts;

namespace OwnIdSdk.NetCore3.Flow.Commands
{
    public class CommandInput<T> : CommandInput
    {
        public CommandInput()
        {
        }

        public CommandInput(RequestIdentity requestIdentity, CultureInfo cultureInfo, T data) : base(requestIdentity,
            cultureInfo)
        {
            Data = data;
        }

        public T Data { get; set; }
    }

    public class CommandInput : ICommandInput
    {
        public CommandInput()
        {
        }

        public CommandInput(RequestIdentity requestIdentity, CultureInfo cultureInfo)
        {
            Context = requestIdentity.Context;
            RequestToken = requestIdentity.RequestToken;
            ResponseToken = requestIdentity.ResponseToken;
            CultureInfo = cultureInfo;
        }

        public string Context { get; set; }

        public string RequestToken { get; set; }

        public string ResponseToken { get; set; }

        public CultureInfo CultureInfo { get; set; }
    }

    public interface ICommandInput
    {
        public string Context { get; set; }

        public string RequestToken { get; set; }

        public string ResponseToken { get; set; }

        public CultureInfo CultureInfo { get; set; }
    }
}