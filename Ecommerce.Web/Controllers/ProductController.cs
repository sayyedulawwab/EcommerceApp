using Ecommerce.Models;
using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;

using Ecommerce.Services.Abstractions.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ecommerce.Web;

public class ProductController : Controller
{
    IProductService _productService;
    IProductCategoryService _productCategoryService;
    public ProductController(IProductService productService, IProductCategoryService productCategoryService)
    {
        _productService = productService;
        _productCategoryService = productCategoryService;
    }

    public IActionResult Index(ProductSearchCriteria productSearchCriteria) {

        var productCategories = _productCategoryService.GetAll();
        var productCategorylistItems = productCategories.Select(c => new SelectListItem()
        {
            Value = c.ProductCategoryID.ToString(),
            Text = c.Name,
        });


        var products = _productService.Search(productSearchCriteria);

        ICollection<ProductListItem> productModels = products.Select(product => new ProductListItem()
        {
            ProductID = product.ProductID,
            Name = product.Name,
            Price = product.Price,
            Quantity = product.Quantity,
            ProductCategoryName = product.ProductCategory?.Name
            
        }).ToList();

        var productListModel = new ProductListVM
        {
            ProductList = productModels,
            ProductCategoryList = productCategorylistItems
        };


        return View(productListModel);
    }

    // GET: Product/Create/5
    public IActionResult Create() {

        var productCategories = _productCategoryService.GetAll();

        var productCategorylistItems = productCategories.Select(c => new SelectListItem()
        {
            Value = c.ProductCategoryID.ToString(),
            Text = c.Name,
        });

        var model = new ProductCreateVM
        {
            ProductCategories = productCategorylistItems
        };

        return View(model);
    }

    // POST: Product/Create/5
    [HttpPost]
    public IActionResult Create(ProductCreateVM model) {

        if (ModelState.IsValid)
        {
            var product = new Product()
            {
                Name = model.Name,
                Price = model.Price,
                Quantity = model.Quantity,
                ProductCategoryID = model.ProductCategoryID
            };
            
            bool isSuccess = _productService.Add(product);

            if (isSuccess)
            {
                return RedirectToAction(nameof(Index));
            }
            
        }
        return View();
    }

    // GET: Product/View/5
    public IActionResult View(int? id)
    {
        if (id == null || id <= 0)
        {
            ViewBag.Error = "Please provide valid id.";
            return View();
        }

        var product = _productService.GetById((int)id);

        if (product == null)
        {
            ViewBag.Error = "Sorry, no product found for this id.";
            return View();
        }

        var model = new ProductViewVM()
        {
     
            Name = product.Name,
            Price = product.Price,
            Quantity = product.Quantity,
            ProductCategoryName = product.ProductCategory.Name,
            ProductCategoryCode = product.ProductCategory.Code

        };

        return View(model);
    }

    // GET: Product/Edit/5
    public IActionResult Edit(int? id)
    {
        if (id == null || id <= 0)
        {
            ViewBag.Error = "Please provide valid id.";
            return View();
        }

        var product = _productService.GetById((int)id);

        var productCategories = _productCategoryService.GetAll();

        var productCategorylistItems = productCategories.Select(c => new SelectListItem()
        {
            Value = c.ProductCategoryID.ToString(),
            Text = c.Name,
        });

       

        if (product == null)
        {
            ViewBag.Error = "Sorry, no product category found for this id.";
            return View();
        }

        var model = new ProductEditVM()
        {
            ProductID = product.ProductID,
            Name = product.Name,
            Price = product.Price,
            Quantity = product.Quantity,
            ProductCategoryID = product.ProductCategoryID,
            ProductCategories = productCategorylistItems
        };

        return View(model);
    }

    // POST Product/Edit/5
    [HttpPost]
    public IActionResult Edit(ProductEditVM model)
    {
        if(ModelState.IsValid) 
        {
            var Product = _productService.GetById(model.ProductID);

            if (Product == null)
            {
                ViewBag.Error = "Prouduct Category not found to update!";
                return View(model);
            }

            //_mapper.Map(model, customer)

            Product.Name = model.Name;
            Product.Price = model.Price;
            Product.Quantity = model.Quantity;
            Product.ProductCategoryID = model.ProductCategoryID;
            



            bool isSuccess = _productService.Update(Product);
            if (isSuccess)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        return View(model);
    }

    // GET: Product/Delete/5
    public IActionResult Delete(int id)
    {
        return View();
    }

    // POST: Product/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id, IFormCollection collection)
    {
        try
        {
            var Product = _productCategoryService.GetById((int)id);

            _productCategoryService.Delete(Product);

            return RedirectToAction(nameof(Index));
            
        }
        catch
        {
            return View();
        }
    }

}
