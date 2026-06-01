using FastData;
using FastData.Config;
using FastData.Demo.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace FastData.Demo.Controllers
{
    [ApiController]
    [Route("api/FeatureDemo")]
    public class FeatureDemoController : ControllerBase
    {
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "FeatureDemo works!" });
        }

        [HttpGet("config")]
        public IActionResult Config()
        {
            return Ok(new
            {
                softDelete = new {
                    enabled = FastDataOptions.SoftDelete.Enabled,
                    propertyName = FastDataOptions.SoftDelete.PropertyName
                },
                audit = new {
                    enabled = FastDataOptions.Audit.Enabled
                }
            });
        }
    }
}
