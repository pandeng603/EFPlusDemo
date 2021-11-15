namespace EFPlusDemo.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("User")]
    public partial class User
    {
        [Key]
        [StringLength(1)]
        public string UId { get; set; }

        [StringLength(1)]
        public string Name { get; set; }

        public int? Age { get; set; }

        [StringLength(1)]
        public string Gender { get; set; }

        [Required]
        [StringLength(1)]
        public string Class { get; set; }

        public virtual Class Class1 { get; set; }
    }
}
