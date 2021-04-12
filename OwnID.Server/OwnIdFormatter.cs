using Serilog.Formatting.Elasticsearch;

namespace OwnID.Server
{
    public class OwnIdFormatter : ElasticsearchJsonFormatter
    {
        // TODO: scope decoupling
        public OwnIdFormatter() : base(renderMessageTemplate: false, inlineFields: true)
        {
        }
    }
}