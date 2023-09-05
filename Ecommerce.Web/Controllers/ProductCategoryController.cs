using Microsoft.AspNetCore.Mvc;
using Ecommerce.Models;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Services.Abstractions.Products;
using Ecommerce.Models.EntityModels;

namespace Ecommerce.Web;

public class ProductCategoryController : Controller
{
    private readonly IProductCategoryService _productCategoryService;
    public ProductCategoryController(IProductCategoryService productCategoryService)
    {
        _productCategoryService = productCategoryService;
    }

    // GET: ProductCategory/
    public IActionResult Index(ProductCategorySearchCriteria productCategorySearchCriteria) {

        var productCategories = _productCategoryService.Search(productCategorySearchCriteria);

        ICollection<ProductCategoryListItem> productCategoryModels = productCategories.Select(productCategory=> new ProductCategoryListItem()
        {
            ProductCategoryID = productCategory.ProductCategoryID,
            Name = productCategory.Name,
            Code = productCategory.Code,
        }).ToList();

        var productCategoryListModel = new ProductCategoryListVM();

        productCategoryListModel.ProductCategoryList = productCategoryModels;

        return View(productCategoryListModel);
    }

    // GET: ProductCategory/Create/5
    public IActionResult Create() {
        return View();
    }

    // POST: ProductCategory/Create/5
    [HttpPost]
    public IActionResult Create(ProductCategoryCreateVM model) {

        if (ModelState.IsValid)
        {
            var productCategory = new ProductCategory()
            {
                Name = model.Name,
                Code = model.Code,
                
            };
            //Database operations 
            bool isSuccess = _productCategoryService.Add(productCategory);

            if (isSuccess)
            {
                return RedirectToAction("Index");
            }
            
        }
        return View();
    }

    // GET: ProductCategory/View/5
    public IActionResult View(int? id)
    {
        if (id == null || id <= 0)
        {
            ViewBag.Error = "Please provide valid id.";
            return View();
        }

        var productCategory = _productCategoryService.GetById((int)id);

        if (productCategory == null)
        {
            ViewBag.Error = "Sorry, no product category found for this id.";
            return View();
        }

        var model = new ProductCategoryViewVM()
        {
            ProductCategoryID = productCategory.ProductCategoryID,
            Name = productCategory.Name,
            Code = productCategory.Code,

        };

        return View(model);
    }

    // GET: ProductCategory/Edit/5
    public IActionResult Edit(int? id)
    {
        if (id == null || id <= 0)
        {
            ViewBag.Error = "Please provide valid id.";
            return View();
        }

        var productCategory = _productCategoryService.GetById((int)id);

        if (productCategory == null)
        {
            ViewBag.Error = "Sorry, no product category found for this id.";
            return View();
        }

        var model = new ProductCategoryEditVM()
        {
            ProductCategoryID = productCategory.ProductCategoryID,
            Name = productCategory.Name,
            Code = productCategory.Code,

        };

        return View(model);
    }

    // POST ProductCategory/Edit/5
    [HttpPost]
    public IActionResult Edit(ProductCategoryEditVM model)
    {
        if(ModelState.IsValid) 
        {
            var productCategory = _productCategoryService.GetById(model.ProductCategoryID);

            if (productCategory == null)
            {
                ViewBag.Error = "Product Category not found to update!";
                return View(model);
            }

            productCategory.Name = model.Name;
            productCategory.Code = model.Code;
           

            bool isSuccess = _productCategoryService.Update(productCategory);
            if (isSuccess)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        return View(model);
    }

    // GET: ProductCategory/Delete/5
    public IActionResult Delete(int? id)
    {
        if (id == null || id <= 0)
        {
            ViewBag.Error = "Please provide valid id.";
            return View();
        }

        var productCategory = _productCategoryService.GetById((int)id);

        if (productCategory == null)
        {
            ViewBag.Error = "Sorry, no product category found for this id.";
            return View();
        }

        var model = new ProductCategoryViewVM()
        {
            ProductCategoryID = productCategory.ProductCategoryID,
            Name = productCategory.Name,
            Code = productCategory.Code,

        };

        return View(model);
    }

    // POST: ProductCategory/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id, IFormCollection collection)
    {
        try
        {
            var productCategory = _productCategoryService.GetById((int)id);

            bool isSuccess = _productCategoryService.Delete(productCategory);

            if (isSuccess)
            {
                return RedirectToAction(nameof(Index));
            }
            return View();

        }
        catch
        {
            return View();
        }
    }




}
