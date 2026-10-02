# 03 — Teknik Mimari (Windows + Steam)

## 1. "Steam sunucuları" ne demek, ne sunar?

Valve oyunlar için **ayrılmış (dedicated) oyun sunucusu barındırmaz**. Sunduğu ve bizim kullanacağımız şeyler:

| Steam servisi | Bizim kullanımımız |
|---|---|
| **Steam Matchmaking / Lobbies** | Lobi oluşturma, listeleme, arkadaş daveti, oyuncu meta verisi (hanedan, renk) |
| **Steam Networking Sockets + Steam Datagram Relay (SDR)** | Oyuncular arası trafik Valve'ın relay ağı üzerinden gider. IP adresleri gizli, NAT sorunu yok, ücretsiz |
| Steam Auth (session ticket) | Lobideki herkesin gerçek Steam kullanıcısı olduğunu doğrulama |
| Achievements / Stats / Leaderboards | Başarımlar, ELO benzeri sıralama |
| Workshop / UGC | Topluluk haritaları |
| Steam Cloud | Ayarlar, replay |

Bu, OpenFront'un mimarisine **mükemmel uyuyor**: oyun simülasyonu zaten her istemcide çalışıyor, ağ üzerinden yalnızca küçük "niyet" mesajları gidiyor. Kendi sunucumuza ihtiyaç yok.

## 2. Ağ modeli: Deterministik kilit-adım (lockstep), lobi sahibi = host

```
 Oyuncu A ─┐                      ┌─> A
 Oyuncu B ─┼─ niyet ─> [HOST] ─ Turn(t) ─┼─> B      (hepsi SDR relay üzerinden)
 Oyuncu C ─┘   (Steam lobi sahibi)  └─> C
```

- 1 tick = 100 ms. Her oyuncu eylemini (`Saldır(hedef, oran)`, `İnşaEt(tip, tile)`, `BüyüAt(...)`) **niyet** olarak hosta yollar.
- Host tick'e ait niyetleri sıralayıp **Turn** olarak herkese yollar. Herkes aynı Turn'u aynı sırada uygular → aynı sonuç.
- **Gecikme gizleme**: Turn'ler 2–3 tick ileri planlanır (input delay ~200–300 ms; bu tür oyunda hissedilmez).
- **Senkron kontrolü**: Her 50 tick'te istemciler durum hash'ini hostla karşılaştırır. Farklıysa: desync raporu + host'tan durum anlık görüntüsü.
- **Host göçü**: Host düşerse Steam lobisi yeni sahibi seçer; herkesin elinde tam durum + turn kaydı olduğu için yeni host son onaylı turn'den devam eder.
- **Yeniden bağlanma / geç katılma**: Anlık görüntü (snapshot) + sonraki turn'ler hızlıca oynatılır.
- Ölçek: Steam lobisi 250 üyeye kadar. 100 oyunculuk maçta host trafiği birkaç KB/sn mertebesinde (niyetler çok küçük). Host'un hesap yükü diğerleriyle aynı.
- Hile: Lockstep'te durum hilesi imkânsız (herkes hesaplar, desync olur). Harita bilgisi hileye karşı: Sis Perdesi / gizlilik mekanikleri istemcide çözülür; rekabetçi modda bu sınırlı kalacak, ileride istenirse kendi "otorite relay"imiz eklenebilir.
- Tek oyunculu/AI maçları: aynı kod, host = kendin, ağ yok.

## 3. Determinizm kuralları (simülasyon çekirdeği)

- **Kayan nokta yok**: Tüm hesaplar tam sayı veya sabit noktalı (fixed-point, örn. 1/1000 ölçek). `exp`, `pow` gibi fonksiyonlar tablo/tam sayı yaklaşımıyla.
- **Tohumlu PRNG** (örn. xorshift / PCG), tohum maç başında host'tan.
- Sözlük/hash-set üzerinde döngüde **sıra garantisi** (sıralı yapılar veya ID sırasına göre).
- Çekirdek motor bağımsız saf kod; ayrı iş parçacığında çalışır. Render yalnızca okuma yapar.
- Her mekanik için birim testi + "aynı turn kaydı → aynı hash" replay testi.

## 4. Motor seçimi (öneri)

**Öneri: Godot 4 + C# (.NET 8)**

| Kriter | Godot 4 + C# | Unity | Electron + TypeScript |
|---|---|---|---|
| Lisans/ücret | MIT, ücretsiz | Ücret/koşullar | Ücretsiz |
| Steam | GodotSteam veya Steamworks.NET / Facepunch.Steamworks | Steamworks.NET | steamworks.js |
| 2M tile simülasyon hızı | C# çok iyi | İyi | Orta (web worker) |
| Windows .exe paketleme | Kolay | Kolay | Büyük paket (~150MB+) |
| Shader ile harita | Var | Var | WebGL |

- Simülasyon çekirdeği **ayrı bir .NET kütüphanesi** (`Keladam.Core`): motor bağımlılığı yok, `dotnet test` ile test edilir, gerekirse ileride motordan bağımsız sunucu/bot olarak da çalışır.
- Godot sadece render, UI, ses ve girdi.

## 5. Harita ve render

- **Harita verisi**: PNG'den kendi aracımızla üretilir (renk → arazi tipi: ova, orman, tepe, dağ, bataklık, su, ley hattı, kalıntı). Workshop'a da bu formatla yüklenir.
- **Bellek**: tile başına `ushort sahip` + `byte arazi` + `byte bayrak` (lanet, yanık, sınır). 2000×1000 harita ≈ 8 MB.
- **Render**:
  - Arazi: statik doku (bir kez).
  - Sahiplik: `R16` doku; her tick sadece **değişen tile'lar** GPU'ya yazılır (kısmi güncelleme).
  - Shader: sahip ID → renk paleti; komşu farklıysa sınır çizgisi; lanet/ley için animasyonlu katman.
  - Birimler/kahraman/büyü: sprite katmanı (sayıca az).
  - Yakınlaştırma: kamera; uzaktan ülke isimleri, yakından binalar.

## 6. Proje yapısı (planlanan)

```
/game                 Godot projesi (render, UI, ses, Steam köprüsü)
/src/Keladam.Core     Deterministik simülasyon (C#, motor bağımsız)
/src/Keladam.Net      Lockstep, turn, Steam Networking adaptörü
/src/Keladam.MapTool  PNG → harita dönüştürücü
/tests                Çekirdek testleri + replay determinizm testleri
/docs                 Bu belgeler
/assets               Modeller (PSX Modeler çıktıları dahil)
```

## 7. Yol haritası (kilometre taşları)

1. **M0 — Çekirdek prototip (tek oyunculu)**: Harita yükle, piksel render, sahipsiz araziye yayılma, nüfus/altın, oyuncu vs. basit AI saldırı cephesi. *Amaç: "OpenFront hissi" bizde de var mı?*
2. **M1 — Binalar + deniz**: Kasaba, Sur Kulesi, Liman, çıkarma gemisi, Büyücü Kulesi + Ateş Topu + Obelisk.
3. **M2 — Ağ**: Steam lobisi, lockstep, SDR, desync kontrolü, 8–16 kişilik test maçları.
4. **M3 — Fark yaratan sistemler**: Mana/ley, kahramanlar, ödül panosu, canavar inleri, relikler, hanedanlar.
5. **M4 — Ritüel zaferi, Kara Ay, AI krallıklar, denge.**
6. **M5 — Steam sayfası, başarımlar, Workshop, Early Access.**

Steam notu: Steamworks'e katılım ve uygulama başına **100 $** Steam Direct ücreti gerekir; geliştirme sırasında test için Valve'ın genel test uygulaması (AppID 480, "Spacewar") kullanılabilir.
