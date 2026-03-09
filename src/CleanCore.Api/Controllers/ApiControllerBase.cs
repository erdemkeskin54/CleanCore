using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanCore.Api.Controllers;

// =============================================================================
// ApiControllerBase — tüm controller'ların base'i
// =============================================================================
// Lazy ISender resolve niye?
//   - Tipik kullanım: ctor injection ile `private readonly ISender _mediator` —
//     her controller'da boilerplate satır. 10 controller = 10 ctor.
//   - Bunun yerine base class'ta `Mediator` property'si — child controller'lar
//     extra ctor yazmadan kullanabilir.
//   - HttpContext.RequestServices her zaman elde — DI container scope'u onda.
//
// Niye nullable + cache (`_mediator ??=`)?
//   - İlk kullanımda resolve, sonraki kullanımlarda cache'lenmiş referans.
//   - HttpContext.RequestServices her çağrıda dictionary lookup yapar — küçük ama
//     her endpoint'te tekrar etmesin.
//
// `[ApiController]`:
//   - Otomatik 400 model validation (FluentValidation pipeline'a girmeden önce)
//   - [FromBody] inference for complex types
//   - ProblemDetails default response
// =============================================================================
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}
