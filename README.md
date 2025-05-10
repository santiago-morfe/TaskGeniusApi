# TaskGeniusApi

TaskGeniusApi es una API desarrollada en .NET 8 que proporciona funcionalidades para la gestión de tareas y usuarios, así como recomendaciones inteligentes para mejorar la productividad. Este proyecto forma parte de un portafolio profesional y está diseñado para demostrar habilidades en el desarrollo de aplicaciones backend escalables y seguras.

## Características principales

- **Gestión de usuarios**: Registro, inicio de sesión y recuperación de información de usuarios.
- **Gestión de tareas**: Creación, actualización, eliminación y consulta de tareas.
- **Recomendaciones inteligentes**: Generación de consejos personalizados y sugerencias para mejorar la gestión de tareas.
- **Autenticación y autorización**: Implementación de JWT para garantizar la seguridad de la API.
- **Paginación**: Respuesta optimizada para grandes conjuntos de datos.

## Estructura del proyecto

El proyecto sigue una arquitectura modular y está organizado en las siguientes carpetas principales:

- **Controllers**: Contiene los controladores que manejan las solicitudes HTTP y actúan como intermediarios entre el cliente y los servicios.
- **Data**: Incluye el contexto de la base de datos y las configuraciones necesarias para interactuar con Entity Framework Core.
- **DTOs**: Define los objetos de transferencia de datos utilizados para la comunicación entre las capas de la aplicación.
- **Models**: Contiene los modelos de datos principales que representan las entidades del dominio.
- **Services**: Implementa la lógica de negocio y los servicios de la aplicación, como la gestión de usuarios, tareas y recomendaciones.
- **Migrations**: Archivos generados por Entity Framework Core para la gestión de la base de datos, incluyendo la creación y actualización de esquemas.

## Tecnologías utilizadas

### Lenguajes
- **C#**: Lenguaje principal utilizado para el desarrollo de la API.

### Frameworks y bibliotecas
- **.NET 8**: Framework principal para el desarrollo de la API.
- **Entity Framework Core**: ORM para la gestión de la base de datos.
- **JWT (JSON Web Tokens)**: Para la autenticación y autorización.
- **BCrypt**: Para el hash y verificación de contraseñas.

### Base de datos
- **SQLite**: Base de datos utilizada en el desarrollo.

### Entornos de trabajo
- **Visual Studio**: IDE principal para el desarrollo del proyecto.
- **Railway**: Plataforma utilizada para el despliegue de la API.
- **Postman**: Herramienta para probar y documentar los endpoints de la API.

## Controladores

### AuthController
- **Descripción**: Maneja la autenticación y el registro de usuarios.
- **Endpoints**:
  - `POST /auth/register`: Registra un nuevo usuario.
  - `POST /auth/login`: Inicia sesión y genera un token JWT.

### TaskController
- **Descripción**: Proporciona funcionalidades para la gestión de tareas.
- **Endpoints**:
  - `GET /tasks`: Obtiene todas las tareas del usuario autenticado.
  - `GET /tasks/{id}`: Obtiene una tarea específica por su ID.
  - `POST /tasks`: Crea una nueva tarea.
  - `PUT /tasks`: Actualiza una tarea existente.
  - `DELETE /tasks/{id}`: Elimina una tarea específica.

### GeniusController
- **Descripción**: Ofrece recomendaciones inteligentes y funcionalidades relacionadas con la mejora de la productividad.
- **Endpoints**:
  - `GET /genius/advice`: Genera consejos personalizados basados en las tareas del usuario.
  - `POST /genius/titleSuggestion`: Sugiere un título para una tarea basada en su descripción.
  - `POST /genius/descriptionFormatting`: Formatea la descripción de una tarea.
  - `GET /genius/taskQuestion/{question}`: Responde preguntas relacionadas con las tareas del usuario.
  - `GET /genius/taskAdvice/{TaskId}`: Proporciona consejos específicos para una tarea.

## Endpoints principales

- **Autenticación**:
  - `POST /auth/login`: Inicia sesión y genera un token JWT.
  - `POST /auth/register`: Registra un nuevo usuario.

- **Usuarios**:
  - `GET /users`: Obtiene la lista de usuarios (pendiente de implementación).
  - `PUT /users/{id}`: Actualiza un usuario existente (pendiente de implementación).

- **Tareas**:
  - `GET /tasks`: Obtiene la lista de tareas.
  - `POST /tasks`: Crea una nueva tarea.
  - `PUT /tasks/{id}`: Actualiza una tarea existente.
  - `DELETE /tasks/{id}`: Elimina una tarea.

- **Recomendaciones**:
  - `GET /genius/advice`: Genera consejos personalizados.
  - `POST /genius/titleSuggestion`: Sugiere títulos para tareas.
  - `POST /genius/descriptionFormatting`: Formatea descripciones de tareas.

## Despliegue

El proyecto se encuentra desplegado en: [taskgeniusapi-production-5575.up.railway.app](https://taskgeniusapi-production-5575.up.railway.app)

## Contacto

- **Correo electrónico**: [santiago01morfe@gmail.com](mailto:santiago01morfe@gmail.com)
- **GitHub**: [https://github.com/santiago-morfe](https://github.com/santiago-morfe)
- **LinkedIn**: [https://linkedin.com/in/jhonyer-santiago-pineda-marin-dev](https://linkedin.com/in/jhonyer-santiago-pineda-marin-dev)

---

Este proyecto es parte de mi portafolio profesional y está diseñado para demostrar mis habilidades en el desarrollo de aplicaciones backend modernas.