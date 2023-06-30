using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EDeals.Api.Controllers.GatewayControllers
{
    // In order to override the authorization used by the gateway middleware for a forwarded request
    // add an empty method (public void is fine) here with the correct method and route attribute (e.g. [HttpPost("api/v1/auth/endpoint")] )
    // and set the desired authorization attributes (e.g. [Authorize] or [Authorize("policy")] or [AllowAnonymous] etc.)
    [Route("[controller]")]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet("api/brand/{id}")]
        public void GetBrand() { }

        [AllowAnonymous]
        [HttpPost("api/cartitem")]
        public void AddCartItem() { }
        
        [AllowAnonymous]
        [HttpGet("api/cartitem/{id}")]
        public void GetCartItem() { }
        
        [AllowAnonymous]
        [HttpDelete("api/cartitem/{id}")]
        public void DeleteCartItem() { }
        
        [AllowAnonymous]
        [HttpGet("api/cartitem/all")]
        public void GetCartItems() { }
        
        [AllowAnonymous]
        [HttpPut("api/cartitem")]
        public void UpdateCartItem() { }

        [AllowAnonymous]
        [HttpPost("api/shoppingsession")]
        public void Addshoppingsession() { }

        [AllowAnonymous]
        [HttpGet("api/shoppingsession/{id}")]
        public void Getshoppingsession() { }

        [AllowAnonymous]
        [HttpDelete("api/shoppingsession/{id}")]
        public void Deleteshoppingsession() { }

        [AllowAnonymous]
        [HttpPut("api/shoppingsession")]
        public void Updateshoppingsession() { }

        [AllowAnonymous]
        [HttpGet("api/category/{id}")]
        public void GetCategory() { }

        [AllowAnonymous]
        [HttpGet("api/category/all")]
        public void GetCategories() { }
        
        [AllowAnonymous]
        [HttpGet("api/discount/{id}")]
        public void GetDiscount() { }

        [AllowAnonymous]
        [HttpGet("api/discount/all")]
        public void GetDiscounts() { }


        [AllowAnonymous]
        [HttpGet("api/product/{id}")]
        public void GetProduct() { }

        [AllowAnonymous]
        [HttpGet("api/product/all")]
        public void GetProductss() { }
        
        [AllowAnonymous]
        [HttpGet("api/homepage")]
        public void GetHomePage() { }

        [AllowAnonymous]
        [HttpGet("api/seller/{id}")]
        public void GetSeller() { }
        
        [AllowAnonymous]
        [HttpGet("api/paymentcontroll")]
        public void CreatePaymentIntent() { }
        
        [AllowAnonymous]
        [HttpGet("api/order/draft")]
        public void CreateDraftOrder() { }
        
        [AllowAnonymous]
        [HttpGet("api/order/{id}")]
        public void CreateOrder() { }
    }
}
