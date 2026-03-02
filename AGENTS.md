# Project Purpose

Socialstack is a modular framework for making a variety of websites and apps.

* Tech Stack: C# (Backend), MongoDB, React/TypeScript (Frontend/Admin/Email).

* Auth: Supports logged-in users and guest users. Permissions are role and capability based.

# Project Structure

The project uses a modular design. UI imports do not contain the /Source/ level.

* Api/ - C# modules (Controllers, Services, Entities).

* UI/Source/ - React functional components for the main site.

* Admin/Source/ - React functional components for the admin panel.

* Email/Source/ - React components for email rendering.

* TypeScript/ - Auto-generated TS bindings from C# reflection.

# Backend (C# / SocialStack)

* Controllers: Do not assume standard ASP.NET behavior. Controllers are abstracted to support WebSockets and Server-Side Rendering (SSR).

* Context: Use the Context object to access User, their Role and Locale.

* Extensibility: Use the WordPress-style event system (e.g., Events.User.BeforeCreate) for cross-module communication, as well as dependency injection in service/ controller constructors and `Services.Get<ServiceType>()`. Favour using Services.Get only when a cyclical reference would occur.

* Entity Generation: Use socialstack generate Api/{EntityName} (Singular) when the entity you need is new.

* Fields added to entities are automatically readable, writeable and put in the database. Sensitive fields, such as an email address, may require permission rules to restrict who can read/write it.

# Frontend (React / TypeScript)

State Management: 

* Use `useRouter` (from `UI/Router`) for URL/Query state.

* Use `useSession` (from `UI/Session`) for User or Business data.

API Calls: Use generated bindings. `import userApi from 'Api/User';`. Use `useApi` to call these methods; it is a useEffect except it runs on the server-side renderer.

Includes: Socialstack has a mechanism for including related information in one request. E.g. the creatorUser of a BlogPost. Includes are passed as an array during an API call.

Styling: Use SCSS within functional components in a .scss file. Prefer standard CSS syntax as much as possible. BEM.

# Critical Constraints & Patterns

* No Async/Await in UI: Use `.then()` promises exclusively. This is required for the Server-Side Renderer to function correctly.

* Import Style: Always use the shortened SocialStack paths.

Correct: `import User from 'UI/User'`;
Incorrect: `import User from 'UI/Source/ThirdParty/User'`;

* Bindings: If API signatures change, bindings must be regenerated via running the built C# itself; `SocialStack.Api.exe ts-bindings`.

# Tooling & Commands

* Generate Entity: `socialstack generate Api/EntityName`. Only done if the entity required does not already exist.

* Build UI: `socialstack buildui`

* Generate Bindings: `bin/Debug/net9.0/SocialStack.Api.exe ts-bindings`