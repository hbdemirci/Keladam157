# 02 — Oyun Tasarım Belgesi (GDD)

Çalışma adı: **Keladam** (değişebilir)
Tür: Gerçek zamanlı, piksel haritalı, büyülü orta çağ toprak savaşı · Windows · Steam · 2–100 oyuncu

## 1. Vizyon

OpenFront'un "her piksel bir toprak, cephe organik ilerler" hissini koruyoruz; üstüne **kahraman birimler, büyü, canavarlar, ödül avı ve ritüel zaferi** ekleyerek oyunu yalnızca "kim daha çok asker biriktirir" olmaktan çıkarıp **kararlı, hikâyeli, karşı-hamleli** bir oyuna dönüştürüyoruz.

Üç tasarım ilkesi:
1. **Haritanın kendisi hâlâ ana karakter.** Toprak = güç. Cephe soyut ordu sayısıyla ilerler (performans + okunabilirlik).
2. **Az sayıda ama önemli birim.** Haritada dolaşan birkaç kahraman/canavar; her biri bir karar.
3. **Her güçlü hamlenin bir cevabı var.** Meteor ↔ Kalkan Obeliski, Suikastçi ↔ Muhafız, Ritüel ↔ Kesintiye uğratma.

## 2. OpenFront → Keladam eşleştirmesi

| OpenFront | Keladam | Fark |
|---|---|---|
| Asker / işçi | **Köylü** (altın + büyüme) / **Asker** (savaş) | Aynı mantık, kaydırıcıyla oran |
| Altın | **Altın** | + **Mana** (ikinci kaynak, büyüler için) |
| City | **Kasaba → Kale → Hisar** | Nüfus tavanı; Hisar seviyesi kahraman kapasitesi açar |
| Port | **Liman** | Kadırga, tüccar gemileri |
| Factory + Tren | **Pazar Yeri + Kervan Yolu** | Kervanlar yol boyunca altın taşır; **haydutlar/ödül avcıları yağmalayabilir** |
| Defense Post | **Sur Kulesi** | Savunma + ok atar |
| Missile Silo | **Büyücü Kulesi** | Büyü fırlatma noktası |
| SAM | **Koruma Obeliski** | Gelen büyüleri bozar (karşı-büyü) |
| Atom / H-Bomba / MIRV | **Ateş Topu / Kıyamet Meteoru / Meteor Yağmuru** | + destructive olmayan büyüler (aşağıda) |
| Fallout | **Lanetli Toprak** | Mor, solgun; savunma ve büyüme cezası. **Tapınak** ile arındırılabilir |
| Transport Ship | **Çıkarma Gemisi** + **Işınlanma Çemberi** | Kara üzerinden de (mana ile) ani çıkarma |
| Warship | **Savaş Kadırgası** | + denizde **Kraken** (nötr canavar) |
| Bot kabileler | **Canavar inleri, goblin kampları, haydut çeteleri** | PvE, ganimet ve eşya düşürür |
| Nation AI | **Krallıklar (AI)** | Hanedan kişilikli |
| %80 toprak zaferi | %80 toprak **veya** **Yükseliş Ritüeli** | Alternatif zafer |

## 3. Kaynaklar

- **Altın**: Köylülerden akış, pazarlar, kervanlar, ganimet, ödüller.
- **Mana**: Haritadaki **Ley hatları** (parlayan çizgiler) ve **Mana Kuyuları** üzerindeki tile'lardan gelir. Büyük ülke ≠ çok mana; **doğru yeri** tutmak önemli. Bu sayede küçük oyuncular stratejik noktayla öne çıkabilir.
- **Nüfus**: Köylü/asker. Büyüme eğrisi OpenFront'takine benzer lojistik eğri (değerleri biz ayarlayacağız).
- **Moral** (opsiyonel, faz 2): Yenilgiler, lanet ve açlık düşürür; Tapınak, zafer ve şölen yükseltir. Saldırı hızını etkiler.

## 4. Arazi

| Arazi | Saldırı maliyeti | Özellik |
|---|---|---|
| Ova | düşük | Hızlı genişleme |
| **Orman** | orta | Savunana pusu bonusu; **yakılabilir** (Ateş büyüsü) → ova olur |
| Tepe | yüksek | Kule menzili + |
| Dağ | çok yüksek | Neredeyse doğal sur; maden (altın +) |
| **Bataklık** | orta, çok yavaş | Hastalık riski |
| **Ley hattı** | normal | Mana üretir |
| **Kadim Kalıntı** | normal | Ele geçirene **Relik** verir |
| Lanetli toprak | savunma − | Büyü sonrası oluşur |
| Su | — | Gemi/ışınlanma ile geçilir |

## 5. Hanedanlar (oyun başında seçilir)

Asimetri ama sade: her hanedanın 1 pasif + 1 özel büyü/birimi var.

| Hanedan | Pasif | Özel |
|---|---|---|
| **Kızıl Ateş Ordosu** (ateş büyücüleri) | Ateş büyüleri −%20 mana | *Ejder Nefesi*: uzun bir şeritte yakar |
| **Demir Taç** (şövalyeler) | Sur kuleleri +%25, ova saldırısı hızlı | *Şövalye Hücumu*: 10 sn cephe hızı ×2 |
| **Yeşil Koru** (orman halkı, elfler) | Ormanda savunma ++, orman yayılır | *Kök Duvarı*: sınırda geçilmez çit |
| **Gölge Loncası** (suikastçi/ödül avcıları) | Ödül ödülleri +%50, kervan yağması | *Sis Perdesi*: bölgeni diğerlerinden gizler |
| **Kemik Ayini** (nekromanlar) | Ölen askerlerin %10'u geri döner | *Ölüleri Kaldır*: lanetli toprakta asker doğar |

## 6. Kahramanlar ve özel birimler (ana fark)

Haritada **gerçekten yürüyen**, sayıca sınırlı birimler (Hisar seviyesine göre 1–5 kahraman). Ölebilir, seviye atlar, eşya taşır.

- **Büyücü**: Kulesiz, kısa menzilli büyü atar (ateş topu, buz). Kırılgan.
- **Savaş Komutanı**: Etrafındaki cepheye **hız + kayıp azaltma** aurası. Cepheyi "yönlendirmenin" yolu.
- **Ödül Avcısı**: Düşman kahramanlarını avlar, kervan/tüccar yağmalar, binaya sabotaj yapar. Ödül panosundaki görevleri alır.
- **Rahip / Şifacı**: Lanetli toprağı arındırır, moral verir, büyüleri hafifletir.
- **Muhafız** (pasif savunma kahramanı): Bölgendeki suikastçileri yakalar.
- **Ejderha** (geç oyun, çok pahalı, tek): Uçar, alan yakar, Koruma Obeliski ve okçu kuleleri tarafından vurulabilir.

Kahramanlar **cepheyi değiştirmez, cepheyi etkiler**. Toprak hâlâ ordu sayısıyla kazanılır — bu, OpenFront'un okunabilirliğini korur.

## 7. Büyüler (Büyücü Kulesi / Büyücü kahraman)

| Büyü | Etki | Karşılık |
|---|---|---|
| Ateş Topu | Küçük alan, bina yıkar, ormanı yakar | Obelisk |
| Kıyamet Meteoru | Dev alan, lanetli toprak | Obelisk (seviyeli) |
| Meteor Yağmuru | Çok parçalı, oyun sonu | Çok obelisk |
| **Veba** | Hedef bölgede nüfus büyümesi durur, komşulara yayılabilir | Rahip, Tapınak |
| **Buz Mührü** | Bir sınır parçası 20 sn dondurulur (saldırı yok, giriş yok) | Ateş büyüsü eritir |
| **Işınlanma** | Askerleri uzak bir noktaya "çıkarma" | Obelisk menzili engeller |
| **Kutsama** | Bölgende savunma + | Dağıtma büyüsü |
| **Kehanet** | Bir oyuncunun ordu/altın/kahramanlarını gösterir | Sis Perdesi |

## 8. Ödül (Bounty) sistemi — sosyal katman

- **Ödül Panosu**: Herhangi bir oyuncu altın koyarak başka bir oyuncuya/kahramana/binaya ödül koyar ("Kızıl Ordo'nun kulesine 200k").
- Görevi yerine getiren (ödül avcısı kahramanı olan herkes, ya da AI **Gölge Lonca** paralıları) ödülü alır.
- Sonuç: Zayıf oyuncular güçlüye karşı **ekonomik birlik** kurabilir, ittifaka gerek kalmadan. "Kral avı" doğal olarak oluşur, liderin kartopu olması zorlaşır.

## 9. PvE: Canavarlar ve Relikler

- Haritada başlangıçtan **canavar inleri** (troll mağarası, goblin kampı, ejderha yuvası). Bunlar OpenFront'taki zayıf bot kabilelerin yerini alır ama daha anlamlıdır: temizlenince **altın + eşya + relik**.
- **Kadim Kalıntılar**: tutana global bonus veren 5–7 relik (örn. *Ejder Kalbi*: büyüler −%15 mana).
- **Kara Ay olayı** (OpenFront'taki kıyamet saati benzeri): ortalarda bir kez, canavarlar haritada dalga hâlinde saldırır; uzun oyunları çözer.

## 10. Zafer koşulları

1. **Fetih**: Kara topraklarının %80'i (süre uzadıkça eşik düşer).
2. **Yükseliş Ritüeli**: 3 relik tutan oyuncu Hisar'da ritüel başlatır (ör. 3 dk). Herkes görür ve ritüel noktası tüm haritaya ifşa edilir. Ritüel alanı kaybedilirse/Hisar yıkılırsa iptal. → Oyun sonuna dramatik bir "herkes ona karşı" anı.
3. Takım modunda: takım toprak payı.

## 11. Diplomasi

OpenFront'taki ittifak (süreli), ihanet cezası, ambargo, bağış, emoji ve hızlı mesaj korunur. Eklemeler:
- **Haraç anlaşması**: "Bana dakikada X altın ver, saldırmayayım."
- **Paralı kahraman kiralama**: Kahramanını süreli olarak müttefikine verebilirsin.

## 12. Oyun akışı (tipik 20–30 dk maç)

1. **Doğuş (0–30 sn)**: Haritada başlangıç yerini seç (Ley hattı mı, dağ eteği mi?).
2. **Erken (0–5 dk)**: Sahipsiz araziye yayılma, canavar inlerinin temizlenmesi, ilk Kasaba/Pazar.
3. **Orta (5–15 dk)**: Komşu savaşları, ilk kahramanlar, Büyücü Kulesi, kervan yolları, ödüller.
4. **Geç (15+ dk)**: Meteorlar, Ejderha, relik yarışı, Kara Ay, Ritüel veya fetih.

## 13. Görsel ve ses yönü

- Harita: Piksel; parşömen/eski harita renk paleti, sınırlar mürekkep çizgisi gibi. Krallık renkleri armalarla.
- Birimler: Haritada küçük piksel sprite'lar; yakınlaşınca ayrıntılı. Mevcut **PSX Modeler** (bu repo) ile düşük poligon büyücü/savaşçı modelleri **menü, kahraman portresi ve kahraman seçme ekranı** için kullanılabilir; ileride 3D sprite render'ı alınabilir.
- Efektler: Ley hatları yavaşça nabız atar, lanetli toprak mor sis, meteor ekran sarsıntısı.
- Müzik: Orta çağ enstrümanları; savaş yoğunluğuna göre katmanlı dinamik müzik.

## 14. Steam özellikleri

- Lobi (arkadaş daveti, herkese açık lobi listesi), Rich Presence ("Kızıl Ordo olarak 34 oyunculu maçta").
- Başarımlar (Steam Achievements), istatistik ve liderlik tabloları.
- **Steam Workshop**: Topluluk haritaları (PNG → harita aracı). Uzun ömür için çok değerli.
- Steam Cloud: ayarlar, kozmetikler, replay'ler.
- Kozmetik (opsiyonel): arma, bayrak, sınır deseni — oynanışa etkisiz.
