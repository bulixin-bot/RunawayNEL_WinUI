using System;
using System.Collections.Generic;
using System.Text;

namespace Runaway.Protocol
{
    public class QueryBuilder
    {
        private readonly Dictionary<string, string> _parameters = new Dictionary<string, string>();

        public QueryBuilder()
        {
        }

        public QueryBuilder(string queryString)
        {
            if (string.IsNullOrEmpty(queryString))
                return;

            var pairs = queryString.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var idx = pair.IndexOf('=');
                if (idx < 0)
                {
                    _parameters[pair] = string.Empty;
                }
                else
                {
                    var key = pair.Substring(0, idx);
                    var value = idx + 1 < pair.Length ? pair.Substring(idx + 1) : string.Empty;
                    _parameters[key] = Uri.UnescapeDataString(value.Replace('+', ' '));
                }
            }
        }

        public void Add(string key, string value)
        {
            _parameters[key] = value;
        }

        public string Get(string key)
        {
            return _parameters.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public Dictionary<string, string> GetAll()
        {
            return _parameters;
        }

        public string BuildQueryString()
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var kvp in _parameters)
            {
                if (!first) sb.Append('&');
                sb.Append(Uri.EscapeDataString(kvp.Key));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(kvp.Value));
                first = false;
            }
            return sb.ToString();
        }

        public static QueryBuilder FromParameters(string url)
        {
            var uri = new Uri(url);
            var query = uri.Query;
            if (query.StartsWith("?"))
                query = query.Substring(1);
            return new QueryBuilder(query);
        }
    }
}
