namespace AgriskyApi.Dtos
{
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class ShippingDto
    {
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class CreateOrderDto
    {
        public int UserId { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public ShippingDto Shipping { get; set; } = new();
        public string PaymentMethod { get; set; } = string.Empty;
    }

    /// <summary>
    /// Used to receive multipart/form-data from the API.
    /// Items and Shipping are raw JSON strings that get deserialized in the controller.
    /// </summary>
    public class CreateOrderFormInput
    {
        public int UserId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// JSON array of items. Example: [{"ProductId": 1, "Quantity": 2}]
        /// </summary>
        public string Items { get; set; } = string.Empty;

        /// <summary>
        /// JSON object for shipping. Example: {"Address":"Cairo","City":"Cairo","Country":"Egypt","ZipCode":"12345","PhoneNumber":"01000000000"}
        /// </summary>
        public string Shipping { get; set; } = string.Empty;
    }
}