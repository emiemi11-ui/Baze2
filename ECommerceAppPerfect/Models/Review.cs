using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceAppPerfect.Models
{
    // CLASA REVIEW - Entitatea pentru review-uri produse
    //
    // CE ESTE UN REVIEW?
    // Un review este o evaluare a unui produs de catre un client
    // Contine:
    // - Rating (1-5 stele)
    // - Comentariu (optional)
    // - Data review-ului
    // - Daca e verified purchase (a cumparat produsul)
    //
    // DE CE SUNT IMPORTANTE REVIEW-URILE?
    // 1. Ajuta alti clienti sa ia decizii
    // 2. Feedback pentru proprietar
    // 3. Credibilitate magazin
    // 4. SEO (continut generat de utilizatori)
    //
    // RELATII:
    // - Product (Many-to-One): Review-ul e pentru un produs
    // - Customer (Many-to-One): Review-ul e scris de un client
    //
    // CONSTRANGERE UNICA:
    // UNIQUE (ProductID, CustomerID) - un client poate lasa
    // UN SINGUR review per produs (previne spam)
    [Table("Reviews")]
    public partial class Review
    {
        // PROPRIETATI - Coloanele din tabel

        // ReviewID - Cheia primara
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReviewID { get; set; }

        // ProductID - Foreign Key catre Products
        //
        // Produsul evaluat
        // ON DELETE CASCADE - daca stergem produsul, se sterg si review-urile
        [Required]
        [ForeignKey("Product")]
        public int ProductID { get; set; }

        // CustomerID - Foreign Key catre Users
        //
        // Clientul care a scris review-ul
        // Trebuie sa fie un User cu UserRole = "Customer"
        [Required]
        [ForeignKey("Customer")]
        public int CustomerID { get; set; }

        // Rating - Nota de la 1 la 5
        //
        // CHECK (Rating >= 1 AND Rating <= 5) in SQL
        // 1 = foarte rau, 5 = excelent
        //
        // AFISARE UI: stele (pline/goale)
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        // Comment - Comentariul clientului
        //
        // Optional - clientul poate lasa doar rating fara text
        // Maxim 1000 caractere pentru a preveni spam
        [StringLength(1000)]
        public string Comment { get; set; }

        // ReviewDate - Data cand s-a postat review-ul
        //
        // DEFAULT GETDATE() = momentul curent
        public DateTime ReviewDate { get; set; }

        // IsVerifiedPurchase - A cumparat clientul produsul?
        //
        // True daca clientul a avut o comanda cu acest produs
        // Se seteaza automat la creare verificand OrderDetails
        //
        // AFISARE UI: "Verified Purchase" badge verde
        public bool IsVerifiedPurchase { get; set; }

        // PROPRIETATI DE NAVIGARE

        // Product - Produsul evaluat
        public virtual Product Product { get; set; }

        // Customer - Clientul care a scris review-ul
        public virtual User Customer { get; set; }

        // PROPRIETATI CALCULATE

        // StarDisplay - Rating-ul ca stele (pentru afisare simpla)
        //
        // Returneaza string cu stele pline si goale
        // Exemplu: Rating=4 => "****"
        [NotMapped]
        public string StarDisplay
        {
            get
            {
                string filled = new string('*', Rating);
                string empty = new string(' ', 5 - Rating);
                return filled + empty;
            }
        }

        // RatingDescription - Descrierea rating-ului ca text
        //
        // Transforma numarul in text descriptiv
        [NotMapped]
        public string RatingDescription
        {
            get
            {
                return Rating switch
                {
                    1 => "Poor",
                    2 => "Fair",
                    3 => "Good",
                    4 => "Very Good",
                    5 => "Excellent",
                    _ => "Unknown"
                };
            }
        }

        // TimeSinceReview - Cat timp a trecut de la review
        //
        // Format: "2 days ago", "1 month ago", etc.
        [NotMapped]
        public string TimeSinceReview
        {
            get
            {
                var timeSpan = DateTime.Now - ReviewDate;

                if (timeSpan.TotalDays < 1)
                    return "Today";
                if (timeSpan.TotalDays < 2)
                    return "Yesterday";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays} days ago";
                if (timeSpan.TotalDays < 30)
                    return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
                if (timeSpan.TotalDays < 365)
                    return $"{(int)(timeSpan.TotalDays / 30)} months ago";

                return $"{(int)(timeSpan.TotalDays / 365)} years ago";
            }
        }

        // ShortComment - Comentariul prescurtat (pentru preview)
        //
        // Primele 100 de caractere, cu "..." daca e mai lung
        [NotMapped]
        public string ShortComment
        {
            get
            {
                if (string.IsNullOrEmpty(Comment))
                    return "(No comment)";

                if (Comment.Length <= 100)
                    return Comment;

                return Comment.Substring(0, 100) + "...";
            }
        }

        // ReviewerName - Numele reviewerului
        //
        // Pentru confidentialitate, afisam doar primul nume
        // sau username daca nu are nume
        [NotMapped]
        public string ReviewerName
        {
            get
            {
                if (Customer == null)
                    return "Anonymous";

                if (!string.IsNullOrEmpty(Customer.FirstName))
                    return Customer.FirstName;

                return Customer.Username;
            }
        }
    }
}
