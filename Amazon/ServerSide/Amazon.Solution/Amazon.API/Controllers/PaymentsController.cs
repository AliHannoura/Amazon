﻿using Amazon.API.Errors;
using Amazon.Core.Entities.OrderAggregate;
using Amazon.Services.CartService.Dto;
using Amazon.Services.PaymentService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Amazon.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PaymentsController : ControllerBase
	{
		private const string _whSecret = "whsec_9957ae1953e31fe975ca159e32c6589c33d5a36b9e38c462d3f02952aa5f8319";
		private readonly IPaymentService _paymentService;
        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [Authorize]
		[HttpPost("{cartId}")] //  api/payments/cartId
		public async Task<ActionResult<CartDto>> SetPaymentIntent(string cartId)
        {
            var cartDto = await _paymentService.SetPaymentIntent(cartId);
            if (cartDto == null)
            {
                return BadRequest(new ApiResponse(400,"Problem with cart."));
            }

            return Ok(cartDto);
        }

        [HttpPost("webhook")]
        public async Task<ActionResult> StripeWebHook()
        {
			var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
			const string endpointSecret = _whSecret;

			var stripeEvent = EventUtility.ParseEvent(json);
			var signatureHeader = Request.Headers["Stripe-Signature"];

			stripeEvent = EventUtility.ConstructEvent(json,
					signatureHeader, endpointSecret);

			var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

			Order order;
            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
                order = await _paymentService.UpdatePaymentIntentToSucceededOrFailed(paymentIntent.Id, true);
            else if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
                order = await _paymentService.UpdatePaymentIntentToSucceededOrFailed(paymentIntent.Id, false);
			//else if (stripeEvent.Type == EventTypes.ChargeRefunded) // Handle the refund event
			//{
			//	var charge = stripeEvent.Data.Object as Charge;
			//	if (charge.PaymentIntentId != null)
			//	{
			//		await _paymentService.UpdateOrderStatusToRefunded(charge.PaymentIntentId);
			//	}
			//}


			return Ok();
		}
    }
}
