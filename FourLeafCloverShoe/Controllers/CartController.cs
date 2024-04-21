﻿using FourLeafCloverShoe.Helper;
using FourLeafCloverShoe.IServices;
using FourLeafCloverShoe.Share.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Security.Claims;

namespace FourLeafCloverShoe.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ICartItemItemService _cartItemItemService;
        private readonly IProductDetailService _productDetailService;
        private readonly IProductService _productService;
        private readonly UserManager<User> _userManager;

        public CartController(ICartService cartService, IProductService productService, ICartItemItemService cartItemItemService, IProductDetailService productDetailService, UserManager<User> userManager)
        {
            _cartService = cartService;
            _cartItemItemService = cartItemItemService;
            _productDetailService = productDetailService;
            _productService = productService;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                var cartItems = await _cartItemItemService.GetsByUserId(user.Id);
                return View(cartItems);
            }
            return View();
        }
        public async Task<IActionResult> addToCart(int quantity, Guid productDetailId)
        {
            var productDetailFromDb = await _productDetailService.GetById(productDetailId);
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                var cartItems = await _cartItemItemService.GetsByUserId(user.Id);
                var itemInCart = cartItems.Where(c => c.ProductDetailId == productDetailId);
                if (quantity <= 0) // số lượng phải hợp lệ
                {
                    return Json(new { message = "Số lượng sản phẩm phải lớn hơn 0!", isSuccess = false });

                }
                else if (productDetailId == Guid.Parse("00000000-0000-0000-0000-000000000000")) // sản phẩm chi tiết phải hợp lệ
                {
                    return Json(new { message = "Vui lòng chọn kích cỡ hợp lệ!", isSuccess = false });
                }
                else if (productDetailFromDb.Status == 0) // sản phẩm chi tiết phải hợp lệ về status
                {
                    return Json(new { message = "Sản phẩm ngừng bán!", isSuccess = false });
                }
                else if (productDetailFromDb.Quantity <= 0) // sản phẩm chi tiết phải hợp lệ về số lượng
                {
                    return Json(new { message = "Sản phẩm đã hết hàng!", isSuccess = false }); // kho hết hàng
                }
                else if (productDetailFromDb.Quantity < quantity)
                {
                    return Json(new { message = "Kho không đủ số lượng yêu cầu!", isSuccess = false }); // kho hết hàng
                }
               
                else
                {
                    if (productDetailFromDb.Status==1&& productDetailFromDb.Products.Status==true)
                    {
                        if (itemInCart.Count() < 1)
                        {
                            var item = new CartItem()
                            {
                                CartId = (await _cartService.GetByUserId(user.Id)).Id,
                                ProductDetailId = productDetailId,
                                Quantity = quantity,
                                Price = productDetailFromDb.PriceSale
                            };
                            var result = await _cartItemItemService.Add(item);
                            if (result)
                            {
                                return Json(new { message = "Thêm sản phẩm thành công!", isSuccess = true }); // Done

                            }
                            return Json(new { message = "Lỗi thêm mới sản phẩm!", isSuccess = false });
                        }
                        else
                        {
                            var productInCart = cartItems.FirstOrDefault(c => c.ProductDetailId == productDetailId);
                            if (productDetailFromDb.Quantity < quantity + productInCart.Quantity)
                            {
                                return Json(new { message = "Số lượng sản phẩm trong giỏ hàng và số lượng muốn thêm vào vượt quá số lượng tồn kho!", isSuccess = false }); // tổng hàng vượt quá kho
                            }
                            else
                            {
                                productInCart.Quantity += quantity;
                                var result = await _cartItemItemService.Update(productInCart);
                                if (result)
                                {

                                    return Json(new { message = "Thêm sản phẩm thành công!", isSuccess = true }); // Done

                                }
                                return Json(new { message = "Lỗi update giỏ hàng!", isSuccess = false }); // Done
                            }
                        }
                    }
                    return Json(new { message = "Sản phẩm ngừng kinh doanh!", isSuccess = false }); // Done
                }

            }

            return Json(new { message = "Vui lòng đăng nhập!", isSuccess = false });
        }
        public async Task<IActionResult> getMiniCart()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                var cartItems = await _cartItemItemService.GetsByUserId(user.Id);
                var lstproductdetail = await _productDetailService.Gets();
                var lstproductdetails = lstproductdetail.Where(c => cartItems.Any(p => p.ProductDetailId == c.Id));
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                var serializedCartItems = JsonConvert.SerializeObject(lstproductdetails, settings);
                var tongtien = lstproductdetails.Sum(productDetail =>
cartItems.FirstOrDefault(c => c.ProductDetailId == productDetail.Id)?.Quantity * productDetail.PriceSale ?? 0).ToString("N0");
                return Json(new { lstproducts = serializedCartItems, tongtien = tongtien });
            }
            return Json(new { lstproducts = new List<CartItem>(), tongtien = 0 });
        }
        public async Task<IActionResult> removeMiniCartItem(Guid productDetailId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var result = await _cartItemItemService.DeleteByProductDetailId(productDetailId);
                if (result)
                {
                    return Json(new { message = "Xoá thành công!", isSuccess = true });
                }

                return Json(new { message = "Có lỗi xảy ra!", isSuccess = false });
            }
            return Json(new { message = "Chưa đăng nhập!", isSuccess = false });

        }
        public async Task<IActionResult> removeCartItem(Guid productDetailId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var result = await _cartItemItemService.DeleteByProductDetailId(productDetailId);
                if (result)
                {
                    return Json(new { message = "Xoá thành công!", isSuccess = true });
                }

                return Json(new { message = "Có lỗi xảy ra!", isSuccess = false });
            }
            return Json(new { message = "Chưa đăng nhập!", isSuccess = false });

        }

        public async Task<IActionResult> getCountCart()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                var cartItems = await _cartItemItemService.GetsByUserId(user.Id);
                var count = cartItems != null ? cartItems.Count() : 0;
                return Json(new { message = "Lấy thành công!", isSuccess = true , count = count });
            }
            return Json(new { message = "Chưa đăng nhập!", isSuccess = false,count=0 });

        }
         public async Task<IActionResult> UpdateSLInCart()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                var cartItems = await _cartItemItemService.GetsByUserId(user.Id);
                var count = cartItems != null ? cartItems.Count() : 0;
                return Json(new { message = "Lấy thành công!", isSuccess = true , count = count });
            }
            return Json(new { message = "Chưa đăng nhập!", isSuccess = false,count=0 });

        }
        [HttpPost]
        public async Task<JsonResult> UpdateSLInCart(Guid idProductDetail, int newQuantity)
        {
            ViewBag.message = "xinnchao";
            if (User.Identity.IsAuthenticated) // đã đăng nhập
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                var lstCartItem = await _cartItemItemService.GetsByUserId(user.Id);
                var cartItem = lstCartItem.FirstOrDefault(c => c.ProductDetailId == idProductDetail); // cartitemid
                var ProductDetail = await _productDetailService.GetById(idProductDetail);
                if (ProductDetail.Quantity < newQuantity)
                {
                    return Json(new { message = "Sản phẩm vượt quá giới hạn trong kho",oldQuantity = cartItem.Quantity, status = false });
                }
                if (newQuantity <= 0)
                {
                    return Json(new { message = "Sản phẩm tối thiểu là 1", oldQuantity = cartItem.Quantity, status = false });
                }
                var Response = await _cartItemItemService.UpdateQuantity(cartItem.Id, newQuantity);
                if (Response)
                {
                    return Json(new { message = "OK", status = true });
                }
                return Json(new { message = "Lỗi không xác định", oldQuantity = cartItem.Quantity, status = false });
            }
            else//chưa đăng nhập
            {

                return Json(new { message = "OK", status = true });
            }

        }


    }
}
