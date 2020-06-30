using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileRepositirySimple.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private static ConcurrentDictionary<string, ActionFile> files = new ConcurrentDictionary<string, ActionFile>();

        public FilesController(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet]
        public ActionResult Get(string filename)
        {
            ActionFile value;
            if (files.TryGetValue(filename, out value))
            {
                value.WaitingCount++;

                // Жду отработки задачи
                var t2 = value.Task.ContinueWith((i) => {
                    ;
                    value.WaitingCount--;

                    return i.Result;
                });
                t2.Wait();

                if (value.WaitingCount == 0 || t2.Result == null)
                    files.TryRemove(filename, out ActionFile removeFile);

                if (t2.Result == null)
                    return NotFound("Файл не найден.");

                return Ok(t2.Result);
            }
            else
            {
                Task<string> task = new Task<string>(() => LoadFile(filename));

                ActionFile res = new ActionFile{ WaitingCount = 1, Task = task };
                files.TryAdd(filename, res);
                res.Task.Start();

                // Жду отработки задачи
                var t2 = task.ContinueWith((i) => {
                    res.WaitingCount--;

                    return i.Result;
                });
                t2.Wait();

                if (res.WaitingCount == 0 || t2.Result == null)
                    files.TryRemove(filename, out ActionFile removeFile);

                if (t2.Result == null)
                    return NotFound("Файл не найден.");

                return Ok(t2.Result);
            }
        }

        string LoadFile(string filename)
        {
            ;
            string textFile = Path.Combine(_hostingEnvironment.ContentRootPath, "App_Data", filename);
            if (!System.IO.File.Exists(textFile))
                return null;   //throw new Exception("Файл не найден.");

            Thread.Sleep(20000);

            string text = System.IO.File.ReadAllText(textFile);
            return text;
        }

        class ActionFile
        {
            public int WaitingCount { get; set; }
            public Task<string> Task { get; set; }
        }
    }
}
