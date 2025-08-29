English Language 

# 🏷️ Versioning Policy

This document describes the Semantic Versioning (SemVer) system used in the project, as well as the rules for naming commits and tags.

## Version Structure

The project uses a hybrid versioning system adapted for the .NET (Visual Studio) ecosystem and Git.

### 1. Internal Assembly Version
Uses the format `X.X.X.X` and is used exclusively for builds in Visual Studio.
*   **X.0.0.0** — Major release with new features and significant changes that break backward compatibility.
*   **X.X.0.0** — Addition of new functionality (Minor). Backward compatibility is maintained.
*   **X.X.X.0** — Bug fixes and minor improvements (Patch).
*   **X.X.X.X** — Build number or revision for internal purposes.

### 2. Public Version (Git Tag / Release)
Uses the format `X.X.X` and is used for tagging commits, creating GitHub releases, and for users.
This version follows **Semantic Versioning** principles:
*   **MAJOR** (`X.0.0`): Version with critical changes that break backward compatibility.
*   **MINOR** (`X.X.0`): Addition of new functionality in a backward-compatible manner.
*   **PATCH** (`X.X.X`): Bug fixes and minor improvements that do not affect the public API.

---

## Build Identifier (Build ID)

A unique identifier is used for precise identification of each specific build.
*   **Format:** `12AB3C` (2 digits, 2 letters, 1 digit, 1 letter).
*   **Purpose:** Used for internal tracking, search, and references in the Change Log.
*   **Usage:**
    *   Only the semantic version is indicated in the release title (e.g., `v2.1.0 Minor`).
    *   The version is duplicated in the release description body with the build identifier: **`v2.1.0 Minor (Build: 12AB3C)`**.

---

## Commit and Tag Naming Convention

Commit messages and tags should have a clear and understandable format.

### Tag Format (Git Tag)
`vX.X.X (NameTag)`
*   Example: `v2.1.0 (BrightIdea)`, `v2.1.1 (PatchUp)`

### List of Tags (NameTag) and Their Meaning

| Name Tag         | Purpose                                                                                             |
| :--------------- | :-------------------------------------------------------------------------------------------------- |
| **PatchUp**      | Fixing bugs and minor issues.                                                                       |
| **Wordsmith**    | Corrections in documentation, comments, or the `README.md` file.                                    |
| **BrightIdea**   | Adding new features or improvements to existing ones.                                               |
| **CleanSweep**   | Refactoring, code cleanup, improving readability without functional changes.                        |
| **Boost**        | Performance optimization.                                                                           |
| **Gatekeeper**   | Changes related to security, authentication, or authorization.                                      |
| **GlueCode**     | Fixing integration, connections between modules or microservices.                                   |
| **Vision**       | Changes in UI/UX, design, and visual elements.                                                      |
| **Anchor**       | Fixing critical errors, stabilizing a release, "hot" fixes.                                         |
| **Bridge**       | Data migrations, database schema changes.                                                           |
| **HookUp**       | Adding new API endpoints, webhooks, or extension points.                                            |
| **NugetUp**      | Adding or updating dependencies (NuGet packages).                                                   |
| **TuneUp**       | Improving configuration, application settings, or environment files.                                |
| **Redesign**     | Change interface or change logo                                                                     |

---

## Examples

1.  **Major Release:**
    *   **Tag:** `v2.0.0 (Major)`
    *   **Assembly:** `2.0.0.0`
    *   **Release Description:** `v2.0.0 Major (Build: 88CD4F)`

2.  **Adding a New Feature:**
    *   **Tag:** `v2.1.0 (BrightIdea)`
    *   **Assembly:** `2.1.0.0`

3.  **Fixing a Critical Bug:**
    *   **Tag:** `v2.1.1 (Anchor)`
    *   **Assembly:** `2.1.1.0`

4.  **Updating Documentation:**
    *   Commit message: `docs: Updated API documentation (Wordsmith)`
    *   A tag is usually not created for such a change.


Russia Language 

# 🏷️ Политика контроля версий

В этом документе описывается система семантического версионирования (SemVer), используемая в проекте, а также правила именования коммитов и тегов.

## Структура версии

В проекте используется гибридная система версий, адаптированная для экосистемы .NET (Visual Studio) и Git.

### 1. Внутренняя версия сборки (Assembly Version)
Имеет формат `X.X.X.X` и используется исключительно для сборок в Visual Studio.
*   **X.0.0.0** — Крупный релиз (Major) с новыми функциями и значительными изменениями, ломающими обратную совместимость.
*   **X.X.0.0** — Добавление нового функционала (Minor). Обратная совместимость сохранена.
*   **X.X.X.0** — Исправления багов и мелкие улучшения (Patch).
*   **X.X.X.X** — Номер сборки или ревизии (Build/Revision) для служебных целей.

### 2. Публичная версия (Git Tag / Release)
Имеет формат `X.X.X` и используется для тегирования коммитов, создания релизов в GitHub и для пользователей.
Эта версия следует принципам **семантического версионирования**:
*   **MAJOR** (`X.0.0`): Версия, в которой есть критические изменения, ломающие обратную совместимость.
*   **MINOR** (`X.X.0`): Добавление новой функциональности, не ломающее обратную совместимость.
*   **PATCH** (`X.X.X`): Исправления багов и мелкие улучшения, не влияющие на публичный API.

---

## Идентификатор сборки (Build ID)

Для точной идентификации каждой конкретной сборки используется уникальный идентификатор.
*   **Формат:** `12AB3C` (2 цифры, 2 буквы, 1 цифра, 1 буква).
*   **Назначение:** Используется для внутреннего отслеживания, поиска и ссылок в Change Log.
*   **Использование:**
    *   В заголовке релиза указывается только семантическая версия (например, `v2.1.0 Minor`).
    *   В теле описания релиза дублируется версия с указанием идентификатора сборки: **`v2.1.0 Minor (Build: 12AB3C)`**.

---

## Конвенция именования коммитов и тегов

Сообщения коммитов и теги должны иметь четкий и понятный формат.

### Формат тега (Git Tag)
`vX.X.X (NameTag)`
*   Пример: `v2.1.0 (BrightIdea)`, `v2.1.1 (PatchUp)`

### Список тегов (NameTag) и их значение

| Name Tag         | Назначение                                                                 |
| :--------------- | :------------------------------------------------------------------------- |
| **PatchUp**      | Исправление багов и мелких проблем.                                        |
| **Wordsmith**    | Правки в документации, комментариях или файле `README.md`.                |
| **BrightIdea**   | Добавление новых функций или улучшений существующих.                       |
| **CleanSweep**   | Рефакторинг, чистка кода, улучшение читаемости без функциональных изменений. |
| **Boost**        | Оптимизация производительности.                                            |
| **Gatekeeper**   | Изменения, связанные с безопасностью, аутентификацией или авторизацией.    |
| **GlueCode**     | Исправления интеграции, связи между модулями или микросервисами.           |
| **Vision**       | Изменения в UI/UX, дизайне и визуальных элементах.                         |
| **Anchor**       | Исправление критических ошибок, стабилизация релиза, "горячие" фиксы.      |
| **Bridge**       | Миграции данных, изменения схемы базы данных.                              |
| **HookUp**       | Добавление новых API endpoints, webhooks или точек расширения.             |
| **NugetUp**      | Добавление или обновление зависимостей (NuGet пакетов).                    |
| **TuneUp**       | Улучшение конфигурации, настроек приложения или файлов окружения.          |
| **Redesign**     | Обновление интерфейса или логотипа                                         |

---

## Примеры

1.  **Крупный релиз:**
    *   **Тег:** `v2.0.0 (Major)`
    *   **Сборка:** `2.0.0.0`
    *   **Описание релиза:** `v2.0.0 Major (Build: 88CD4F)`

2.  **Добавление новой фичи:**
    *   **Тег:** `v2.1.0 (BrightIdea)`
    *   **Сборка:** `2.1.0.0`

3.  **Исправление критического бага:**
    *   **Тег:** `v2.1.1 (Anchor)`
    *   **Сборка:** `2.1.1.0`

4.  **Обновление документации:**
    *   Коммит с сообщением: `docs: Updated API documentation (Wordsmith)`
    *   Тег для такого изменения обычно не создается.

ph1ncyn© 2025
