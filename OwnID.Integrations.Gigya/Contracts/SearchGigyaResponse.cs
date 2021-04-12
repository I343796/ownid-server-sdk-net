using System.Collections.Generic;
using System.Linq;

namespace OwnID.Integrations.Gigya.Contracts
{
    public class SearchGigyaResponse<TResult> : BaseGigyaResponse
    {
        private List<TResult> _results;

        public List<TResult> Results
        {
            get => _results;
            set
            {
                _results = value;
                First = _results.FirstOrDefault();
            }
        }

        public TResult First { get; private set; }
        
        public bool IsSuccess => (Results?.Any() ?? false) && ErrorCode == 0;
    }
}