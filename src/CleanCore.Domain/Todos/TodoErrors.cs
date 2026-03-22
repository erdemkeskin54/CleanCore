using CleanCore.Domain.Shared;

namespace CleanCore.Domain.Todos;

// Hata kodları tek yerde — handler'lar bu sabitleri referans eder, magic string yok.
// Backend → frontend → UI mesajı zinciri için: kod (`Todo.NotFound`) i18n key gibi davranır.
//
// Ekleme nedeni:
//   - NotFound: id ile sorgulanan todo DB'de yok ya da başka kullanıcıya ait + global query filter sebebiyle görünmüyor.
//   - NotOwner: todo başkasının; ne 404 ne 403, BİZ özellikle 403 dönüyoruz çünkü
//     "todo var ama senin değil" bilgisi olsa enumeration açar (id deneme saldırısı).
//     Aslında daha güvenli yaklaşım NotFound dönmek olurdu — niyet okunabilirliği için
//     burada NotOwner ayrıştırdık. Production'da NotFound'a dönüştürmek de mantıklı.
public static class TodoErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Todo.NotFound", "Bu görev bulunamadı.");

    public static readonly Error NotOwner =
        Error.Forbidden("Todo.NotOwner", "Bu görev sana ait değil.");
}
