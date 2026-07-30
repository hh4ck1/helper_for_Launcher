# Imperfect Salvation — launcher distribution

Этот репозиторий является хранилищем версий сборки для лаунчера.

- `channel/stable/manifest.json` — актуальная сборка;
- `channel/stable/manifest.sig` — её цифровая подпись;
- `packs/<version>/` — неизменяемые файлы опубликованных версий;
- `public-key.pem` — открытый ключ для проверки подписи.

Файлы здесь публикуются автоматически командой из основного проекта:

```powershell
.\launcher\Publish-Pack.ps1 -Version "0.1.96"
```

Закрытый ключ подписи никогда не должен попадать в этот репозиторий.
