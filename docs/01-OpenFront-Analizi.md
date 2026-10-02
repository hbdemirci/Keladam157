# 01 — OpenFront.io Analizi

> Kaynak: OpenFront'un açık kaynak kodu (github.com/openfrontio/OpenFrontIO) ve topluluk rehberleri incelendi.
> Bu belge **mekanikleri anlamak** içindir. Kod, harita ve görsel kopyalamıyoruz (bkz. §7 Lisans).

## 1. Oyunun özü

- Gerçek zamanlı, çok oyunculu (FFA / takım) **toprak kapma** oyunu.
- Harita = dev bir piksel ızgarası (örn. 2000×1000 ≈ 2 milyon kare). **Her piksel bir "tile"**, her tile'ın bir sahibi var.
- Kara birliği diye hareket eden asker yok: ordu **soyut bir sayı** (asker sayısı). Saldırı, sınır boyunca tile tile ilerleyen bir **cephe**dir.
- Kazanma: kara tile'larının **%80'ine** sahip olmak (süre uzadıkça eşik düşer – "overtime").
- Oyun ~10 tick/saniye (1 tick = 100 ms) çalışır.

## 2. Ekonomi

| Kaynak | Nasıl artar | Ne işe yarar |
|---|---|---|
| **Nüfus/Asker** | Lojistik büyüme: `ekle = (10 + asker^0.73 / 4) × (1 − asker/maks)` | Saldırı, savunma |
| **Maks. nüfus** | `2 × (tile^0.6 × 1000 + 50 000) + şehir seviyeleri × bonus` | Büyüme tavanı |
| **Altın** | Sabit akış (~100/tick) + ticaret gemileri + tren | Bina, gemi, nükleer |

- Büyüme en hızlı ~%40-50 doluluktayken → oyuncu ordusunu sürekli "ideal doluluk"ta tutmaya çalışır. Bu oyunun ana ritmini oluşturur.
- **Saldırı oranı kaydırıcısı**: ordunun yüzde kaçının saldırıya gideceğini seçersin.

## 3. Savaş (en önemli sistem)

Saldırı her tick'te bir "zaman bütçesi" harcayarak öncelik kuyruğundan tile alır:

1. Hedef tile seçimi: sınırdaki düşman tile'ları **öncelik kuyruğu**nda; çevresinde senin tile'ın çok olanlar (girintiler) ve düz arazi önce alınır, + biraz rastgelelik → cephe organik, dalgalı ilerler.
2. Her tile için: **saldıran kaybı**, **savunan kaybı** ve **zaman maliyeti** hesaplanır.
3. Etkenler:
   - **Arazi**: Ova < Tepe < Dağ (dağ hem pahalı hem yavaş).
   - **Güç oranı**: savunan ordu / saldıran ordu → kayıp ve hız buna göre.
   - **Yoğunluk**: savunanın asker/tile oranı; kalabalık topraklar pahalı.
   - **Savunma kulesi** (Defense Post): yakınındaki tile'larda kayıp ve süre artar.
   - **Büyük ülke bonusu**: devler daha ucuz saldırır ki oyun sonu tıkanmasın.
   - **Hain** (ittifak bozan): savunması zayıflar.
   - **Sahipsiz arazi** (oyun başı genişleme): ucuz ve hızlı.
4. Oyuncu <100 tile'a düşünce yok edilir, kalan toprakları ve altını fatihe geçer.

## 4. Binalar ve birimler

| OpenFront | İşlevi |
|---|---|
| City | Nüfus tavanı +; seviye atlatılabilir |
| Port | Gemi üretimi, ticaret gemisi altını |
| Factory + Tren | Raylarla şehirler arası altın |
| Defense Post | Çevrede savunma bonusu, ateş eder |
| Missile Silo | Nükleer fırlatma |
| SAM Launcher | Füze önleme, menzil seviyeyle artar |
| Transport Ship | Denizden çıkarma (ordunun 1/5'i) |
| Warship | Devriye, gemi avlar, deneyim kazanır |
| Atom / Hidrojen / MIRV | Alan hasarı, kalıcı "fallout" (savunmayı bozar) |

Bina maliyetleri sayıyla katlanarak artar (2^n), bu da yayılmayı dengeler.

## 5. Diplomasi ve sosyal

- İttifak isteği (süreli), uzatma, ihanet → "Traitor" cezası.
- Ambargo (ticaret kesme), altın/asker bağışı, emoji ve hazır mesajlar, "hedef göster".
- Botlar (zayıf yerliler/kabileler) ve **Nation** yapay zekâları (zorluk seviyeli).

## 6. Teknik mimari

- **Deterministik kilit-adım (lockstep)** simülasyon: oyunun tamamı **her istemcide** çalışır.
- Sunucu oyunu hesaplamaz; sadece oyuncu **niyetlerini (intent)** toplar, her tick bir **Turn** olarak herkese dağıtır.
- Simülasyon: tohumlu PRNG, kayan nokta yok → herkes aynı sonucu üretir. Ağ trafiği çok küçük.
- Yeniden bağlanma / replay: turn kaydını baştan oynatarak.
- Harita: PNG'nin mavi kanalından arazi tipi + yükseklik → ikili `map.bin`.
- Görüntü: WebGL (Pixi.js), tile sahipliği bir dokuya yazılır, sınırlar shader ile çizilir.
- Dikkat: **OpenFront'un kendisi de artık Steam'de** (kodunda Steam SDK entegrasyonu var). Yani doğrudan rakibimiz; sadece yeniden tema yetmez, oynanış gerçekten farklı olmalı.

## 7. Lisans — çok önemli

- OpenFront kodu **AGPL-3.0**: kodunu alırsak tüm oyunumuzun kaynak kodunu açmak zorunda kalırız. Steam'de kapalı/ticari bir oyun için uygun değil.
- Görseller **CC BY-SA 4.0**, bir kısmı tescilli (`proprietary/`).
- **Kararımız: Sıfırdan kendi kodumuzu yazıyoruz.** Oyun mekaniği fikirleri (toprak kapma, nüfus büyümesi vb.) telif konusu değildir; kod, harita dosyaları, isimler ve görseller ise öyledir. Formülleri de kendimiz ayarlayacağız.
