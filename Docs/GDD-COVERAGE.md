# Pemetaan revisi terhadap GDD v2

| Bagian | Implementasi | Batas / interpretasi |
|---|---|---|
| Explore → collect → knead → bake → serve | Desa dan interior terpisah, graf jalur, bahan, pilihan resep, 3 ulen, fermentasi, oven, pelayanan | Satu batch aktif; pelayanan warga umum diwakili etalase, tanpa simulasi antrean NPC |
| Hujan | Panen herba meningkat, blueberry, Aruna berteduh, fermentasi 12 detik | Belum ada sungai dengan tinggi air dinamis; latar pulau menampilkan laut |
| Kabut | Jamur, target bake +1 detik, roti rempah, quest Aruna di mercusuar | Veil transparan screen-space, bukan volumetric 3D |
| Cerah | Ladang dan dermaga tersedia, buah/madu, favorit baguette | Preferensi dan akses berubah; tidak ada timer antrean cepat agar tetap tanpa tekanan |
| Karakter | Mira, Nami, Aruna dengan pakaian sesuai deskripsi; tiga arah dan potret | PDF revisi tidak melampirkan turnaround sumber, sehingga atlas merupakan interpretasi baru. Proporsi chibi mengikuti ilustrasi hasil generasi, bukan model-sheet ukuran presisi. Berjalan memakai bob, bukan animasi kaki penuh |
| Desa pesisir | Bakery pusat, ladang kiri, mercusuar kanan atas, dermaga kanan bawah, sumur | Ilustrasi utuh dengan jalur interaksi, bukan tile set modular |
| Interior | Oven batu, pantry, meja ulen, etalase lengkung, meja dekat jendela | Furnitur baked ke latar; tidak ada dekorasi yang dapat dipindah |
| URP 2D | Renderer2D, Global Light2D warna/intensitas GDD, amber point lights oven/lampu, rain particles/ripples, kabut, soft sun shaft | Belum diuji pada Unity Editor/GPU di cloud. Light shaft merupakan overlay artistik, bukan ray tracing |
| Sorting/grid | Orthographic camera, IsometricZAsY Grid + Tilemap scaffold, 6 sorting layers, Y-sorted characters | Tilemap belum berisi tile modular; backdrop dan aktor adalah SpriteRenderer |
| UI | uGUI, tekstur kayu/kertas prosedural, Sprite border 9-slice, potret, timer/slider oven, sound toggle | Tidak memakai Sprite Editor manual; border 9-slice dibuat oleh kode. Kontrol pengaturan berbentuk tombol teks, belum ikon gear berukir tersendiri |
| Item | 8 ikon roti + 8 ikon bahan sesuai katalog | Herba dan buah tambahan tampil sebagai label inventori |
| Progress | 8 resep, 10 bahan, roti ditemukan, hubungan, quest Aruna, gudang madu terbuka, save/bak | Cerita personal hanya Aruna; Nami memberi petunjuk. Belum sistem musim dengan kalender atau cerita warga tambahan |
| No game over | Semua hasil roti diterima, bonus resep/kualitas, tanpa kehilangan progres utama | Akses area dan batch aktif membatasi tindakan, bukan kondisi gagal |

GDD dipakai sebagai spesifikasi desain. Keputusan platform tetap Unity 2022.3.62f1 sesuai permintaan pengguna. Revisi ini menggantikan loop tiga-order otomatis dari prototipe awal dengan progres harian bebas dan tujuan cerita/koleksi.
