﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FourLeafCloverShoe.Share.ViewModels
{
    public class RateViewModel
    {
        public Guid ID { get; set; }
        public Guid IDPro { get; set; }
        public string Contents { get; set; }
        public float? Rating { get; set; }
        public int? Status { get; set; }
        public string Size { get; set; }
        public string TenKH { get; set; }
        public string? CreateDate { get; set; }
        public byte[] AnhKh { get; set; }
    }
}
