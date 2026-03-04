using CleanCore.Domain.Shared;
using MediatR;

namespace CleanCore.Application.Users.GetUserById;

// Read query — belli bir user'ı `UserDto` olarak döner.
// `[Authorize]` korumasıyla GET /api/v1/users/{id} altında çalışıyor.
//
// Authorization context şu an sade — herhangi bir authenticated user
// herhangi bir user'ı görebiliyor. İleride: kendi profili dışındakini
// göremesin (`UserId == currentUser.UserId`) ya da admin policy.
public sealed record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserDto>>;
