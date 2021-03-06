﻿namespace Nop.Plugin.Payments.PayuRedirect.Integration.Models
{
    internal static class PayuOrderStatusCode
    {
        public const string Success = "SUCCESS";

        public const string WaitingForConfirmation = "WAITING_FOR_CONFIRMATION";

        public const string Pending = "PENDING";

        public const string Completed = "COMPLETED";

        public const string Rejected = "REJECTED";

        public const string Canceled = "CANCELED";
    }
}
