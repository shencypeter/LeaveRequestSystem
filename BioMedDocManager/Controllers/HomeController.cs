using System.Diagnostics;
using BioMedDocManager.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Controllers;

/// <summary>
/// 首頁
/// </summary>
/// <param name="logger">log紀錄器</param>
/// <param name="context">資料庫查詢物件</param>
/// <param name="hostingEnvironment">網站環境變數</param>
public class HomeController(ILogger<HomeController> logger, DocControlContext context, IWebHostEnvironment hostingEnvironment) : BaseController(context, hostingEnvironment)
{

    /// <summary>
    /// mockup page
    /// </summary>
    /// <returns></returns>
    public IActionResult PeoplePurchaseDemo()
    {
        return View();
    }

    /// <summary>
    /// 登入後與左上角的入口畫面
    /// </summary>
    /// <returns></returns>
    [Route("[controller]")]
    [Route("/Welcome")]
    [Route("/")]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// 已選系統尚未進入畫面
    /// </summary>
    /// <returns></returns>
    [Route("/Control")]
    [Route("/Purchase")]
    [Route("/Control/Index")]
    [Route("/Purchase/Index")]
    [Authorize(Roles = DocRoleStrings.Anyone + "," + PurchaseRoleStrings.Anyone)]
    public IActionResult SystemIndex()
    {
        var path = HttpContext.Request.Path.Value?.ToLowerInvariant();

        switch (path)
        {
            case "/control/index":
                ViewData["Title"] = "文件管理系統";
                TempData["Menu"] = "Document";
                break;
            case "/purchase/index":
                ViewData["Title"] = "電子採購系統";
                TempData["Menu"] = "Purchase";
                break;
        }

        return View();
    }

}
