using Microsoft.AspNetCore.Mvc;

using System;
using System.ComponentModel.DataAnnotations;

using WatchDogCompleteTestAPI.Models;

namespace WatchDogCompleteTestAPI.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class TestController : Controller {
        [HttpGet("testGet")]
        public Product TestGet(string reference) {
            return new Product { Id = 1, Name = "Get Test Product", Description = $"This is the response from testGet - {reference}", IsOnSale = true };
        }

        [HttpGet("testGetthrow")]
        public Product TestGetThrow(string reference) {
            throw new Exception("O get o, then forget");
        }

        [HttpPost("testPost")]
        public Product TestPost([FromBody] Product product) {
            return product;
        }

        [HttpPut("testPut")]
        public string TestPut(Product product) {
            throw new NotImplementedException("Ask yourself, did you implement this?");
        }

        [HttpPatch("testPatch")]
        public JsonResult TestPatch([Required] int id, string name) {
            throw new AccessViolationException("That one there was a violation, personally i wouldn't have it");
        }

        [HttpDelete("testDelete")]
        public string TestDelete(int id) {
            return $"Product with ID: {id} deleted successfully";
        }
    }
}
