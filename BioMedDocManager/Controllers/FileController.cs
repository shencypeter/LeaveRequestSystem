using BioMedDocManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Controllers;

/// <summary>
/// 檔案控制器
/// </summary>
/// <param name="context">資料庫查詢物件</param>
/// <param name="hostingEnvironment">網站環境變數</param>
public class FileController(DocControlContext context, IWebHostEnvironment hostingEnvironment) : BaseController(context, hostingEnvironment)
{

    /// <summary>
    /// 首頁(用不到)
    /// </summary>
    /// <returns></returns>
    public IActionResult Index()
    {
        return NotFound();
    }

    /// <summary>
    /// 取得先前領用過的檔案
    /// </summary>
    /// <param name="IdNo">文件編號</param>
    /// <returns>檔案</returns>
    [HttpGet("File/GetClaimFile/{IdNo}")]
    [Authorize(Roles = "領用人")]
    public async Task<IActionResult> GetClaimFile(string IdNo)
    {

        // 抓登入者資料工號
        string Id = GetLoginUserId();

        var model = await context.DocControlMaintables.FirstOrDefaultAsync(d => d.IdNo == IdNo && d.Id == Id);

        if (model == null)
        {
            return NotFound();
        }

        //回傳文件檔案blob
        return GetDocument(model);

    }

    /// <summary>
    /// 取得先前領用過的檔案
    /// </summary>
    /// <param name="IdNo">文件編號</param>
    /// <returns>檔案</returns>
    [HttpGet("File/GetClaimFileByAdmin/{IdNo}")]
    [Authorize(Roles = "負責人")]
    public async Task<IActionResult> GetClaimFileByAdmin(string IdNo)
    {
        // 不需要登入者資料工號
        var model = await context.DocControlMaintables.FirstOrDefaultAsync(d => d.IdNo == IdNo);

        if (model == null)
        {
            return NotFound();
        }

        //回傳文件檔案blob
        return GetDocument(model);

    }

    /// <summary>
    /// 取得表單檔案
    /// </summary>
    /// <param name="OriginalDocNo">表單編號</param>
    /// <param name="DocVer">版本</param>
    /// <returns></returns>
    [HttpGet("File/GetFormFile/{OriginalDocNo}/{DocVer}")]
    [Authorize(Roles = "領用人")]
    public async Task<IActionResult> GetFormFile(string OriginalDocNo, string DocVer)
    {

        var model = await context.IssueTables.FirstOrDefaultAsync(d => d.OriginalDocNo == OriginalDocNo && d.DocVer == DocVer);

        if (model == null)
        {
            return NotFound();
        }

        //回傳文件檔案blob
        return GetFormFile(model);

    }







}
