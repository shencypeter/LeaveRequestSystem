using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Controllers
{

    /// <summary>
    /// 檔案控制器
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>
    
    public class FileController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IAccessLogService _accessLog, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {

        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "檔案";

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
        public async Task<IActionResult> GetClaimFile(string IdNo)
        {

            // 抓登入者資料工號
            User? LoginUser = GetLoginUser();
            if (LoginUser == null)
            {
                return NotFound();
            }

            var model = await _context.DocControlMaintables.FirstOrDefaultAsync(d => d.IdNo == IdNo && d.Id == LoginUser.UserAccount);// 因為DocControlMaintables的Id是工號不是id

            if (model == null)
            {
                return NotFound();
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "檔案下載-取得先前領用過的檔案");

            //回傳文件檔案blob
            return GetDocument(model);

        }

        /// <summary>
        /// 取得先前領用過的檔案
        /// </summary>
        /// <param name="IdNo">文件編號</param>
        /// <returns>檔案</returns>
        public async Task<IActionResult> GetClaimFileByAdmin(string IdNo)
        {
            // 不需要登入者資料工號
            var model = await _context.DocControlMaintables.FirstOrDefaultAsync(d => d.IdNo == IdNo);

            if (model == null)
            {
                return NotFound();
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "檔案下載-取得先前領用過的檔案(負責人)");

            //回傳文件檔案blob
            return GetDocument(model);

        }

    }
}
