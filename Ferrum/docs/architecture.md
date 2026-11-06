# Структура Ferrum

Разделение по ответственности ориентировано на структуру EXILED: короткая точка
входа, отдельные Features, Enums, Structs, Delegates, Exceptions и платформенные
обёртки. Пустых проектов для ещё не реализованных игровых подсистем нет.

```text
Ferrum.Loader/
  LoaderPlugin.cs                     # Точка входа LabAPI и метаданные плагина
  Enums/
    LogLevel.cs
  Features/
    BridgeLoader.cs                   # Enable/Disable, владелец NativeBridge
    Diagnostics/
      BridgeProbe.cs                  # Проверка строки и обратного callback
    Paths/
      FerrumPaths.cs                  # ~/.config/Ferrum и создание рабочих каталогов
      NativePaths.cs                  # Rust-мост в Plugins/Dependencies
    Logging/
      Log.cs                          # Формат, уровни, цвета и источник Ferrum/Rust
      ServerConsoleWriter.cs          # Прямой ServerConsole.AddLog

Ferrum.Interop/
  Features/
    NativeBridge.cs                   # Публичный фасад, блокировка и Dispose
    NativeModule.cs                   # Дескриптор модуля и получение экспортов
  Contracts/
    NativeApi.cs                      # Проверка ABI/layout и таблица функций
  Delegates/
    AbiVersion.cs
    LogCallback.cs
    ProcessUtf8.cs
    FreeBuffer.cs
  Enums/
    NativeStatus.cs
  Structs/
    NativeBuffer.cs
  Exceptions/
    NativeBridgeException.cs
  Platforms/
    NativePlatform.cs                 # Платформа, архитектура, имя библиотеки
  Native/
    WindowsNativeMethods.cs           # Только объявления Windows P/Invoke
    LinuxNativeMethods.cs             # Только объявления Linux P/Invoke
  Loading/
    NativeLibraryLoader.cs            # Загрузка и диагностика ошибок ОС
    NativeModuleRegistry.cs           # Кэш модулей на срок жизни процесса
  Marshalling/
    Utf8Marshaller.cs                 # Строгий UTF-8 без неявного ANSI
  Memory/
    PinnedUtf8Input.cs                 # Время жизни GCHandle входного массива
    OwnedNativeBuffer.cs              # Освобождение Rust-owned результата
  Callbacks/
    NativeLogCallback.cs              # Удержание делегата, перехват исключений C#
  Invocation/
    NativeInvocation.cs               # Синхронный вызов в ресурсных using-областях
  Properties/
    AssemblyInfo.cs                   # Доступ для существующего smoke runner
```

## Вызовы и владение

`LoaderPlugin` передаёт lifecycle в `BridgeLoader`. Загрузчик получает путь через
`NativePaths`, создаёт `NativeBridge` и вызывает вынесенный `BridgeProbe`.
Перед загрузкой `FerrumPaths` создаёт `Configs`, `Logs`, `Plugins` и
`Plugins/Dependencies` в `~/.config/Ferrum` на обеих ОС. Для изолированного стенда
корень можно явно переопределить переменной `FERRUM_HOME`.

```text
~/.config/Ferrum/
  Configs/
  Logs/
  Plugins/
    Dependencies/
      ferrum_native.dll или libferrum_native.so
```

`Ferrum.Loader.dll` и `Ferrum.Interop.dll` остаются в каталогах LabAPI, который
загружает managed-код. Собственные данные Ferrum и Rust-мост находятся в его корне.
Папки Configs и Logs подготовлены для соответствующих подсистем; создание папки
Logs само по себе не добавляет запись логов в файлы. Пользовательские Rust-плагины
пока автоматически не загружаются.

`NativeBridge` оставляет публичный интерфейс `RoundTrip` и сериализует обращения
с `Dispose`. `NativeApi` отдельно проверяет ABI и связывает функции. За дескрипторы
отвечает registry, за конкретные вызовы ОС — loader и NativeMethods.

В одном `NativeInvocation` создаются независимые владельцы input, callback и output.
При выходе сначала освобождается Rust output, затем заканчивается время жизни
callback, затем снимается pin входного массива. Эти области действуют также при
ошибке callback, ошибочном статусе Rust и исключении декодирования.

Публичные `NativeBridge`, `NativeStatus`, `NativeBridgeException` остаются в
пространстве имён `Ferrum.Interop`; контракт C ABI и native Rust-код не менялись.

## Собственные логи Ferrum

```text
Rust callback → NativeLogCallback → BridgeProbe → Log.Rust
                                                   ↓
                               ServerConsoleWriter → ServerConsole.AddLog
```

Все сообщения Ferrum проходят через собственный `Log`, без вызовов логгера LabAPI:

```text
[INFO] [Ferrum] ...
[INFO] [Ferrum/Rust] ...
[WARN] [Ferrum] ...
[ERROR] [Ferrum] ...
```

Именно `ServerConsole.AddLog` используется для вывода в EXILED. Связывание Ferrum
с уже загруженным игровым типом кэшируется: для двух аргументов создаётся прямой
делегат, для перегрузки с дополнительными optional-параметрами используется прямой
reflection-вызов этого же игрового метода. Это позволяет не добавлять ссылку на
закрытую игровую assembly для компиляции и не менять сборку CI. LabAPI logger и
EXILED не участвуют в этом пути. У сообщений самого загрузчика LabAPI остаётся
его собственный формат.

Native-библиотека по-прежнему остаётся загруженной до завершения процесса.
Сам рефакторинг не добавляет физическую выгрузку или горячую замену.

## Проверки этой редакции

По просьбе пользователя после изменения структуры и логирования сборка, тесты,
форматтеры, CI и dedicated server не запускались. Существующие DLL/пакеты в artifacts
и прежний отчёт о проверках относятся к предыдущей редакции исходников.

Ориентир: [EXILED](https://github.com/ExMod-Team/EXILED/tree/master/EXILED),
[EXILED Log](https://github.com/ExMod-Team/EXILED/blob/master/EXILED/Exiled.API/Features/Log.cs).
