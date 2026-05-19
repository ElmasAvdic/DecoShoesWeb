using System.ComponentModel.DataAnnotations;

namespace DecoShoesWeb.Models
{
    public class CheckoutViewModel
    {
        public List<CartItem> Items { get; set; } = new();

        public decimal Total => Items.Sum(item => item.Total);

        [Required(ErrorMessage = "Ime je obavezno.")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Prezime je obavezno.")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "Email je obavezan.")]
        [EmailAddress(ErrorMessage = "Email nije ispravan.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Telefon je obavezan.")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Adresa je obavezna.")]
        public string Address { get; set; } = "";

        [Required(ErrorMessage = "Grad je obavezan.")]
        public string City { get; set; } = "";

        public string? PostalCode { get; set; }

        [Required(ErrorMessage = "Način plaćanja je obavezan.")]
        public string PaymentMethod { get; set; } = "Plaćanje pouzećem";

        public bool SaveCustomerInfo { get; set; }
    }
}
