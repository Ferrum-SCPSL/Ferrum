# Фактически выполненные проверки

> Исторические результаты: приведённые ниже проверки относятся к версии до рефакторинга структуры и прямого логирования ServerConsole. После этих изменений сборка и тесты не запускались по просьбе пользователя.

Дата: **06.11.2026**, Europe/Kiev. Рабочий проект:
`C:\Users\Client\source\repos\Ferrum`.

## Результаты

| Проверка | Результат |
|---|---|
| `cargo fmt --check` | Пройдено |
| `cargo clippy --locked --all-targets --all-features -- -D warnings` | Пройдено |
| Rust unit tests | 3/3 |
| C# / native Debug | Сборка успешна, C# без предупреждений/ошибок |
| C# / native Release | Сборка успешна, C# без предупреждений/ошибок |
| Реальный FFI Debug | **10 059 assertions, PASS** |
| Реальный FFI Release | **10 059 assertions, PASS** |
| Незавершённые Rust-owned buffers | **0** после нормальных вызовов, ошибок и GC stress |
| Отсутствие test-hooks в production | Подтверждено попытками разрешения обоих тестовых экспортов |
| Windows dedicated server, Release | **PASS**, LabAPI загрузил Ferrum, callback и результат проверены |
| Linux автономный FFI | Workflow подготовлен, **не запускался** |
| Linux dedicated server | **Не проверен** |

FFI smoke покрывает ASCII, кириллицу, emoji, пустую строку, встроенный NUL,
длинный текст, 10 000 повторений с принудительным GC, некорректный UTF-8,
нулевой указатель с ненулевой длиной, отсутствующий callback, исключение C# callback,
контролируемый Rust panic с последующим успешным вызовом, освобождение error payload,
отсутствующий файл/экспорт, ABI 999, неверный native image, Dispose и повторное включение.
Rust unit tests дополнительно проверяют null output/free и layout ABI.

Диагностический счётчик доказывает отсутствие незавершённых переданных буферов
в этих сценариях; он не является общим профилировщиком native памяти.

## Проверенное окружение

- Windows x64; managed target `net48`, C# 12; SDK .NET **10.0.400**.
- Rust **1.99.1** (`58cf8f9c3`, 2026-10-13), target `x86_64-pc-windows-msvc`.
- Установлены rustup, rustfmt, clippy, MSVC Build Tools и Windows SDK **10.0.26100.0**.
- Исходный dedicated server: Steam app **996560**, build ID **26364472**,
  ветка `public` из локального Steam manifest.
- Серверный LabAPI: **1.1.7.0**, LocalAdmin: **2.5.16**.
- Unity из серверного журнала: **6000.0.43f1** (`97272b72f107`).
- Сервер использует bundled Mono: журнал содержит пути Managed и MonoBleedingEdge.
  Точная версия/сборка Mono отдельно не установлена; `net48` не используется как её доказательство.

## Серверное доказательство

Финальный прогон расположен в
`artifacts/server-smoke/20261105-234037-229/`:

- `console.log`: загрузка Ferrum.Interop и Ferrum, callback, успешная проверка.
- `unity.log`: Unity version, managed/Mono paths.
- Копия сервера с отдельной hoster policy и конфигурацией, порт 7788, loopback bindings.

Подтверждённый маркер:

```text
[Ferrum] PoC passed: ABI=1, platform=win-x64, callback=verified, UTF-8=verified
```

Loader сравнивает исходную строку `Ferrum: Привет из C# — Rust 🦀` и оба результата
по `StringComparison.Ordinal` до записи маркера. В Windows OEM-консоли LocalAdmin
длинное тире может отображаться как дефис, emoji как `??`; это ограничение отображения
консоли после проверки Unicode, а не потери данных на FFI. Smoke-скрипт декодирует
OEM stdout в UTF-8 файл журнала.

Тестовые процессы завершены. Установленный сервер и обычные конфиги AppData не изменены.
Из имеющейся записи LocalAdmin перенесён только timestamp принятия EULA в тестовую копию;
токены и другая конфигурация не переносились.

## Повторение

Из корня проекта в PowerShell 7:

```powershell
./scripts/test.ps1 -Configuration Debug
./scripts/test.ps1 -Configuration Release
./scripts/server-smoke.ps1 -ServerPath 'D:\SteamLibrary\steamapps\common\SCP Secret Laboratory Dedicated Server' -Configuration Release
```

Журналы автоматических проверок: `artifacts/verification-debug.log`,
`artifacts/verification-release.log`.

Production-комплект: `artifacts/package/win-x64/Release/LabAPI`.
Он содержит Ferrum.Loader, Ferrum.Interop, ferrum_native и symbols; LabAPI,
EXILED, YamlDotNet и игровые assemblies в него не входят.

## Ограничения и следующий шаг

Linux workflow нужно запустить после публикации кода, затем отдельно проверить Linux
dedicated server и зафиксировать его точные версии. CI без этого не подтверждает Unity Mono.
Горячая замена, физический unload, отдельные Rust-плагины, игровой SDK и события не реализованы.

Git-репозиторий локально не создавался, изменения на GitHub/Linear не публиковались.
Исходный пустой C# проект сохранён отдельно в
`C:\Users\Client\Documents\Codex\2026-11-06\new-chat\work\original-Ferrum-project`.

## Изменение каталогов

Рабочий корень перенесён в ~/.config/Ferrum с Configs, Logs и Plugins/Dependencies.
Исходники, упаковка и изоляция smoke-скрипта обновлены без запуска сборки и тестов
по просьбе пользователя. Прежние бинарные артефакты не пересобирались.
