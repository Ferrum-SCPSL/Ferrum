# Сборка и проверка Ferrum

## Инструменты

- PowerShell 7; .NET SDK 10.0.400 (закреплён в global.json).
- Target framework managed-проектов: .NET Framework 4.8, C# 12, x64.
  Reference assemblies восстанавливаются из Microsoft.NETFramework.ReferenceAssemblies 1.0.3.
- Rust 1.99.1, rustfmt, clippy (rust-toolchain.toml), Cargo.lock.
- Windows: MSVC x64 и Windows SDK; установщик Visual Studio, компоненты
  «Разработка классических приложений на C++». Скрипты импортируют окружение
  vcvars64 только в текущий процесс.
- Linux x64: C linker/GCC, glibc, .NET SDK, PowerShell 7 и Mono для net48 smoke runner.

Rust: установить официальный rustup, затем `rustup toolchain install 1.99.1 --profile minimal --component rustfmt --component clippy`.
Скрипты также ищут Cargo в стандартном каталоге rustup, без изменения глобального PATH.

## Команды из корня репозитория

```powershell
./scripts/build.ps1 -Configuration Debug
./scripts/build.ps1 -Configuration Release
./scripts/test.ps1 -Configuration Debug
./scripts/test.ps1 -Configuration Release
```

`build.ps1` создаёт production native и managed сборки, затем комплект
`artifacts/package/<win-x64|linux-x64>/<Debug|Release>/`:

```text
LabAPI/
  plugins/global/Ferrum.Loader.dll (+ pdb)
  dependencies/global/Ferrum.Interop.dll (+ pdb)
.config/Ferrum/
  Configs/
  Logs/
  Plugins/Dependencies/ferrum_native.dll или libferrum_native.so
```

Серверные LabAPI/YamlDotNet/game assemblies не входят в комплект.
Рабочий корень на Windows и Linux одинаков: `~/.config/Ferrum`, где `~` — профиль
пользователя, запускающего сервер. На текущем ПК это `C:\Users\Client\.config\Ferrum`.
Каталоги автоматически создаются при включении Ferrum. Для отдельного экземпляра
можно задать абсолютный путь в `FERRUM_HOME`; обычная установка этого не требует.
`test.ps1` проверяет rustfmt, clippy, Rust unit tests, собирает production и отдельную
test-hooks библиотеку, выполняет настоящие вызовы FFI и 10 000 повторений с GC.
Ожидаемый panic в тесте печатается штатным Rust panic hook, после чего тест проверяет,
что процесс продолжает работу. Это не означает падение smoke runner.

## Изолированный Windows dedicated server

```powershell
./scripts/server-smoke.ps1 `
  -ServerPath 'D:\SteamLibrary\steamapps\common\SCP Secret Laboratory Dedicated Server' `
  -Configuration Release
```

Скрипт копирует сервер в новый каталог artifacts/server-smoke, включает локальную
hoster policy, создаёт отдельные конфиги, использует порт 7788 и loopback bindings.
Рабочий сервер и обычный каталог AppData не изменяются. Нужен свободный порт и
место для копии сервера. Используется только уже существующая запись принятия EULA
из LocalAdmin; токены и остальные данные не копируются. Дочерний тестовый процесс
завершается после проверки.
`FERRUM_HOME` дочернего процесса указывает на `.config/Ferrum` внутри тестовой копии,
поэтому настоящий пользовательский каталог Ferrum не используется.

После включения всех плагинов LabAPI и успешной инициализации моста Ferrum
выводится один приветственный баннер. Проверка FFI выполняется без обычных
сообщений `Received UTF-8` и `PoC passed`; ошибки инициализации остаются видимыми.
Баннер содержит надпись FERRUM и следующие строки:

```text
Thank you for using and supporting the Ferrum framework!
Branch: alpha | Version: v0.1.0
Contributors: Narin
```

Для обычной установки после остановки сервера вручную объединить содержимое
`LabAPI` из комплекта с каталогом LabAPI, выбранным серверным PathManager. Содержимое
`.config/Ferrum` поместить в `~/.config/Ferrum` пользователя сервера. Rust-мост
находится в `Plugins/Dependencies`, отдельно от сканирования managed DLL LabAPI.
Автоматического развёртывания нет. Папка Logs пока создаётся без записи файлов;
собственные сообщения Ferrum выводятся напрямую в серверную консоль.

## Linux dedicated server: отдельная проверка

1. Подготовить отдельный SCP:SL dedicated server Linux x64. Зафиксировать Steam build ID,
   Unity/runtime из запуска и фактическую версию LabAPI; начальная цель — LabAPI 1.1.7.
2. На Linux выполнить обе конфигурации `test.ps1`, затем использовать production
   `artifacts/package/linux-x64/Release/` с двумя частями: `LabAPI` и `.config/Ferrum`.
3. В отдельной копии сервера создать hoster_policy.txt с `gamedir_for_configs: true`,
   изолированные конфиги и свободный порт; managed-часть поместить в каталог LabAPI,
   возвращаемый PathManager для этой конфигурации. Часть `.config/Ferrum` разместить
   в отдельном каталоге стенда и задать его через `FERRUM_HOME` до запуска LocalAdmin.
4. Запустить LocalAdmin из каталога копии сервера. Подтвердить загрузку Ferrum,
   приветственный баннер после инициализации; сохранить логи и остановить сервер.
5. До этого результата Linux CI доказывает только автономный FFI под Mono,
   а не совместимость с серверным Unity Mono.

CI описан в .github/workflows/ci.yml: Windows/Linux, Debug/Release. Сам файл workflow
не доказывает успешный прогон; результаты выполненных проверок записаны в verification.md.

## Границы PoC

ABI экспериментальный, API не считается стабильным. EXILED, SDK, игровые обёртки,
события и горячая замена не реализованы. Dispose запрещает новые вызовы, но не
выгружает библиотеку. Runtime сервера не выводится из target framework проекта.

## Архитектура и вывод логов

Код разделён по Features, Contracts, Delegates, Memory, Marshalling, Callbacks и
платформенным обёрткам: см. [структуру](architecture.md). Собственные сообщения
Ferrum идут через Log и ServerConsole.AddLog, без логгера LabAPI. Префиксы —
[INFO] [Ferrum] и [INFO] [Ferrum/Rust]. LabAPI продолжает выводить собственные
сообщения загрузки своим способом. После этого рефакторинга сборка и тесты не запускались.

## Праздничные поздравления

Перед строкой Thank you for using and supporting the Ferrum framework! выводится:
- 25–31 октября включительно — Spooky scary skeletons! Happy Halloween!
- 25 декабря–7 января включительно — Happy New Year!
- В остальные даты дополнительная строка отсутствует.

Дата сервера берётся один раз через DateTime.Today при выводе баннера.
Выбор строки вынесен в публичный HolidayGreeting.GetMessage(DateTime date).
Цвет DarkYellow, логотип, благодарность, ветка, версия с v и участники сохраняются.
Период Нового года охватывает конец декабря и начало января независимо от года.
