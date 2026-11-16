# BROZILLA

**BROZILLA**, Windows için geliştirilmiş, web tabanlı olmayan, ultra optimize **C# / WPF (.NET 9)** tabanlı bir **GoodbyeDPI-Turkey** masaüstü arayüzüdür.

## Özellikler
- **Native .NET 9 WPF Arayüz:** Sıfır Chromium/Electron yükü, donanım hızlandırmalı 3D Claymorphism tasarım.
- **Animasyonlu 3D Güç Kalkanı:** Tek tıkla koruma başlatma/durdurma, enerji dalga halkaları ve tepsiye küçültüldüğünde %0 CPU modu.
- **7 Hazır Türkiye İSS Profili:** Standart (Türk Telekom, TurkNet, Kablonet) ve Superonline / Vodafone Alternatif 1–6 profilleri.
- **Sistem Tepsisi & Otomatik Başlatma:** Çarpıya basıldığında saatin yanına küçülür, son profili hatırlar.
- **Bağlantı Testi:** Uygulama içinden tek tıkla erişim doğrulaması.

## Derleme
```bash
dotnet publish BROZILLA.csproj -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true -o ./dist/BROZILLA
```

## Lisans
Bu proje [MIT Lisansı](LICENSE) altında açık kaynaklıdır. İçerdiği GoodbyeDPI ve WinDivert bileşenleri kendi açık kaynak lisanslarına tabidir.
