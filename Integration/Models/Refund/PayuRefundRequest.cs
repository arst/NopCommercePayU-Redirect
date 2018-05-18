﻿using Newtonsoft.Json;

namespace Nop.Plugin.Payments.PayU.Integration.Models.Refund
{
    public class PayuRefundRequest
    {
        [JsonProperty("refund")]
        public PayuRefund Refund { get; set; }

        public PayuRefundRequest()
        {
            this.Refund = new PayuRefund();
        }
    }
}
