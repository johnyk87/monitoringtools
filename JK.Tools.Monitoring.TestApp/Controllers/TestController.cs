namespace JK.Tools.Monitoring.TestApp.Controllers
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using JK.Tools.Monitoring.TestApp.Models;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private static readonly ConcurrentDictionary<Guid, TestModel> Store = new ConcurrentDictionary<Guid, TestModel>();

        [HttpGet]
        public IActionResult Get(int? intFilter = null)
        {
            var enumerable = Store.Values.AsEnumerable();

            if (intFilter.HasValue)
            {
                enumerable = enumerable.Where(value => value.Int == intFilter);
            }

            return this.Ok(enumerable);
        }

        [HttpGet("{id}")]
        public IActionResult Get(Guid id)
        {
            if (!Store.TryGetValue(id, out var value))
            {
                return this.NotFound();
            }

            return this.Ok(value);
        }

        [HttpPost]
        public IActionResult Post([FromBody] TestModel value)
        {
            if (value == null)
            {
                return this.BadRequest();
            }

            if (value.Guid == Guid.Empty)
            {
                value.Guid = Guid.NewGuid();
            }

            if (!Store.TryAdd(value.Guid, value))
            {
                return this.Conflict();
            }

            return this.CreatedAtAction(nameof(this.Get), new { id = value.Guid }, value);
        }

        [HttpPut("{id}")]
        public IActionResult Put(Guid id, [FromBody] TestModel value)
        {
            if (value == null)
            {
                return this.BadRequest();
            }

            if (!Store.TryGetValue(id, out var previousValue))
            {
                return this.NotFound();
            }

            if (!Store.TryUpdate(id, value, previousValue))
            {
                return this.Conflict();
            }

            return this.Ok();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            if (!Store.TryRemove(id, out _))
            {
                return this.NotFound();
            }

            return this.Ok();
        }
    }
}
