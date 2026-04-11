using AgriskyApi.Dtos;
using AgriskyApi.IRepo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AgriskyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderRepo _repo;

        public OrderController(IOrderRepo repo)
        {
            _repo = repo;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateOrder([FromForm] CreateOrderFormInput input, IFormFile? proof)
        {
            // Basic null/empty checks
            if (string.IsNullOrWhiteSpace(input.Items) ||
                string.IsNullOrWhiteSpace(input.Shipping) ||
                string.IsNullOrWhiteSpace(input.PaymentMethod))
            {
                return BadRequest(new
                {
                    message = "بعض الحقول مفقودة أو فارغة.",
                    missingFields = new
                    {
                        PaymentMethod = string.IsNullOrWhiteSpace(input.PaymentMethod),
                        Items = string.IsNullOrWhiteSpace(input.Items),
                        Shipping = string.IsNullOrWhiteSpace(input.Shipping)
                    }
                });
            }

            try
            {
                // Fix: if Swagger strips the array brackets, wrap it back
                var itemsJson = input.Items.Trim();
                if (itemsJson.StartsWith("{"))
                    itemsJson = $"[{itemsJson}]";

                var itemsList = JsonConvert.DeserializeObject<List<OrderItemDto>>(itemsJson);
                var shippingObj = JsonConvert.DeserializeObject<ShippingDto>(input.Shipping);

                if (itemsList == null || !itemsList.Any())
                    return BadRequest(new { message = "قائمة المنتجات فارغة أو غير صالحة." });

                if (shippingObj == null)
                    return BadRequest(new { message = "بيانات الشحن غير صالحة." });

                var dto = new CreateOrderDto
                {
                    UserId = input.UserId,
                    PaymentMethod = input.PaymentMethod,
                    Items = itemsList,
                    Shipping = shippingObj
                };

                var order = await _repo.CreateOrder(dto, proof);

                return Ok(new
                {
                    message = "تم إنشاء الطلب بنجاح",
                    orderId = order.OrderID,
                    total = order.TotalAmount,
                    status = order.Status
                });
            }
            catch (JsonReaderException ex)
            {
                return BadRequest(new
                {
                    message = "خطأ في صيغة الـ JSON المرسلة. تأكد من الأقواس والاقتباسات.",
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "حدث خطأ أثناء تنفيذ الطلب", error = ex.Message });
            }
        }
    }
}