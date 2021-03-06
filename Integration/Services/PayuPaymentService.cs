﻿using System;
using System.Collections.Generic;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.PayuRedirect.Infrastructure;
using Nop.Plugin.Payments.PayuRedirect.Integration.Models;
using Nop.Plugin.Payments.PayuRedirect.Integration.Models.Capture;
using Nop.Plugin.Payments.PayuRedirect.Integration.Models.Payment;
using Nop.Plugin.Payments.PayuRedirect.Integration.Models.Refund;
using Nop.Services.Directory;
using RestSharp;

namespace Nop.Plugin.Payments.PayuRedirect.Integration.Services
{
    internal class PayuPaymentService : IPayuPaymentService
    {
        private const string NotifyRelativeUrl = "Plugins/PaymentPayu/Return";

        private readonly IPayuRestClientFactory clientFactory;
        private readonly IPayuAuthorizationService authorizationService;
        private readonly ICurrencyService currencyService;
        private readonly PayuPaymentSettings paymentSettings;

        public PayuPaymentService(IPayuRestClientFactory clientFactory, IPayuAuthorizationService authorizationService, ICurrencyService currencyService, PayuPaymentSettings payuPaymentSettings)
        {
            this.clientFactory = clientFactory;
            this.authorizationService = authorizationService;
            this.currencyService = currencyService;
            this.paymentSettings = payuPaymentSettings;
        }

        public PayuCaptureOrderResponse CapturePayment(Order order)
        {
            var request = new PayuCaptureOrderRequest();
            request.OrderId = order.AuthorizationTransactionId;
            request.OrderStatus = PayuOrderStatusCode.Completed;
            var payUApiClient = clientFactory.GetApiClient("api/v2_1");
            var captureRequest = ResolvePreConfiguredRequest(String.Format("orders/{0}/status", request.OrderId), Method.PUT, request);
            var orderResponse = payUApiClient.Put<PayuCaptureOrderResponse>(captureRequest);

            return orderResponse.Data;
        }

        public PayuRefundResponse RequestRefund(Order order, decimal refundAmount, bool isPartial)
        {
            RestClient payuApiClient = clientFactory.GetApiClient(String.Format("/api/v2_1/orders/{0}/", order.AuthorizationTransactionId));
            var refund = new PayuRefundRequest()
            {
                Refund = new PayuRefund()
                {
                    Amount = isPartial ? refundAmount.ToString() : null,
                    Description = "refund"
                }
            };
            var request = ResolvePreConfiguredRequest("refunds", Method.POST, refund);
            var apiCallResult = payuApiClient.Post<PayuRefundResponse>(request);

            return apiCallResult.Data;
        }

        public PayuOrderResponse PlaceOrder(Order order, string customerIpAddress, string storeName, Uri storeUrl)
        {
            var payUApiClient = clientFactory.GetApiClient("api/v2_1");
            var payuOrder = ResolvePayuOrder(order, customerIpAddress, storeName, storeUrl);
            var request = ResolvePreConfiguredRequest("orders", Method.POST, payuOrder);
            var orderResponse = payUApiClient.Post<PayuOrderResponse>(request);

            if (orderResponse.ResponseStatus != ResponseStatus.Completed)
            {
                throw new InvalidOperationException("Payu service failed to place an order.");
            }

            return orderResponse.Data;
        }

        private PayuOrder ResolvePayuOrder(Order order, string customerIpAddress, string storeName, Uri storeUrl)
        {
            PayuOrder result = new PayuOrder();
            var currencyForPayuOrder = currencyService.GetCurrencyByCode(paymentSettings.Currency);

            if (currencyForPayuOrder == null)
            {
                throw new ArgumentException("Currency for PayU must be present in the store. Please, change settings of your store and/or PayU merchant account to match currencies between PayU and your store.");
            }
            //PayU Order general info
            result.CurrencyCode = currencyForPayuOrder.CurrencyCode;
            result.CustomerIp = customerIpAddress;
            result.Description = String.Format("Order from {0}", storeName);
            result.ExtOrderId = order.Id.ToString();
            result.MerchantPosId = paymentSettings.PosId;
            result.NotifyUrl = new Uri(storeUrl, NotifyRelativeUrl).ToString();
            result.TotalAmount = (int)(order.OrderTotal * 100);
            //PayU Order buyer
            result.Buyer = new PayuBuyer()
            {
                Email = order.BillingAddress.Email,
                FirstName = order.BillingAddress.FirstName,
                LastName = order.BillingAddress.LastName,
                Phone = order.BillingAddress.PhoneNumber
            };
            //PayU Order products
            List<PayuProduct> products = new List<PayuProduct>();
            foreach (var orderItem in order.OrderItems)
            {
                PayuProduct product = new PayuProduct();
                product.Name = orderItem.Product.Name;
                product.Quantity = orderItem.Quantity;
                product.UnitPrice = (int)(orderItem.Product.Price * 100);
                products.Add(product);
            }
            result.Products = products;

            return result;
        }

        private RestRequest ResolvePreConfiguredRequest(string url, Method method, object payload)
        {
            var request = new RestRequest(url, method);
            request.JsonSerializer = new RestSharpJsonNetSerializer();
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json; charset=utf-8", request.JsonSerializer.Serialize(payload), ParameterType.RequestBody);
            var authenticationToken = authorizationService.GetAuthToken();
            request.AddHeader("Authorization", String.Concat("Bearer ", authenticationToken));

            return request;
        }
    }
}
