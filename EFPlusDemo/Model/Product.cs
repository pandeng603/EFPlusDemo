namespace EFPlusDemo.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Product")]
    public partial class Product
    {
        [Key]
        public int PId { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductName { get; set; }

        public int CategoryId { get; set; }

        [Column(TypeName = "money")]
        public decimal Price { get; set; }

        public DateTime Created { get; set; }

        [Column(TypeName = "money")]
        public decimal SPrice { get; set; }

        public virtual Category Category { get; set; }
    }
}
