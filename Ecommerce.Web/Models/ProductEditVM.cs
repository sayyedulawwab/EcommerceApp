﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ecommerce.Web;

public class ProductEditVM
{
    public int ProductID { get; set; }
    [Required(ErrorMessage = "Please provide a name")]
    public string Name { get; set;}
    [Required(ErrorMessage = "Please provide a price")]
    public double Price { get; set;}
    [Required(ErrorMessage = "Please provide a quantity")]
    public int Quantity { get; set;}
    public int? ProductCategoryID  { get; set;}
    public IEnumerable<SelectListItem>? ProductCategories { get; set; }
}
