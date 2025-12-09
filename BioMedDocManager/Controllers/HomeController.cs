using BioMedDocManager.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BioMedDocManager.Controllers;

/// <summary>
/// 首頁
/// </summary>
/// <param name="logger">log紀錄器</param>
/// <param name="context">資料庫查詢物件</param>
/// <param name="hostingEnvironment">網站環境變數</param>
[Route("[controller]")]
public class HomeController(ILogger<HomeController> logger, DocControlContext context, IWebHostEnvironment hostingEnvironment) : BaseController(context, hostingEnvironment)
{
    /// <summary>
    /// 頁面名稱
    /// </summary>
    public const string PageName = "首頁";

    // mockup page：用 /Home/PeoplePurchaseDemo
    [HttpGet("PeoplePurchaseDemo")]
    public IActionResult PeoplePurchaseDemo()
    {
        return View();
    }

    /// <summary>
    /// 登入後與左上角的入口畫面（首頁）
    /// </summary>
    [HttpGet("")]           // /Home
    [HttpGet("Index")]      // /Home/Index
    [HttpGet("/")]          // 根路徑 /
    [HttpGet("/Welcome")]   // /Welcome
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// 已選系統尚未進入畫面
    /// </summary>
    [HttpGet("/Control")]
    [HttpGet("/Purchase")]
    [HttpGet("/Control/Index")]
    [HttpGet("/Purchase/Index")]
    [Authorize(Roles = AppSettings.CombinedRoles.DocAndPurchase)]
    public IActionResult SystemIndex()
    {
        var path = HttpContext.Request.Path.Value?.ToLowerInvariant();

        switch (path)
        {
            case "/control/index":
            case "/control":
                ViewData["Title"] = "文件管理系統";
                TempData["Menu"] = "Document";
                break;
            case "/purchase/index":
            case "/purchase":
                ViewData["Title"] = "電子採購系統";
                TempData["Menu"] = "Purchase";
                break;
        }

        return View();
    }
}
