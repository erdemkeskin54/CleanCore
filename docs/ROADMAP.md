# CleanCore — Yol Haritası

v1.0 hazır. v1.x sürecinde eklenecek başlıklar aşağıda. Öncelik ihtiyaca göre değişir.

---

## Yol haritası

- [ ] **Role / policy bazlı yetkilendirme** — `[Authorize(Policy = "...")]` örneği + admin/user rol seed'i. Faz 5'ten ertelendi: generic middleware yerine kullanım senaryosuna özgü policy tasarımı uygulanacak.
- [ ] **Domain Events + Outbox pattern** — event'lerin güvenilir teslimi. Kapsam:
    - `AggregateRoot`'a `_domainEvents` koleksiyonu + `RaiseDomainEvent` / `ClearDomainEvents` API'si geri eklenecek
    - `ApplicationDbContext.SaveChangesAsync` override → tracked entity'lerden event'leri toplayıp aynı transaction içinde `outbox_messages` tablosuna yazma (at-least-once teslim için)
    - Background worker (hosted service) outbox'ı polling ile okuyup `IPublisher.Publish(event)` — idempotency için handler'larda `processed_at` takibi
    - DLQ (dead letter queue): retry sayısı aşılan mesajlar için
    - **Şu an niye yok:** Yarım girmiş bir mekanizma (raise var, dispatch yok) kafa karıştırıyordu. Gerçek ihtiyaç (e-posta gönderimi, webhook) doğunca tam implement edilecek.
- [ ] **Background job desteği** — Hangfire entegrasyonu (opsiyonel paket). Outbox worker'ı da burayla birleşebilir.
- [ ] **E-posta servisi** — SMTP şablon + Resend / SendGrid adaptörleri.
- [ ] **Dosya depolama** — `IFileStorage` interface'i + local / S3 / Azure Blob implementasyonları.
- [ ] **OpenTelemetry** — yapılandırılmış trace, Grafana'ya export.
- [ ] **Rate limiting** — .NET 10 dahili rate limiter yapılandırması.
- [ ] **Webhook handler** — Stripe / İyzico için idempotent webhook iskeleti.
- [ ] **SignalR** — gerçek zamanlı bildirim örneği.
- [ ] **Hybrid Cache** (.NET 9+) — IMemoryCache + Redis kombine.
- [ ] **Feature flag desteği** — config bazlı, LaunchDarkly opsiyonel.
- [ ] **Multi-tenant opsiyonu** — TenantId kolon yaklaşımı (schema değil).
- [ ] **i18n** — resource dosyaları + culture middleware.

---

## Yayın planı

- **v0.1** — Faz 1-3 bitmiş → GitHub private
- **v0.5** — Faz 4-5 bitmiş → GitHub public
- **v1.0** — Faz 6 bitmiş ✅ → NuGet.org yayınına hazır (`dotnet pack` çalışıyor, `.nupkg` lokal doğrulandı)
- **v1.x** — Yol haritası başlıkları (role/policy auth + outbox öncelikli)
