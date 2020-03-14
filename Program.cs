using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;

namespace RubyToC3
{
    class ChartRequest
    {
        public string Method { get; set; }

        public Dictionary<string, object> ChartParams { get; set; }

        public ChartRequest(string method, Dictionary<string, object> chartParams)
        {
            Method = method;
            ChartParams = chartParams;
        }

        private Uri Url => new Uri(
            $"http://{ConfigurationManager.AppSettings["hostname"]}:{ConfigurationManager.AppSettings["port"]}/{chart_path()}?{query_params()}");


        private string ResultsName => ChartParams.ContainsKey("as") ? (string) ChartParams["as"] : Method;

        private string query_params()
        {
            Dictionary<string, string> param = new Dictionary<string, string>();

            DateRange dateRange = parse_date_range(Convert.ToString(ChartParams["date_range"]));

            param["start_date"] = dateRange.Start.ToString(CultureInfo.CurrentCulture);
            param["end_date"] = dateRange.End.ToString(CultureInfo.CurrentCulture);

            ChartParams["ed"] = ChartParams.ContainsKey("er") ? ChartParams["er"] : "";
            foreach (var key in additional_keys())
            {
                if (ChartParams.ContainsKey(key))
                    param[key] = "1";
            }

            if (ChartParams.ContainsKey("inactive_facilities"))
                param["inactive_facilities"] = Convert.ToString(ChartParams["inactive_facilities"]);

            if (ChartParams.ContainsKey("date_filter_type"))
                param["date_filter_type"] = Convert.ToString(ChartParams["date_filter_type"]);

            if (ChartParams.ContainsKey("exclude_cancel"))
                param["exclude_cancel"] = Convert.ToString(ChartParams["exclude_cancel"]);
            else
                param["exclude_cancel"] = "off";

            foreach (var t in chartable_resources())
            {
                if (ChartParams.ContainsKey(t))
                {
                    param[t] = ChartParams[t].GetType() == typeof(Array)
                        ? string.Join(",", ChartParams[t])
                        : Convert.ToString(ChartParams[t]);
                }
            }

            return HttpUtility.UrlEncode(string.Join("&",
                param.Select(kvp =>
                    $"{kvp.Key}={kvp.Value}")));
        }

        private DateRange parse_date_range(string range)
        {
            try
            {
                if (ChartParams.ContainsKey("start_date") && ChartParams.ContainsKey("end_date"))
                    return new DateRange(Convert.ToString(ChartParams["start_date"]),
                        Convert.ToString(ChartParams["end_date"]));
                string[] splitter = {" - "};
                string[] dates = range.Split(splitter, StringSplitOptions.None);
                return new DateRange(dates[0], dates[1]);
            }
            catch (Exception)
            {
                int daysToDisplay = ChartParams.ContainsKey("default_days_to_display")
                    ? Convert.ToInt32(
                        ChartParams["default_days_to_display"])
                    : 30;
                return new DateRange(DateTime.Now.AddDays(daysToDisplay * -1).ToString(CultureInfo.CurrentCulture),
                    DateTime.Now.ToString(CultureInfo.CurrentCulture));
            }
        }

        protected string[] additional_keys()
        {
            return new[]
            {
                "mobile", "ed", "pcp", "ucc", "inactive"
            };
        }

        private string[] chartable_resources()
        {
            return new[] {"health_systems", "facilities", "locations", "providers", "regions", "state"};
        }

        private string chart_path()
        {
            if (ChartParams.ContainsKey("facility_id"))
            {
                Facility facility = Facility.GetById(Convert.ToInt32(ChartParams["facility_id"]));
                return HttpUtility.UrlEncode($"{Method}/{facility.health_system.id}/{facility.id}");
            }
            else if (ChartParams.ContainsKey("health_system_id"))
            {
                HealthSystem healthSystem = HealthSystem.find_by(Convert.ToInt32(ChartParams["health_system_id"]));
                return HttpUtility.UrlEncode($"{Method}/{@healthSystem.id}");
            }
            else
            {
                return HttpUtility.UrlEncode(Method);
            }
        }
    }
}
