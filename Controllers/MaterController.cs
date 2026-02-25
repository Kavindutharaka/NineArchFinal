using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using NineArchTours.Models;

namespace NineArchTours.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _dbcon;

        public MaterController(IConfiguration configuration)
        {
            _configuration = configuration;
            _dbcon = _configuration.GetSection("DBCon").Value ?? "";
        }

        // GET data via stored procedure / query
        [HttpPost("sp")]
        public ActionResult Sp([FromBody] Mater udata)
        {
            var tb = new DataTable();
            using (var myCon = new SqlConnection(_dbcon))
            {
                myCon.Open();
                using (var myCom = new SqlCommand(udata.SysID, myCon))
                {
                    myCom.CommandTimeout = 60;
                    using (var myR = myCom.ExecuteReader())
                    {
                        tb.Load(myR);
                    }
                }
            }
            return new OkObjectResult(tb);
        }

        // INSERT/UPDATE/DELETE via stored procedure / query
        [HttpPost("spd")]
        public ActionResult Spd([FromBody] Mater udata)
        {
            var tb = new DataTable();
            using (var myCon = new SqlConnection(_dbcon))
            {
                myCon.Open();
                using (var myCom = new SqlCommand(udata.SysID, myCon))
                {
                    myCom.CommandTimeout = 60;
                    using (var myR = myCom.ExecuteReader())
                    {
                        tb.Load(myR);
                    }
                }
            }
            return new OkObjectResult(tb);
        }
    }
}
