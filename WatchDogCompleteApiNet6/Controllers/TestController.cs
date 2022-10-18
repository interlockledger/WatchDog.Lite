// ******************************************************************************************************************************
//  
// Copyright (c) 2022 InterlockLedger Network
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met
//
// * Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// * Neither the name of the copyright holder nor the names of its
//   contributors may be used to endorse or promote products derived from
//   this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES, LOSS OF USE, DATA, OR PROFITS, OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
// ******************************************************************************************************************************

using InterlockLedger.WatchDog;

using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;

using WatchDogCompleteApiNet6.Models;

namespace WatchDogCompleteApiNet6.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : Controller
{
    [HttpGet("testGet")]
    public Product TestGet(string reference) {
        WatchLogger.Log("...TestGet Started...");
        // Some lines of code
        WatchLogger.Log("...TestGet Ended...");
        return new Product { Id = 1, Name = "Get Test Product", Description = $"This is the response from testGet - {reference}", IsOnSale = true };
    }

    [HttpGet("testGet/{id:int}")]
    public ActionResult<Product> TestGet([FromRoute][Required] int id) {
        WatchLogger.Log($"...TestGet/{id} Called...");
        return ValidateId(id, () => new Product {
            Id = id,
            Name = "Get Numbered Test Product",
            Description = $"This is the response from testGet/{id}",
            IsOnSale = true
        });
    }

    [HttpPost("testPost")]
    public Product TestPost([FromBody] Product product) {
        WatchLogger.Log($"Create new product");
        WatchLogger.Log($"New product created with ID: {product.Id}, Name: {product.Name}, Description: {product.Description}");
        return product;
    }

    [HttpPut("testPut/{id:int}")]
    public ActionResult<string> TestPut([FromRoute][Required] int id, [FromBody] Product product) =>
        ValidateId(id, () => UnimplementedPutProduct(id));

    [HttpPatch("testPatch/{id:int}")]
    public ActionResult TestPatch([FromRoute][Required] int id, string name) =>
        ValidateId(id, () => (ActionResult)Problem(title: "Trying to patch a product is a daring violation...", detail: $"TestPatch on {(id, name)}"));

    [HttpDelete("testDelete/{id:int}")]
    public ActionResult<string> TestDelete([FromRoute][Required] int id) =>
        ValidateId(id, () => WatchLogger.LogReturning($"Product with ID: {id} deleted successfully").Result);

    private ActionResult<T> ValidateId<T>(int id, Func<T> func) =>
        id < 1 ? ProductNotFound(id) : func();
    private ActionResult ValidateId(int id, Func<ActionResult> func) =>
        id < 1 ? ProductNotFound(id) : func();
    private NotFoundObjectResult ProductNotFound(int id) =>
        NotFound($"Product with id '{id}' not found");
    private string UnimplementedPutProduct(int id) =>
        throw new NotImplementedException("Ask yourself, did you implement this?");
}
