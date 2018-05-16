﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayU.Integration.Models.Capture
{
    public class PayuCaptureOrderResponse
    {
        [JsonProperty("statusCode")]
        public string StatusCode { get; set; }

        [JsonProperty("statusDesc")]
        public string StatusDesc { get; set; }
    }
}