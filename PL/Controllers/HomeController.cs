using BLL.Services;
using Microsoft.AspNetCore.Mvc;
using PL.Models;
using System.Diagnostics;

namespace PL.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Trong một Controller nào đó ở tầng PL...
        // Document document = ... (Tài liệu vừa tạo xong)
        // string textNoiDung = ... (Nội dung chữ bạn đọc được từ file PDF/Word)

        //await _documentProcessorService.ProcessAndEmbedDocumentAsync(document.Id, textNoiDung);

    }
}
