namespace MyThuatShop.Models
{
    public class Cart
    {
        public Dictionary<int, CartItem> Carts { get; set; } = new();

        public void Add(CartItem item)
        {
            if (Carts.TryGetValue(item.ProductId, out var exist))
                exist.Quantity += item.Quantity;
            else
                Carts[item.ProductId] = item;
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            if (!Carts.ContainsKey(productId)) return;
            if (quantity < 1) quantity = 1;
            Carts[productId].Quantity = quantity;
        }

        public void Remove(int productId) => Carts.Remove(productId);

        public int CartSize() => Carts.Count;

        public int TotalQuantity() => Carts.Values.Sum(x => x.Quantity);

        public decimal TotalAmount() => Carts.Values.Sum(x => x.SubTotal);
    }
}
